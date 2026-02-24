using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;

namespace Rcv.Web.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ResultsService"/>. Uses an in-memory database
/// so no real SQL Server connection is required.
/// </summary>
public class ResultsServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static RcvDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RcvDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RcvDbContext(options);
    }

    private static IMemoryCache CreateCache() =>
        new MemoryCache(new MemoryCacheOptions());

    private static async Task<User> CreateTestUser(RcvDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            Provider = "Google",
            Email = "voter@example.com",
            DisplayName = "Test Voter",
            CreatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<(Poll Poll, List<PollOption> Options)> CreateTestPoll(
        RcvDbContext context,
        Guid creatorId,
        int optionCount = 3,
        string status = "Active")
    {
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Test Poll",
            CreatorId = creatorId,
            Status = status,
            IsResultsPublic = true,
            CreatedAt = DateTime.UtcNow,
        };

        var options = Enumerable.Range(0, optionCount)
            .Select(i => new PollOption
            {
                Id = Guid.NewGuid(),
                PollId = poll.Id,
                OptionText = $"Option {(char)('A' + i)}",
                DisplayOrder = i,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        context.Polls.Add(poll);
        context.PollOptions.AddRange(options);
        await context.SaveChangesAsync();
        return (poll, options);
    }

    private static async Task CastVote(RcvDbContext context, Guid pollId, Guid voterId, List<Guid> rankedOptionIds)
    {
        context.Votes.Add(new Vote
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            VoterId = voterId,
            RankedChoices = rankedOptionIds,
            CastAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();
    }

    private static IResultsService CreateService(RcvDbContext context, IMemoryCache? cache = null) =>
        new ResultsService(context, cache ?? CreateCache());

    // -----------------------------------------------------------------------
    // CalculateResultsAsync — happy paths
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CalculateResultsAsync_ClearMajorityWinner_ReturnsWinner()
    {
        using var context = CreateInMemoryContext();
        var userA = await CreateTestUser(context);
        var userB = await CreateTestUser(context);
        var userC = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, userA.Id);

        // All three voters rank Option A first → clear majority
        await CastVote(context, poll.Id, userA.Id, new List<Guid> { options[0].Id, options[1].Id, options[2].Id });
        await CastVote(context, poll.Id, userB.Id, new List<Guid> { options[0].Id, options[2].Id });
        await CastVote(context, poll.Id, userC.Id, new List<Guid> { options[0].Id });

        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.PollId.Should().Be(poll.Id);
        result.Winner.Should().NotBeNull();
        result.Winner!.Id.Should().Be(options[0].Id);
        result.IsTie.Should().BeFalse();
        result.TiedOptions.Should().BeEmpty();
        result.TotalVotes.Should().Be(3);
        result.Rounds.Should().NotBeEmpty();
        result.FinalVoteTotals.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateResultsAsync_MultiRoundElimination_TransfersVotes()
    {
        using var context = CreateInMemoryContext();
        var voters = new List<User>();
        for (int i = 0; i < 5; i++)
            voters.Add(await CreateTestUser(context));
        var (poll, options) = await CreateTestPoll(context, voters[0].Id);

        // 5 voters, 3 options: A gets 2, C gets 2, B gets 1 → B eliminated
        // B's voter ranked A second → A gets 3 votes → A wins in round 2
        await CastVote(context, poll.Id, voters[0].Id, new List<Guid> { options[0].Id, options[1].Id, options[2].Id });
        await CastVote(context, poll.Id, voters[1].Id, new List<Guid> { options[0].Id, options[2].Id });
        await CastVote(context, poll.Id, voters[2].Id, new List<Guid> { options[1].Id, options[0].Id, options[2].Id });
        await CastVote(context, poll.Id, voters[3].Id, new List<Guid> { options[2].Id, options[0].Id });
        await CastVote(context, poll.Id, voters[4].Id, new List<Guid> { options[2].Id, options[1].Id });

        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.TotalVotes.Should().Be(5);
        // Should have at least 2 rounds (initial + at least one elimination)
        result.Rounds.Count.Should().BeGreaterThanOrEqualTo(2);
        // First round should have vote counts for all 3 options
        result.Rounds[0].VoteCounts.Should().HaveCount(3);
    }

    [Fact]
    public async Task CalculateResultsAsync_TieScenario_ReturnsTiedOptions()
    {
        using var context = CreateInMemoryContext();
        var userA = await CreateTestUser(context);
        var userB = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, userA.Id, optionCount: 2);

        // Two voters, each ranks a different option first → tie
        await CastVote(context, poll.Id, userA.Id, new List<Guid> { options[0].Id });
        await CastVote(context, poll.Id, userB.Id, new List<Guid> { options[1].Id });

        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.TotalVotes.Should().Be(2);
        result.IsTie.Should().BeTrue();
        result.Winner.Should().BeNull();
        result.TiedOptions.Should().HaveCount(2);
        result.TiedOptions.Select(o => o.Id).Should()
            .BeEquivalentTo(new[] { options[0].Id, options[1].Id });
    }

    [Fact]
    public async Task CalculateResultsAsync_NoVotes_ReturnsEmptyResult()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, _) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.PollId.Should().Be(poll.Id);
        result.Winner.Should().BeNull();
        result.IsTie.Should().BeFalse();
        result.TiedOptions.Should().BeEmpty();
        result.Rounds.Should().BeEmpty();
        result.FinalVoteTotals.Should().BeEmpty();
        result.TotalVotes.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // CalculateResultsAsync — error cases
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CalculateResultsAsync_UnknownPoll_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var act = async () => await service.CalculateResultsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CalculateResultsAsync_DeletedPoll_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, _) = await CreateTestPoll(context, user.Id, status: "Deleted");
        var service = CreateService(context);

        var act = async () => await service.CalculateResultsAsync(poll.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // -----------------------------------------------------------------------
    // Caching
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CalculateResultsAsync_CachesResult_OnFirstCall()
    {
        using var context = CreateInMemoryContext();
        var cache = CreateCache();
        var user = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, user.Id);
        await CastVote(context, poll.Id, user.Id, new List<Guid> { options[0].Id });

        var service = CreateService(context, cache);

        var result = await service.CalculateResultsAsync(poll.Id);

        // Verify the cache was populated
        cache.TryGetValue($"results:{poll.Id}", out ResultResponse? cached).Should().BeTrue();
        cached.Should().BeSameAs(result);
    }

    [Fact]
    public async Task CalculateResultsAsync_ReturnsCachedResult_OnSecondCall()
    {
        using var context = CreateInMemoryContext();
        var cache = CreateCache();
        var user = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, user.Id);
        await CastVote(context, poll.Id, user.Id, new List<Guid> { options[0].Id });

        var service = CreateService(context, cache);

        var first = await service.CalculateResultsAsync(poll.Id);
        var second = await service.CalculateResultsAsync(poll.Id);

        // Should be the exact same cached object reference
        second.Should().BeSameAs(first);
    }

    // -----------------------------------------------------------------------
    // Result mapping correctness
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CalculateResultsAsync_MapsRoundData_Correctly()
    {
        using var context = CreateInMemoryContext();
        var userA = await CreateTestUser(context);
        var userB = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, userA.Id, optionCount: 2);

        // Clear winner: both vote for Option A
        await CastVote(context, poll.Id, userA.Id, new List<Guid> { options[0].Id });
        await CastVote(context, poll.Id, userB.Id, new List<Guid> { options[0].Id, options[1].Id });

        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.Rounds.Should().NotBeEmpty();
        result.Rounds[0].RoundNumber.Should().Be(1);
        result.Rounds[0].VoteCounts.Should().ContainKey(options[0].Id);
        result.FinalVoteTotals.Should().ContainKey(options[0].Id);
    }

    [Fact]
    public async Task CalculateResultsAsync_ClosedPoll_ReturnsResults()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, options) = await CreateTestPoll(context, user.Id, status: "Closed");
        // Manually add a vote (since voting service would block closed polls)
        await CastVote(context, poll.Id, user.Id, new List<Guid> { options[0].Id });

        var service = CreateService(context);

        var result = await service.CalculateResultsAsync(poll.Id);

        result.TotalVotes.Should().Be(1);
        result.Winner.Should().NotBeNull();
    }
}
