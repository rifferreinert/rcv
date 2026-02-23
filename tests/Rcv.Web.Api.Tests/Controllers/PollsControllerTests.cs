using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Models.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Rcv.Web.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="Rcv.Web.Api.Controllers.PollsController"/>.
/// Each test creates a fresh <see cref="PollsApiFactory"/> with an isolated in-memory
/// database to prevent cross-test state pollution.
/// </summary>
public class PollsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // -----------------------------------------------------------------------
    // POST /api/polls
    // -----------------------------------------------------------------------

    /// <summary>
    /// Posting a valid <see cref="CreatePollRequest"/> while authenticated should create
    /// the poll and return 201 Created with the new poll in the response body.
    /// </summary>
    [Fact]
    public async Task CreatePoll_WhenAuthenticated_Returns201WithPollResponse()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CreatePollRequest
        {
            Title = "Best programming language?",
            Options = new List<string> { "C#", "Python", "TypeScript" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/polls", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "creating a poll while authenticated should return 201 Created");

        var poll = await DeserializeAsync<PollResponse>(response);
        poll.Should().NotBeNull();
        poll!.Id.Should().NotBe(Guid.Empty);
        poll.Title.Should().Be("Best programming language?");
        poll.Status.Should().Be("Active");
    }

    /// <summary>
    /// Posting without a JWT cookie should be rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task CreatePoll_WhenUnauthenticated_Returns401()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var client = factory.CreateUnauthenticatedClient();

        var request = new CreatePollRequest
        {
            Title = "Test Poll",
            Options = new List<string> { "Option A", "Option B" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/polls", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "creating a poll without authentication should return 401");
    }

    /// <summary>
    /// A poll with only one option violates the minimum-2-options rule and
    /// should return 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreatePoll_WithInvalidRequest_Returns400()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CreatePollRequest
        {
            Title = "Invalid Poll",
            Options = new List<string> { "Only One Option" } // requires at least 2
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/polls", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "a poll with fewer than 2 options must fail validation");
    }

    // -----------------------------------------------------------------------
    // GET /api/polls/{id}
    // -----------------------------------------------------------------------

    /// <summary>
    /// Fetching a poll that exists in the database should return 200 OK with the
    /// full poll payload.
    /// </summary>
    [Fact]
    public async Task GetPoll_ForExistingPoll_Returns200WithPollResponse()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Favourite season?");
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/polls/{pollId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "fetching a known poll should return 200 OK");

        var poll = await DeserializeAsync<PollResponse>(response);
        poll.Should().NotBeNull();
        poll!.Id.Should().Be(pollId);
        poll.Title.Should().Be("Favourite season?");
    }

    /// <summary>
    /// Requesting a poll that does not exist should return 404 Not Found.
    /// </summary>
    [Fact]
    public async Task GetPoll_ForUnknownId_Returns404()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/polls/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a poll that does not exist should return 404");
    }

    // -----------------------------------------------------------------------
    // GET /api/polls
    // -----------------------------------------------------------------------

    /// <summary>
    /// Listing polls should return 200 OK with a paginated response containing
    /// at least as many items as were seeded.
    /// </summary>
    [Fact]
    public async Task ListPolls_Returns200WithPaginatedList()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        await factory.SeedPollAsync(user.Id, "Poll One");
        await factory.SeedPollAsync(user.Id, "Poll Two");
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/polls");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "listing polls should return 200 OK");

        var list = await DeserializeAsync<PollListResponse>(response);
        list.Should().NotBeNull();
        list!.Items.Should().HaveCountGreaterThanOrEqualTo(2,
            "both seeded polls should appear in the list");
    }

    /// <summary>
    /// When a <c>creatorId</c> query parameter is supplied, only that user's polls
    /// should be returned.
    /// </summary>
    [Fact]
    public async Task ListPolls_WithCreatorIdFilter_ReturnsOnlyCreatorPolls()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var userA = MakeUser(Guid.NewGuid(), "a@test.com", "User A");
        var userB = MakeUser(Guid.NewGuid(), "b@test.com", "User B");
        await factory.SeedUserAsync(userA);
        await factory.SeedUserAsync(userB);

        await factory.SeedPollAsync(userA.Id, "User A's Poll");
        await factory.SeedPollAsync(userB.Id, "User B's Poll");

        var client = factory.CreateUnauthenticatedClient();

        // Act — filter by User A's ID only
        var response = await client.GetAsync($"/api/polls?creatorId={userA.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await DeserializeAsync<PollListResponse>(response);
        list.Should().NotBeNull();
        list!.Items.Should().OnlyContain(p => p.Creator.Id == userA.Id,
            "filtering by creatorId must exclude other users' polls");
    }

    // -----------------------------------------------------------------------
    // PUT /api/polls/{id}
    // -----------------------------------------------------------------------

    /// <summary>
    /// The poll creator updating their own poll should receive 200 OK with the
    /// updated poll data.
    /// </summary>
    [Fact]
    public async Task UpdatePoll_ByCreator_Returns200WithUpdatedPoll()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Original Title");
        var client = factory.CreateAuthenticatedClient(user);

        var updateRequest = new UpdatePollRequest { Title = "Updated Title" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/polls/{pollId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the creator updating their own poll should return 200 OK");

        var poll = await DeserializeAsync<PollResponse>(response);
        poll.Should().NotBeNull();
        poll!.Title.Should().Be("Updated Title");
    }

    /// <summary>
    /// A user who did not create a poll must not be allowed to update it; the
    /// server should respond with 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdatePoll_ByNonCreator_Returns403()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var userA = MakeUser(Guid.NewGuid(), "a@test.com", "User A");
        var userB = MakeUser(Guid.NewGuid(), "b@test.com", "User B");
        await factory.SeedUserAsync(userA);
        await factory.SeedUserAsync(userB);

        var pollId = await factory.SeedPollAsync(userA.Id, "User A's Poll");

        // User B tries to update User A's poll
        var clientB = factory.CreateAuthenticatedClient(userB);

        // Act
        var response = await clientB.PutAsJsonAsync(
            $"/api/polls/{pollId}",
            new UpdatePollRequest { Title = "Hijacked Title" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the poll creator may update a poll");
    }

    // -----------------------------------------------------------------------
    // DELETE /api/polls/{id}
    // -----------------------------------------------------------------------

    /// <summary>
    /// The poll creator deleting their own poll should receive 204 No Content.
    /// </summary>
    [Fact]
    public async Task DeletePoll_ByCreator_Returns204()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Delete");
        var client = factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.DeleteAsync($"/api/polls/{pollId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "the creator deleting their own poll should return 204 No Content");
    }

    /// <summary>
    /// A user who did not create a poll must not be allowed to delete it; the
    /// server should respond with 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task DeletePoll_ByNonCreator_Returns403()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var userA = MakeUser(Guid.NewGuid(), "a@test.com", "User A");
        var userB = MakeUser(Guid.NewGuid(), "b@test.com", "User B");
        await factory.SeedUserAsync(userA);
        await factory.SeedUserAsync(userB);

        var pollId = await factory.SeedPollAsync(userA.Id, "User A's Poll");

        // User B tries to delete User A's poll
        var clientB = factory.CreateAuthenticatedClient(userB);

        // Act
        var response = await clientB.DeleteAsync($"/api/polls/{pollId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the poll creator may delete a poll");
    }

    // -----------------------------------------------------------------------
    // POST /api/polls/{id}/close
    // -----------------------------------------------------------------------

    /// <summary>
    /// The poll creator closing their own active poll should receive 200 OK with
    /// the poll's status set to "Closed".
    /// </summary>
    [Fact]
    public async Task ClosePoll_ByCreator_Returns200WithClosedStatus()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Close");
        var client = factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.PostAsync($"/api/polls/{pollId}/close", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the creator closing their own poll should return 200 OK");

        var poll = await DeserializeAsync<PollResponse>(response);
        poll.Should().NotBeNull();
        poll!.Status.Should().Be("Closed",
            "the poll status should be 'Closed' after the close action");
    }

    /// <summary>
    /// A user who did not create a poll must not be allowed to close it; the
    /// server should respond with 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task ClosePoll_ByNonCreator_Returns403()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var userA = MakeUser(Guid.NewGuid(), "a@test.com", "User A");
        var userB = MakeUser(Guid.NewGuid(), "b@test.com", "User B");
        await factory.SeedUserAsync(userA);
        await factory.SeedUserAsync(userB);

        var pollId = await factory.SeedPollAsync(userA.Id, "User A's Poll");

        // User B tries to close User A's poll
        var clientB = factory.CreateAuthenticatedClient(userB);

        // Act
        var response = await clientB.PostAsync($"/api/polls/{pollId}/close", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "only the poll creator may close a poll");
    }

    // -----------------------------------------------------------------------
    // DELETE /api/polls/{id} — unauthenticated / not found
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sending DELETE without a JWT cookie should be rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task DeletePoll_WhenUnauthenticated_Returns401()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Delete Unauth");
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.DeleteAsync($"/api/polls/{pollId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "deleting a poll without authentication should return 401");
    }

    /// <summary>
    /// Attempting to delete a poll that does not exist should return 404 Not Found.
    /// </summary>
    [Fact]
    public async Task DeletePoll_ForNonExistentPoll_Returns404()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.DeleteAsync($"/api/polls/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "deleting a non-existent poll should return 404");
    }

    // -----------------------------------------------------------------------
    // PUT /api/polls/{id} — unauthenticated / not found
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sending PUT without a JWT cookie should be rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdatePoll_WhenUnauthenticated_Returns401()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Update Unauth");
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/polls/{pollId}",
            new UpdatePollRequest { Title = "Should Fail" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "updating a poll without authentication should return 401");
    }

    /// <summary>
    /// Attempting to update a poll that does not exist should return 404 Not Found.
    /// </summary>
    [Fact]
    public async Task UpdatePoll_ForNonExistentPoll_Returns404()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/polls/{Guid.NewGuid()}",
            new UpdatePollRequest { Title = "Ghost Poll" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "updating a non-existent poll should return 404");
    }

    // -----------------------------------------------------------------------
    // POST /api/polls/{id}/close — unauthenticated / not found / already closed
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sending POST /close without a JWT cookie should be rejected with 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task ClosePoll_WhenUnauthenticated_Returns401()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Close Unauth");
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync($"/api/polls/{pollId}/close", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "closing a poll without authentication should return 401");
    }

    /// <summary>
    /// Attempting to close a poll that does not exist should return 404 Not Found.
    /// </summary>
    [Fact]
    public async Task ClosePoll_ForNonExistentPoll_Returns404()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        // Act
        var response = await client.PostAsync($"/api/polls/{Guid.NewGuid()}/close", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "closing a non-existent poll should return 404");
    }

    /// <summary>
    /// Attempting to close a poll that is already closed should return 409 Conflict.
    /// </summary>
    [Fact]
    public async Task ClosePoll_AlreadyClosed_Returns409()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Close Twice");
        var client = factory.CreateAuthenticatedClient(user);

        // Close the poll once — should succeed.
        var firstClose = await client.PostAsync($"/api/polls/{pollId}/close", content: null);
        firstClose.StatusCode.Should().Be(HttpStatusCode.OK,
            "the first close should succeed");

        // Act — attempt to close the already-closed poll.
        var response = await client.PostAsync($"/api/polls/{pollId}/close", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "closing an already-closed poll should return 409 Conflict");
    }

    // -----------------------------------------------------------------------
    // GET /api/polls/{id} — soft-deleted poll
    // -----------------------------------------------------------------------

    /// <summary>
    /// Fetching a poll that has been soft-deleted (Status = "Deleted") should
    /// return 404 Not Found, as deleted polls are invisible to the public API.
    /// </summary>
    [Fact]
    public async Task GetPoll_ForDeletedPoll_Returns404()
    {
        // Arrange
        await using var factory = new PollsApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var pollId = await factory.SeedPollAsync(user.Id, "Poll to Soft Delete");

        // Directly mark the poll as deleted in the in-memory database.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();
        var poll = await db.Polls.FindAsync(pollId);
        poll!.Status = "Deleted";
        await db.SaveChangesAsync();

        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/polls/{pollId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a soft-deleted poll should not be visible and must return 404");
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reads the HTTP response body and deserialises it to <typeparamref name="T"/>.
    /// </summary>
    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    /// <summary>
    /// Creates a minimal <see cref="User"/> with the supplied identity values.
    /// </summary>
    private static User MakeUser(
        Guid id,
        string email = "test@example.com",
        string displayName = "Test User") => new()
    {
        Id = id,
        ExternalId = $"ext-{id}",
        Provider = "Google",
        Email = email,
        DisplayName = displayName,
        CreatedAt = DateTime.UtcNow,
    };
}

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for polls integration tests.
/// Each instance uses a unique in-memory database so tests remain fully isolated.
/// </summary>
public class PollsApiFactory : WebApplicationFactory<Program>
{
    // Shared test JWT settings – must match what the test server is configured with.
    // These are intentionally the same values used by <see cref="AuthApiFactory"/>
    // so both test suites can share the same application configuration.
    internal const string TestSecretKey = "test-secret-key-that-is-long-enough-32-chars!";
    internal const string TestIssuer = "TestIssuer";
    internal const string TestAudience = "TestAudience";

    // Unique name per factory instance guarantees DB isolation between parallel tests.
    private readonly string _dbName = $"PollsControllerTests_{Guid.NewGuid()}";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Override JWT config so tokens minted in tests are accepted by the server.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = TestSecretKey,
                ["Authentication:Jwt:Issuer"] = TestIssuer,
                ["Authentication:Jwt:Audience"] = TestAudience,
                ["Authentication:Jwt:ExpirationDays"] = "7",
                // Fake OAuth credentials so providers configure without errors.
                ["Authentication:Google:ClientId"] = "test-google-client-id",
                ["Authentication:Google:ClientSecret"] = "test-google-client-secret",
                ["Authentication:Microsoft:ClientId"] = "test-microsoft-client-id",
                ["Authentication:Microsoft:ClientSecret"] = "test-microsoft-client-secret",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the production SQL Server context with an isolated in-memory one.
            // EF Core 9 registers both DbContextOptions<T> and IDbContextOptionsConfiguration<T>;
            // both must be removed to avoid a provider conflict.
            services.RemoveAll<DbContextOptions<RcvDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<RcvDbContext>));
            services.AddDbContext<RcvDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    // -----------------------------------------------------------------------
    // Client helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with the given user's JWT set as the
    /// <c>rcv_jwt</c> httpOnly cookie, matching the production cookie name.
    /// </summary>
    /// <param name="user">The user whose identity should be embedded in the token.</param>
    public HttpClient CreateAuthenticatedClient(User user)
    {
        var client = CreateClient();
        var jwt = CreateJwtForUser(user);
        client.DefaultRequestHeaders.Add("Cookie", $"rcv_jwt={jwt}");
        return client;
    }

    /// <summary>Creates an <see cref="HttpClient"/> with no authentication credentials.</summary>
    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    // -----------------------------------------------------------------------
    // JWT helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Generates a valid JWT for the given user using the same signing key and
    /// claims structure that the application expects.
    /// </summary>
    /// <param name="user">The user for whom to generate the token.</param>
    /// <returns>A signed JWT string.</returns>
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

    // -----------------------------------------------------------------------
    // Database seeding helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Seeds a <see cref="User"/> directly into the in-memory database.
    /// Triggers host initialisation on the first call so that
    /// <see cref="WebApplicationFactory{T}.Services"/> is available.
    /// </summary>
    /// <param name="user">The user entity to persist.</param>
    public async Task SeedUserAsync(User user)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a <see cref="Poll"/> with two default options directly into the
    /// in-memory database, bypassing the API layer.  The creator must already
    /// be seeded via <see cref="SeedUserAsync"/> before calling this method.
    /// </summary>
    /// <param name="creatorId">The ID of the user who owns the poll.</param>
    /// <param name="title">The poll title.</param>
    /// <returns>The newly-created poll's ID.</returns>
    public async Task<Guid> SeedPollAsync(Guid creatorId, string title = "Test Poll")
    {
        var pollId = Guid.NewGuid();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();

        db.Polls.Add(new Poll
        {
            Id = pollId,
            Title = title,
            CreatorId = creatorId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            Options = new List<PollOption>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OptionText = "Option A",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    OptionText = "Option B",
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                },
            },
        });

        await db.SaveChangesAsync();
        return pollId;
    }
}
