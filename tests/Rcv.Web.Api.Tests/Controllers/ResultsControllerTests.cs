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
using Rcv.Web.Api.Models.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Rcv.Web.Api.Tests.Controllers;

/// <summary>
/// Integration tests for <see cref="Rcv.Web.Api.Controllers.ResultsController"/>.
/// Each test creates a fresh <see cref="ResultsApiFactory"/> with an isolated in-memory
/// database to prevent cross-test state pollution.
/// </summary>
public class ResultsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        };

    // -----------------------------------------------------------------------
    // GET /api/polls/{pollId}/results
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetResults_ForClosedPoll_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, status: "Closed");
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        // Unauthenticated client — anyone can see closed poll results
        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "anyone should be able to see results of a closed poll");

        var result = await DeserializeAsync<ResultsResponse>(response);
        result.Should().NotBeNull();
        result!.PollId.Should().Be(pollId);
        result.TotalVotes.Should().Be(1);
    }

    [Fact]
    public async Task GetResults_AsCreator_ActivePoll_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, isResultsPublic: false);
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        var client = factory.CreateAuthenticatedClient(creator);

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "creator should always be able to see results");
    }

    [Fact]
    public async Task GetResults_AsNonCreator_ActiveNonPublic_Returns403()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var other = MakeUser(Guid.NewGuid(), "other@test.com", "Other");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(other);
        var (pollId, _) = await factory.SeedPollAsync(creator.Id, isResultsPublic: false);

        var client = factory.CreateAuthenticatedClient(other);

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-creator should not see results of non-public active poll");
    }

    [Fact]
    public async Task GetResults_AsNonCreator_ActivePublic_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, isResultsPublic: true);
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        var client = factory.CreateAuthenticatedClient(voter);

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "non-creator should see results of public active poll");
    }

    [Fact]
    public async Task GetResults_UnknownPoll_Returns404()
    {
        await using var factory = new ResultsApiFactory();
        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{Guid.NewGuid()}/results");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetResults_NoVotes_Returns200WithEmptyResult()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(creator);
        var (pollId, _) = await factory.SeedPollAsync(creator.Id, isResultsPublic: true);

        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeAsync<ResultsResponse>(response);
        result.Should().NotBeNull();
        result!.TotalVotes.Should().Be(0);
        result.Winner.Should().BeNull();
        result.IsTie.Should().BeFalse();
    }

    [Fact]
    public async Task GetResults_Unauthenticated_ClosedPoll_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, status: "Closed", isResultsPublic: false);
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "unauthenticated user should see results of closed poll");
    }

    [Fact]
    public async Task GetResults_Unauthenticated_ActivePublicPoll_Returns200()
    {
        // Arrange
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(
            creator.Id,
            status: "Active",
            isResultsPublic: true);
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "an unauthenticated user should be able to see results of an active poll when IsResultsPublic is true");
    }

    [Fact]
    public async Task GetResults_ForClosedPollWithMajority_ReturnsCorrectWinnerAndRounds()
    {
        // Arrange
        await using var factory = new ResultsApiFactory();

        var creator = MakeUser(Guid.NewGuid());
        var voterOne = MakeUser(Guid.NewGuid(), "voter1@test.com", "Voter One");
        var voterTwo = MakeUser(Guid.NewGuid(), "voter2@test.com", "Voter Two");

        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voterOne);
        await factory.SeedUserAsync(voterTwo);

        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, status: "Closed");

        // Both voters rank Option A (optionIds[0]) first — clear majority
        await factory.SeedVoteAsync(pollId, voterOne.Id, optionIds);
        await factory.SeedVoteAsync(pollId, voterTwo.Id, optionIds);

        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeAsync<ResultsResponse>(response);
        result.Should().NotBeNull();
        result!.PollId.Should().Be(pollId);
        result.Winner.Should().NotBeNull("a clear majority exists so a winner must be declared");
        result.IsTie.Should().BeFalse("one option has all first-choice votes so there is no tie");
        result.TotalVotes.Should().Be(2, "exactly two votes were cast");
        result.Rounds.Should().NotBeEmpty("RCV must produce at least one round of counting");
        result.FinalVoteTotals.Should().NotBeEmpty("final vote totals must be recorded");
    }

    // -----------------------------------------------------------------------
    // Live snapshots use the same results endpoint as final results.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLiveResults_WhenPublic_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var voter = MakeUser(Guid.NewGuid(), "voter@test.com", "Voter");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(voter);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, isResultsPublic: true);
        await factory.SeedVoteAsync(pollId, voter.Id, optionIds);

        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "live results should be available when IsResultsPublic is true");
    }

    [Fact]
    public async Task GetLiveResults_WhenNotPublic_NonCreator_Returns403()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        var other = MakeUser(Guid.NewGuid(), "other@test.com", "Other");
        await factory.SeedUserAsync(creator);
        await factory.SeedUserAsync(other);
        var (pollId, _) = await factory.SeedPollAsync(creator.Id, isResultsPublic: false);

        var client = factory.CreateAuthenticatedClient(other);

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "non-creator should not see live results of non-public poll");
    }

    [Fact]
    public async Task GetLiveResults_AsCreator_WhenNotPublic_Returns200()
    {
        await using var factory = new ResultsApiFactory();
        var creator = MakeUser(Guid.NewGuid());
        await factory.SeedUserAsync(creator);
        var (pollId, optionIds) = await factory.SeedPollAsync(creator.Id, isResultsPublic: false);
        await factory.SeedVoteAsync(pollId, creator.Id, optionIds);

        var client = factory.CreateAuthenticatedClient(creator);

        var response = await client.GetAsync($"/api/polls/{pollId}/results");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "creator should always see live results");
    }

    [Fact]
    public async Task GetLiveResults_UnknownPoll_Returns404()
    {
        await using var factory = new ResultsApiFactory();
        var client = factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync($"/api/polls/{Guid.NewGuid()}/results");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for results integration tests.
/// Each instance uses a unique in-memory database so tests remain fully isolated.
/// </summary>
public class ResultsApiFactory : WebApplicationFactory<Program>
{
    internal const string TestSecretKey = "test-secret-key-that-is-long-enough-32-chars!";
    internal const string TestIssuer = "TestIssuer";
    internal const string TestAudience = "TestAudience";

    private readonly string _dbName = $"ResultsControllerTests_{Guid.NewGuid()}";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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
        string status = "Active",
        bool isResultsPublic = true)
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
            Status = Enum.Parse<PollStatus>(status),
            IsResultsPublic = isResultsPublic,
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

    /// <summary>
    /// Seeds a vote for the specified poll and voter.
    /// </summary>
    public async Task SeedVoteAsync(Guid pollId, Guid voterId, List<Guid> rankedOptionIds)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RcvDbContext>();

        db.Votes.Add(new Vote
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            VoterId = voterId,
            RankedChoices = rankedOptionIds,
            CastAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
    }
}
