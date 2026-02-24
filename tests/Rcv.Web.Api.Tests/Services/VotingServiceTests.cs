using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Services;

namespace Rcv.Web.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="VotingService"/>. Uses an in-memory database
/// so no real SQL Server connection is required.
/// </summary>
public class VotingServiceTests
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

    /// <summary>
    /// Creates an active poll with three options and returns the poll + option IDs.
    /// </summary>
    private static async Task<(Poll Poll, List<Guid> OptionIds)> CreateTestPoll(RcvDbContext context, Guid creatorId)
    {
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Test Poll",
            CreatorId = creatorId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        };

        var options = new List<PollOption>
        {
            new() { Id = Guid.NewGuid(), PollId = poll.Id, OptionText = "Option A", DisplayOrder = 0, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PollId = poll.Id, OptionText = "Option B", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PollId = poll.Id, OptionText = "Option C", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
        };

        context.Polls.Add(poll);
        context.PollOptions.AddRange(options);
        await context.SaveChangesAsync();

        return (poll, options.Select(o => o.Id).ToList());
    }

    private static IVotingService CreateService(RcvDbContext context) =>
        new VotingService(context);

    // -----------------------------------------------------------------------
    // CastVoteAsync — happy paths
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CastVoteAsync_ValidVote_CreatesVoteAndReturnsResponse()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        var (result, isNew) = await service.CastVoteAsync(poll.Id, user.Id, optionIds);

        isNew.Should().BeTrue();
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.PollId.Should().Be(poll.Id);
        result.RankedChoices.Should().BeEquivalentTo(optionIds, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task CastVoteAsync_DuplicateVote_UpdatesExistingVote()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        // Cast initial vote
        var (firstVote, _) = await service.CastVoteAsync(poll.Id, user.Id, optionIds);

        // Cast updated vote with reversed order
        var reversed = new List<Guid>(optionIds);
        reversed.Reverse();
        var (updatedVote, isNew) = await service.CastVoteAsync(poll.Id, user.Id, reversed);

        isNew.Should().BeFalse();
        updatedVote.Id.Should().Be(firstVote.Id);
        updatedVote.RankedChoices.Should().BeEquivalentTo(reversed, opts => opts.WithStrictOrdering());
        updatedVote.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CastVoteAsync_PartialBallot_AcceptsSubsetOfOptions()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        // Only rank the first option (partial ballot)
        var partial = new List<Guid> { optionIds[0] };

        var (result, isNew) = await service.CastVoteAsync(poll.Id, user.Id, partial);

        isNew.Should().BeTrue();
        result.RankedChoices.Should().HaveCount(1);
        result.RankedChoices[0].Should().Be(optionIds[0]);
    }

    // -----------------------------------------------------------------------
    // CastVoteAsync — error cases
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CastVoteAsync_ClosedPoll_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);

        poll.Status = "Closed";
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var act = async () => await service.CastVoteAsync(poll.Id, user.Id, optionIds);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CastVoteAsync_DeletedPoll_ThrowsInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);

        poll.Status = "Deleted";
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var act = async () => await service.CastVoteAsync(poll.Id, user.Id, optionIds);

        // Deleted polls won't be found by Include query (status isn't checked there,
        // but the poll's status IS checked and isn't "Active")
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CastVoteAsync_UnknownPoll_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var act = async () => await service.CastVoteAsync(Guid.NewGuid(), user.Id, new List<Guid> { Guid.NewGuid() });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CastVoteAsync_InvalidOptionIds_ThrowsArgumentException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, _) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        var invalidIds = new List<Guid> { Guid.NewGuid() }; // ID not from this poll

        var act = async () => await service.CastVoteAsync(poll.Id, user.Id, invalidIds);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // -----------------------------------------------------------------------
    // GetUserVoteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserVoteAsync_ExistingVote_ReturnsVoteResponse()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        await service.CastVoteAsync(poll.Id, user.Id, optionIds);

        var result = await service.GetUserVoteAsync(poll.Id, user.Id);

        result.Should().NotBeNull();
        result!.PollId.Should().Be(poll.Id);
        result.RankedChoices.Should().BeEquivalentTo(optionIds, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task GetUserVoteAsync_NoVote_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, _) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        var result = await service.GetUserVoteAsync(poll.Id, user.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserVoteAsync_UnknownPoll_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var service = CreateService(context);

        var act = async () => await service.GetUserVoteAsync(Guid.NewGuid(), user.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // -----------------------------------------------------------------------
    // GetVoteCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetVoteCountAsync_ReturnsCorrectCounts()
    {
        using var context = CreateInMemoryContext();
        var userA = await CreateTestUser(context);
        var userB = await CreateTestUser(context);
        var (poll, optionIds) = await CreateTestPoll(context, userA.Id);
        var service = CreateService(context);

        await service.CastVoteAsync(poll.Id, userA.Id, optionIds);
        await service.CastVoteAsync(poll.Id, userB.Id, new List<Guid> { optionIds[1], optionIds[0] });

        var result = await service.GetVoteCountAsync(poll.Id);

        result.TotalVotes.Should().Be(2);
        result.UniqueVoters.Should().Be(2);
    }

    [Fact]
    public async Task GetVoteCountAsync_NoVotes_ReturnsZero()
    {
        using var context = CreateInMemoryContext();
        var user = await CreateTestUser(context);
        var (poll, _) = await CreateTestPoll(context, user.Id);
        var service = CreateService(context);

        var result = await service.GetVoteCountAsync(poll.Id);

        result.TotalVotes.Should().Be(0);
        result.UniqueVoters.Should().Be(0);
    }

    [Fact]
    public async Task GetVoteCountAsync_UnknownPoll_ThrowsKeyNotFoundException()
    {
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var act = async () => await service.GetVoteCountAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
