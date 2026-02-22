using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rcv.Web.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuthService"/>. Uses an in-memory database
/// so no real SQL Server connection is required.
/// </summary>
public class AuthServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an isolated in-memory <see cref="RcvDbContext"/> for each test.
    /// Each test gets its own database name to prevent cross-test data leakage.
    /// </summary>
    private static RcvDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RcvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RcvDbContext(options);
    }

    /// <summary>
    /// Creates an <see cref="IConfiguration"/> with test JWT settings.
    /// The secret key must be long enough for HMAC-SHA256 (≥32 bytes).
    /// </summary>
    private static IConfiguration CreateTestConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = "test-secret-key-that-is-long-enough-32-chars!",
                ["Authentication:Jwt:Issuer"] = "TestIssuer",
                ["Authentication:Jwt:Audience"] = "TestAudience",
                ["Authentication:Jwt:ExpirationDays"] = "7"
            })
            .Build();

    private static AuthService CreateService(RcvDbContext context) =>
        new AuthService(context, CreateTestConfiguration());

    // -----------------------------------------------------------------------
    // GetOrCreateUserAsync – user creation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserDoesNotExist_CreatesNewUser()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        user.Should().NotBeNull();
        user.ExternalId.Should().Be("google-123");
        user.Provider.Should().Be("Google");
        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        context.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserDoesNotExist_SetsCreatedAt()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var before = DateTime.UtcNow;
        var user = await service.GetOrCreateUserAsync("google-123", "Google", null, null);
        var after = DateTime.UtcNow;

        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    // -----------------------------------------------------------------------
    // GetOrCreateUserAsync – existing user lookup
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateUserAsync_WhenUserExists_ReturnsExistingUserWithoutCreatingDuplicate()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var first = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");
        var second = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        second.Id.Should().Be(first.Id);
        context.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_WithSameExternalIdButDifferentProvider_CreatesSeparateUsers()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        await service.GetOrCreateUserAsync("user-123", "Google", "test@google.com", "Google User");
        await service.GetOrCreateUserAsync("user-123", "Microsoft", "test@microsoft.com", "Microsoft User");

        context.Users.Should().HaveCount(2);
    }

    // -----------------------------------------------------------------------
    // GetOrCreateUserAsync – LastLoginAt
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateUserAsync_SetsLastLoginAtOnFirstLogin()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var before = DateTime.UtcNow;
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");
        var after = DateTime.UtcNow;

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UpdatesLastLoginAtOnSubsequentLogins()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var first = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");
        var initialLoginAt = first.LastLoginAt;

        // Small delay to ensure time difference is measurable
        await Task.Delay(20);

        var second = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        second.LastLoginAt.Should().BeAfter(initialLoginAt!.Value);
    }

    // -----------------------------------------------------------------------
    // GenerateJwtToken – claims
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GenerateJwtToken_ContainsUserIdClaim()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);
        var claims = ReadJwtClaims(token);

        claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
    }

    [Fact]
    public async Task GenerateJwtToken_ContainsEmailClaim()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);
        var claims = ReadJwtClaims(token);

        claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public async Task GenerateJwtToken_ContainsDisplayNameClaim()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);
        var claims = ReadJwtClaims(token);

        claims.Should().Contain(c =>
            c.Type == ClaimTypes.Name && c.Value == "Test User");
    }

    [Fact]
    public async Task GenerateJwtToken_TokenIsSignedAndVerifiable()
    {
        using var context = CreateInMemoryContext();
        var config = CreateTestConfiguration();
        var service = new AuthService(context, config);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);

        // Validate the token signature, issuer, audience, and lifetime
        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Authentication:Jwt:SecretKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = config["Authentication:Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["Authentication:Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        var act = () => handler.ValidateToken(token, validationParams, out _);
        act.Should().NotThrow("the token should be valid and correctly signed");
    }

    [Fact]
    public async Task GenerateJwtToken_ContainsProviderClaim()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);
        var claims = ReadJwtClaims(token);

        claims.Should().Contain(c => c.Type == "provider" && c.Value == "Google");
    }

    [Fact]
    public async Task GenerateJwtToken_ExpiresAfterConfiguredDays()
    {
        using var context = CreateInMemoryContext();
        var config = CreateTestConfiguration(); // ExpirationDays = 7
        var service = new AuthService(context, config);
        var user = await service.GetOrCreateUserAsync("google-123", "Google", "test@example.com", "Test User");

        var token = service.GenerateJwtToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddDays(7);

        // Allow a 30-second window for test execution time
        jwt.ValidTo.Should().BeCloseTo(expectedExpiry, precision: TimeSpan.FromSeconds(30));
    }

    // -----------------------------------------------------------------------
    // GenerateJwtToken – missing configuration
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateJwtToken_ThrowsWhenSecretKeyMissing()
    {
        using var context = CreateInMemoryContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "TestIssuer",
                ["Authentication:Jwt:Audience"] = "TestAudience",
                // SecretKey deliberately omitted
            })
            .Build();
        var service = new AuthService(context, config);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "x", Provider = "Google" };

        var act = () => service.GenerateJwtToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*");
    }

    [Fact]
    public void GenerateJwtToken_ThrowsWhenIssuerMissing()
    {
        using var context = CreateInMemoryContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = "test-secret-key-that-is-long-enough-32-chars!",
                ["Authentication:Jwt:Audience"] = "TestAudience",
                // Issuer deliberately omitted
            })
            .Build();
        var service = new AuthService(context, config);
        var user = new User { Id = Guid.NewGuid(), ExternalId = "x", Provider = "Google" };

        var act = () => service.GenerateJwtToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Issuer*");
    }

    // -----------------------------------------------------------------------
    // GetOrCreateUserAsync – null profile fields
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetOrCreateUserAsync_WithNullEmailAndDisplayName_SavesNullValues()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var user = await service.GetOrCreateUserAsync("google-123", "Google", null, null);

        user.Email.Should().BeNull();
        user.DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateUserAsync_DoesNotOverwriteEmailOrDisplayNameOnReLogin()
    {
        // The implementation intentionally does NOT update Email/DisplayName on re-login.
        // Only LastLoginAt is refreshed. This test pins that contract.
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        await service.GetOrCreateUserAsync("google-123", "Google", "original@example.com", "Original Name");
        var updated = await service.GetOrCreateUserAsync("google-123", "Google", "new@example.com", "New Name");

        updated.Email.Should().Be("original@example.com");
        updated.DisplayName.Should().Be("Original Name");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IEnumerable<Claim> ReadJwtClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }
}
