using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rcv.Web.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="Rcv.Web.Api.Controllers.AuthController"/>.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to spin up a real
/// in-process test server with mocked services.
/// </summary>
public class AuthControllerTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthControllerTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    // -----------------------------------------------------------------------
    // GET /api/auth/login/{provider}
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("google")]
    [InlineData("microsoft")]
    public async Task Login_WithValidProvider_RedirectsToProviderConsentScreen(string provider)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Do not follow redirects so we can assert the 302 status
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/api/auth/login/{provider}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "login should initiate an OAuth2 redirect to the provider");
        response.Headers.Location.Should().NotBeNull("redirect must include a Location header");
    }

    [Fact]
    public async Task Login_WithUnknownProvider_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/login/unknown-provider");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("GOOGLE")]
    [InlineData("Microsoft")]
    [InlineData("MICROSOFT")]
    public async Task Login_IsCaseInsensitive(string provider)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync($"/api/auth/login/{provider}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "provider name matching should be case-insensitive");
    }

    // -----------------------------------------------------------------------
    // GET /api/auth/callback/{provider}
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Callback_WithUnknownProvider_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/callback/unknown-provider");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Callback_WithoutExternalCookie_ReturnsUnauthorized()
    {
        // Calling /callback directly without going through the OAuth flow means
        // no External cookie is present, so AuthenticateAsync("External") will fail.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/callback/google");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Callback_WhenSuccessful_SetsJwtCookieAndRedirectsToDashboard()
    {
        // Use a fresh factory so ConfigureWebHost runs with the fake external auth configured.
        // (The shared _factory may already be built; a new instance ensures our overrides apply.)
        await using var factory = new AuthApiFactory()
            .WithFakeExternalAuth(
                externalId: "google-456",
                email: "callback@example.com",
                displayName: "Callback User");

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/auth/callback/google");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "a successful callback should redirect to the frontend");
        response.Headers.Location?.ToString().Should().Be("/dashboard");

        var setCookie = response.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .ToList();

        setCookie.Should().Contain(h => h.StartsWith("rcv_jwt="),
            "the JWT cookie must be set after a successful callback");
        setCookie.Should().Contain(h => h.Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "the JWT cookie must be httpOnly");
        setCookie.Should().Contain(h => h.Contains("secure", StringComparison.OrdinalIgnoreCase),
            "the JWT cookie must be Secure");
    }

    // -----------------------------------------------------------------------
    // POST /api/auth/logout
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_DeletesJwtCookie()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        var setCookieHeaders = response.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .ToList();

        setCookieHeaders.Should().Contain(h =>
            h.Contains("rcv_jwt") && h.Contains("expires=", StringComparison.OrdinalIgnoreCase),
            "logout should expire the JWT cookie");
    }

    [Fact]
    public async Task Logout_SetsCookieWithSecurityFlags()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        var setCookieHeaders = response.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .ToList();

        setCookieHeaders.Should().Contain(h =>
            h.Contains("rcv_jwt") && h.Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "the JWT cookie must always be httpOnly");
        setCookieHeaders.Should().Contain(h =>
            h.Contains("rcv_jwt") && h.Contains("secure", StringComparison.OrdinalIgnoreCase),
            "the JWT cookie must always be Secure");
    }

    [Fact]
    public async Task Logout_CanBeCalledWithoutBeingLoggedIn()
    {
        // Logout should succeed even if no JWT cookie is present
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -----------------------------------------------------------------------
    // GET /api/auth/me
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Me_WhenNotAuthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WhenAuthenticated_ReturnsUserResponse()
    {
        // Arrange: create a test user and a valid JWT for them
        var userId = Guid.NewGuid();
        var testUser = new User
        {
            Id = userId,
            ExternalId = "google-test-123",
            Provider = "Google",
            Email = "me@example.com",
            DisplayName = "Integration Test User",
            CreatedAt = DateTime.UtcNow,
        };

        var client = _factory.WithUser(testUser).CreateClient();
        var jwt = _factory.CreateJwtForUser(testUser);
        client.DefaultRequestHeaders.Add("Cookie", $"rcv_jwt={jwt}");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var userResponse = JsonSerializer.Deserialize<UserResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        userResponse.Should().NotBeNull();
        userResponse!.Id.Should().Be(userId);
        userResponse.Email.Should().Be("me@example.com");
        userResponse.DisplayName.Should().Be("Integration Test User");
        userResponse.Provider.Should().Be("Google");
    }

    [Fact]
    public async Task Me_WithExpiredJwt_Returns401()
    {
        var expiredJwt = _factory.CreateExpiredJwtForUser(new User
        {
            Id = Guid.NewGuid(),
            ExternalId = "x",
            Provider = "Google",
            Email = "expired@example.com",
            DisplayName = "Expired User",
            CreatedAt = DateTime.UtcNow,
        });

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"rcv_jwt={expiredJwt}");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the real
/// SQL Server database with an in-memory one and provides helpers for injecting
/// test users and JWTs.
/// </summary>
public class AuthApiFactory : WebApplicationFactory<Program>
{
    // Shared test JWT settings – must match what the test server is configured with
    internal const string TestSecretKey = "test-secret-key-that-is-long-enough-32-chars!";
    internal const string TestIssuer = "TestIssuer";
    internal const string TestAudience = "TestAudience";

    private User? _testUser;
    private FakeExternalAuthOptions? _fakeExternalAuth;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Override JWT config so tokens created in tests are valid
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = TestSecretKey,
                ["Authentication:Jwt:Issuer"] = TestIssuer,
                ["Authentication:Jwt:Audience"] = TestAudience,
                ["Authentication:Jwt:ExpirationDays"] = "7",
                // Fake OAuth credentials so the providers configure without errors
                ["Authentication:Google:ClientId"] = "test-google-client-id",
                ["Authentication:Google:ClientSecret"] = "test-google-client-secret",
                ["Authentication:Microsoft:ClientId"] = "test-microsoft-client-id",
                ["Authentication:Microsoft:ClientSecret"] = "test-microsoft-client-secret",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server context with in-memory.
            // EF Core 9 registers both DbContextOptions<T> and IDbContextOptionsConfiguration<T>;
            // both must be removed or the SqlServer and InMemory providers will conflict.
            services.RemoveAll<DbContextOptions<RcvDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<RcvDbContext>));
            services.AddDbContext<RcvDbContext>(options =>
                options.UseInMemoryDatabase("AuthControllerTests"));

            // Seed the test user if one has been configured via WithUser()
            if (_testUser is not null)
            {
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();
                db.Users.Add(_testUser);
                db.SaveChanges();
            }

            // If a fake external auth is configured, replace the "External" cookie scheme
            // with a test handler that immediately succeeds with preset claims.
            if (_fakeExternalAuth is not null)
            {
                var fakeAuth = _fakeExternalAuth;

                // PostConfigure runs after ALL Configure callbacks. We modify the
                // existing "External" scheme builder in-place so we don't touch the
                // internal _schemes list (SchemeMap.Remove would leave a stale entry there).
                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    if (opts.SchemeMap.TryGetValue("External", out var builder))
                        builder.HandlerType = typeof(FakeExternalAuthHandler);
                });
                services.AddTransient<FakeExternalAuthHandler>();
                services.Configure<FakeExternalAuthOptions>("External", opts =>
                {
                    opts.ExternalId = fakeAuth.ExternalId;
                    opts.Email = fakeAuth.Email;
                    opts.DisplayName = fakeAuth.DisplayName;
                });
            }
        });
    }

    /// <summary>
    /// Returns a factory variant that has the given user pre-seeded in the test database.
    /// </summary>
    public AuthApiFactory WithUser(User user)
    {
        _testUser = user;
        return this;
    }

    /// <summary>
    /// Returns a factory variant that injects a fake External auth handler,
    /// simulating a successful OAuth callback without a real provider.
    /// </summary>
    public AuthApiFactory WithFakeExternalAuth(string externalId, string email, string displayName)
    {
        _fakeExternalAuth = new FakeExternalAuthOptions
        {
            ExternalId = externalId,
            Email = email,
            DisplayName = displayName,
        };
        return this;
    }

    /// <summary>
    /// Generates a valid JWT for the given user using the same settings the test server uses.
    /// </summary>
    public string CreateJwtForUser(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.DisplayName ?? string.Empty),
            new Claim("provider", user.Provider),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an already-expired JWT for testing 401 responses on <c>/me</c>.
    /// </summary>
    public string CreateExpiredJwtForUser(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.DisplayName ?? string.Empty),
            new Claim("provider", user.Provider),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddDays(-10),
            expires: DateTime.UtcNow.AddDays(-1), // expired yesterday
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// -----------------------------------------------------------------------
// Fake External Authentication Handler
// Used to simulate a successful OAuth callback in tests.
// -----------------------------------------------------------------------

/// <summary>Options for the fake External authentication handler.</summary>
public class FakeExternalAuthOptions : AuthenticationSchemeOptions
{
    public string ExternalId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>
/// A test-only authentication handler that always succeeds for the "External" scheme,
/// returning preset claims. This lets us test <c>Callback</c> without a real OAuth provider.
/// Also implements <see cref="IAuthenticationSignOutHandler"/> so <c>SignOutAsync("External")</c>
/// in the controller succeeds (as a no-op) during testing.
/// </summary>
public class FakeExternalAuthHandler : AuthenticationHandler<FakeExternalAuthOptions>, IAuthenticationSignOutHandler
{
    public FakeExternalAuthHandler(
        IOptionsMonitor<FakeExternalAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Options.ExternalId),
            new Claim(ClaimTypes.Email, Options.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, Options.DisplayName ?? string.Empty),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>No-op sign-out so the controller's SignOutAsync("External") succeeds in tests.</summary>
    public Task SignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;
}
