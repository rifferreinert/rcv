using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;

namespace Rcv.Web.Api.Tests.Services;

public sealed class ResultsServiceTests
{
    [Fact]
    public async Task GetResultsAsync_NoVotes_ReturnsExplicitNoVotesState()
    {
        using var fixture = await ResultsFixture.CreateAsync(isResultsPublic: false);

        var result = await fixture.Service.GetResultsAsync(fixture.Poll.Id, fixture.Poll.CreatorId);

        result.State.Should().Be(ResultsState.NoVotes);
        result.TotalVotes.Should().Be(0);
        result.Winner.Should().BeNull();
        result.Rounds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResultsAsync_PrivateOpenPoll_AllowsCreatorButForbidsOthers()
    {
        using var fixture = await ResultsFixture.CreateAsync(isResultsPublic: false);
        await fixture.AddVoteAsync(fixture.OptionIds);

        (await fixture.Service.GetResultsAsync(fixture.Poll.Id, fixture.Poll.CreatorId))
            .State.Should().Be(ResultsState.InProgress);

        var act = () => fixture.Service.GetResultsAsync(fixture.Poll.Id, Guid.NewGuid());
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetResultsAsync_ClosedPrivatePoll_IsFinalPublicAndCached()
    {
        using var fixture = await ResultsFixture.CreateAsync(isResultsPublic: false);
        await fixture.AddVoteAsync(fixture.OptionIds);
        fixture.Poll.Status = PollStatus.Closed;
        fixture.Poll.ClosedAt = fixture.Clock.GetUtcNow().UtcDateTime;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);
        var cached = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);

        result.State.Should().Be(ResultsState.Final);
        result.TotalVotes.Should().Be(1);
        ReferenceEquals(result, cached).Should().BeTrue();
    }

    [Fact]
    public async Task GetResultsAsync_OpenPoll_RecalculatesAfterVoteAndRemainsDeterministic()
    {
        using var fixture = await ResultsFixture.CreateAsync(isResultsPublic: true);
        await fixture.AddVoteAsync(new[] { fixture.OptionIds[0], fixture.OptionIds[1], fixture.OptionIds[2] });
        await fixture.AddVoteAsync(new[] { fixture.OptionIds[0], fixture.OptionIds[2], fixture.OptionIds[1] });
        await fixture.AddVoteAsync(new[] { fixture.OptionIds[1], fixture.OptionIds[0], fixture.OptionIds[2] });
        await fixture.AddVoteAsync(new[] { fixture.OptionIds[2], fixture.OptionIds[0], fixture.OptionIds[1] });

        var first = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);
        var recalculatedBeforeVote = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);
        ReferenceEquals(first, recalculatedBeforeVote).Should().BeFalse();

        var voterId = Guid.NewGuid();
        var lifecycle = new PollLifecycleService(fixture.Context, fixture.Clock, fixture.Cache);
        var voting = new VotingService(fixture.Context, lifecycle, fixture.Cache, fixture.Clock);
        await voting.CastVoteAsync(
            fixture.Poll.Id,
            voterId,
            new List<Guid> { fixture.OptionIds[0], fixture.OptionIds[1], fixture.OptionIds[2] });
        var afterVote = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);
        afterVote.TotalVotes.Should().Be(first.TotalVotes + 1);

        var recalculated = await fixture.Service.GetResultsAsync(fixture.Poll.Id, null);
        recalculated.Winner.Should().Be(afterVote.Winner);
        recalculated.Rounds.Should().BeEquivalentTo(afterVote.Rounds, options => options.WithStrictOrdering());
    }

    private sealed class ResultsFixture : IDisposable
    {
        public required RcvDbContext Context { get; init; }
        public required MemoryCache Cache { get; init; }
        public required ManualTimeProvider Clock { get; init; }
        public required ResultsService Service { get; init; }
        public required Poll Poll { get; init; }
        public required List<Guid> OptionIds { get; init; }

        public static async Task<ResultsFixture> CreateAsync(bool isResultsPublic)
        {
            var options = new DbContextOptionsBuilder<RcvDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new RcvDbContext(options);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var clock = new ManualTimeProvider(
                new DateTimeOffset(2026, 8, 8, 1, 0, 0, TimeSpan.Zero));
            var creator = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = Guid.NewGuid().ToString(),
                Provider = "Google",
                CreatedAt = clock.GetUtcNow().UtcDateTime,
            };
            var poll = new Poll
            {
                Id = Guid.NewGuid(),
                Title = "Results",
                CreatorId = creator.Id,
                Status = PollStatus.Active,
                IsResultsPublic = isResultsPublic,
                CreatedAt = clock.GetUtcNow().UtcDateTime,
            };
            var pollOptions = Enumerable.Range(0, 3)
                .Select(index => new PollOption
                {
                    Id = Guid.NewGuid(),
                    PollId = poll.Id,
                    OptionText = $"Option {index}",
                    DisplayOrder = index,
                    CreatedAt = clock.GetUtcNow().UtcDateTime,
                })
                .ToList();
            context.AddRange(creator, poll);
            context.PollOptions.AddRange(pollOptions);
            await context.SaveChangesAsync();
            var lifecycle = new PollLifecycleService(context, clock, cache);
            return new ResultsFixture
            {
                Context = context,
                Cache = cache,
                Clock = clock,
                Service = new ResultsService(context, lifecycle, cache, clock),
                Poll = poll,
                OptionIds = pollOptions.Select(option => option.Id).ToList(),
            };
        }

        public async Task AddVoteAsync(IEnumerable<Guid> rankings)
        {
            Context.Votes.Add(new Vote
            {
                Id = Guid.NewGuid(),
                PollId = Poll.Id,
                VoterId = Guid.NewGuid(),
                RankedChoices = rankings.ToList(),
                CastAt = Clock.GetUtcNow().UtcDateTime,
            });
            await Context.SaveChangesAsync();
            Cache.Remove($"poll-results:{Poll.Id:N}");
        }

        public void Dispose()
        {
            Context.Dispose();
            Cache.Dispose();
        }
    }
}
