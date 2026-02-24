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
/// Integration tests for <see cref="Rcv.Web.Api.Controllers.VotesController"/>.
/// Each test creates a fresh <see cref="VotesApiFactory"/> with an isolated in-memory
/// database to prevent cross-test state pollution.
/// </summary>
public class VotesControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    // -----------------------------------------------------------------------
    // POST /api/polls/{pollId}/votes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CastVote_WhenAuthenticated_Returns201()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CastVoteRequest { RankedOptionIds = optionIds };

        var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "casting a vote while authenticated should return 201 Created");

        var vote = await DeserializeAsync<VoteResponse>(response);
        vote.Should().NotBeNull();
        vote!.PollId.Should().Be(pollId);
        vote.RankedChoices.Should().BeEquivalentTo(optionIds, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task CastVote_WhenUnauthenticated_Returns401()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateUnauthenticatedClient();

        var request = new CastVoteRequest { RankedOptionIds = optionIds };

        var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "casting a vote without authentication should return 401");
    }

    [Fact]
    public async Task CastVote_ForClosedPoll_Returns409()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id, status: "Closed");
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CastVoteRequest { RankedOptionIds = optionIds };

        var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "voting on a closed poll should return 409 Conflict");
    }

    [Fact]
    public async Task CastVote_ForNonExistentPoll_Returns404()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CastVoteRequest { RankedOptionIds = new List<Guid> { Guid.NewGuid() } };

        var response = await client.PostAsJsonAsync($"/api/polls/{Guid.NewGuid()}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "voting on a non-existent poll should return 404");
    }

    [Fact]
    public async Task CastVote_WithInvalidOptions_Returns400()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, _) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        // Use an option ID that doesn't belong to this poll
        var request = new CastVoteRequest { RankedOptionIds = new List<Guid> { Guid.NewGuid() } };

        var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "voting with invalid option IDs should return 400");
    }

    [Fact]
    public async Task CastVote_DuplicateVote_Returns200WithUpdatedVote()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        // Cast initial vote
        var firstRequest = new CastVoteRequest { RankedOptionIds = optionIds };
        var firstResponse = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Cast updated vote with reversed order
        var reversed = new List<Guid>(optionIds);
        reversed.Reverse();
        var secondRequest = new CastVoteRequest { RankedOptionIds = reversed };
        var secondResponse = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", secondRequest);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "updating an existing vote should return 200 OK");

        var vote = await DeserializeAsync<VoteResponse>(secondResponse);
        vote.Should().NotBeNull();
        vote!.RankedChoices.Should().BeEquivalentTo(reversed, opts => opts.WithStrictOrdering());
        vote.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CastVote_WithEmptyRankedOptionIds_Returns400()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, _) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        var request = new CastVoteRequest { RankedOptionIds = new List<Guid>() };

        var response = await client.PostAsJsonAsync($"/api/polls/{pollId}/votes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an empty ranked options list must fail validation");
    }

    // -----------------------------------------------------------------------
    // GET /api/polls/{pollId}/votes/me
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyVote_WhenVoteExists_Returns200()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, optionIds) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        // Cast a vote first
        await client.PostAsJsonAsync($"/api/polls/{pollId}/votes",
            new CastVoteRequest { RankedOptionIds = optionIds });

        var response = await client.GetAsync($"/api/polls/{pollId}/votes/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "fetching an existing vote should return 200 OK");

        var vote = await DeserializeAsync<VoteResponse>(response);
        vote.Should().NotBeNull();
        vote!.PollId.Should().Be(pollId);
    }

    [Fact]
    public async Task GetMyVote_WhenNoVote_Returns404()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, _) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateAuthenticatedClient(user);

        var response = await client.GetAsync($"/api/polls/{pollId}/votes/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "fetching a non-existent vote should return 404");
    }

    [Fact]
    public async Task GetMyVote_WhenUnauthenticated_Returns401()
    {
        await using var factory = new VotesApiFactory();
        var user = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(user);
        var (pollId, _) = await factory.SeedPollAsync(user.Id);
        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{pollId}/votes/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "fetching a vote without authentication should return 401");
    }

    // -----------------------------------------------------------------------
    // GET /api/polls/{pollId}/votes/count
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetVoteCount_Returns200WithCounts()
    {
        await using var factory = new VotesApiFactory();
        var userA = MakeUser(Guid.NewGuid(), "a@test.com", "User A");
        var userB = MakeUser(Guid.NewGuid(), "b@test.com", "User B");
        await factory.SeedUserAsync(userA);
        await factory.SeedUserAsync(userB);
        var (pollId, optionIds) = await factory.SeedPollAsync(userA.Id);

        // Cast two votes
        var clientA = factory.CreateAuthenticatedClient(userA);
        await clientA.PostAsJsonAsync($"/api/polls/{pollId}/votes",
            new CastVoteRequest { RankedOptionIds = optionIds });

        var clientB = factory.CreateAuthenticatedClient(userB);
        await clientB.PostAsJsonAsync($"/api/polls/{pollId}/votes",
            new CastVoteRequest { RankedOptionIds = new List<Guid> { optionIds[1], optionIds[0] } });

        // Get count (public endpoint, no auth needed)
        var client = factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync($"/api/polls/{pollId}/votes/count");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "getting vote count should return 200 OK");

        var count = await DeserializeAsync<VoteCountResponse>(response);
        count.Should().NotBeNull();
        count!.TotalVotes.Should().Be(2);
        count.UniqueVoters.Should().Be(2);
    }

    [Fact]
    public async Task GetVoteCount_ForNonExistentPoll_Returns404()
    {
        await using var factory = new VotesApiFactory();
        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{Guid.NewGuid()}/votes/count");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "getting vote count for a non-existent poll should return 404");
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

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
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for votes integration tests.
/// Each instance uses a unique in-memory database so tests remain fully isolated.
/// </summary>
public class VotesApiFactory : WebApplicationFactory<Program>
{
    internal const string TestSecretKey = "test-secret-key-that-is-long-enough-32-chars!";
    internal const string TestIssuer = "TestIssuer";
    internal const string TestAudience = "TestAudience";

    private readonly string _dbName = $"VotesControllerTests_{Guid.NewGuid()}";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:SecretKey"] = TestSecretKey,
                ["Authentication:Jwt:Issuer"] = TestIssuer,
                ["Authentication:Jwt:Audience"] = TestAudience,
                ["Authentication:Jwt:ExpirationDays"] = "7",
                ["Authentication:Google:ClientId"] = "test-google-client-id",
                ["Authentication:Google:ClientSecret"] = "test-google-client-secret",
                ["Authentication:Microsoft:ClientId"] = "test-microsoft-client-id",
                ["Authentication:Microsoft:ClientSecret"] = "test-microsoft-client-secret",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RcvDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<RcvDbContext>));
            services.AddDbContext<RcvDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    // -----------------------------------------------------------------------
    // Client helpers
    // -----------------------------------------------------------------------

    public HttpClient CreateAuthenticatedClient(User user)
    {
        var client = CreateClient();
        var jwt = CreateJwtForUser(user);
        client.DefaultRequestHeaders.Add("Cookie", $"rcv_jwt={jwt}");
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    // -----------------------------------------------------------------------
    // JWT helpers
    // -----------------------------------------------------------------------

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

    public async Task SeedUserAsync(User user)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds a poll with two options. Returns the poll ID and option IDs.
    /// </summary>
    public async Task<(Guid PollId, List<Guid> OptionIds)> SeedPollAsync(
        Guid creatorId,
        string title = "Test Poll",
        string status = "Active")
    {
        var pollId = Guid.NewGuid();
        var optionA = Guid.NewGuid();
        var optionB = Guid.NewGuid();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();

        db.Polls.Add(new Poll
        {
            Id = pollId,
            Title = title,
            CreatorId = creatorId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Options = new List<PollOption>
            {
                new() { Id = optionA, OptionText = "Option A", DisplayOrder = 0, CreatedAt = DateTime.UtcNow },
                new() { Id = optionB, OptionText = "Option B", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
            },
        });

        await db.SaveChangesAsync();
        return (pollId, new List<Guid> { optionA, optionB });
    }
}
