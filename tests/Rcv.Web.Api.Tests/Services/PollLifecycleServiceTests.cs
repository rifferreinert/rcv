using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Services;

namespace Rcv.Web.Api.Tests.Services;

public sealed class PollLifecycleServiceTests
{
    [Fact]
    public async Task ApplyEffectiveStatusAsync_AtDeadline_ClosesExactlyOnce()
    {
        using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = new DateTimeOffset(2026, 8, 8, 1, 0, 0, TimeSpan.Zero);
        var clock = new ManualTimeProvider(now);
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Boundary",
            CreatorId = Guid.NewGuid(),
            Status = PollStatus.Active,
            CreatedAt = now.UtcDateTime.AddDays(-1),
            ClosesAt = now.UtcDateTime,
        };
        context.Polls.Add(poll);
        await context.SaveChangesAsync();
        var service = new PollLifecycleService(context, clock, cache);

        (await service.ApplyEffectiveStatusAsync(poll)).Should().BeTrue();
        var closedAt = poll.ClosedAt;
        clock.Advance(TimeSpan.FromHours(1));
        (await service.ApplyEffectiveStatusAsync(poll)).Should().BeFalse();

        poll.Status.Should().Be(PollStatus.Closed);
        poll.ClosedAt.Should().Be(closedAt);
    }

    [Fact]
    public async Task CastVoteAsync_AfterDeadline_RejectsRevisionAndPersistsClosure()
    {
        using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = new DateTimeOffset(2026, 8, 8, 1, 0, 0, TimeSpan.Zero);
        var clock = new ManualTimeProvider(now);
        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            Title = "Closed",
            CreatorId = Guid.NewGuid(),
            Status = PollStatus.Active,
            CreatedAt = now.UtcDateTime.AddDays(-1),
            ClosesAt = now.UtcDateTime,
        };
        var option = new PollOption
        {
            Id = Guid.NewGuid(),
            PollId = poll.Id,
            OptionText = "A",
            CreatedAt = now.UtcDateTime,
        };
        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = poll.Id,
            VoterId = Guid.NewGuid(),
            RankedChoices = new List<Guid> { option.Id },
            CastAt = now.UtcDateTime.AddHours(-1),
        };
        context.AddRange(poll, option, vote);
        await context.SaveChangesAsync();
        var lifecycle = new PollLifecycleService(context, clock, cache);
        var service = new VotingService(context, lifecycle, cache, clock);

        var act = () => service.CastVoteAsync(poll.Id, vote.VoterId, new List<Guid> { option.Id });

        await act.Should().ThrowAsync<InvalidOperationException>();
        poll.Status.Should().Be(PollStatus.Closed);
        poll.ClosedAt.Should().Be(now.UtcDateTime);
        vote.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task ApplyEffectiveStatusesForCreatorAsync_ClosesOnlyOwnedExpiredPolls()
    {
        using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = new DateTimeOffset(2026, 8, 8, 1, 0, 0, TimeSpan.Zero);
        var creatorId = Guid.NewGuid();
        var expiredOwnedPoll = CreatePoll(creatorId, now.UtcDateTime.AddMinutes(-1));
        var futureOwnedPoll = CreatePoll(creatorId, now.UtcDateTime.AddMinutes(1));
        var otherCreatorPoll = CreatePoll(Guid.NewGuid(), now.UtcDateTime.AddMinutes(-1));
        context.Polls.AddRange(expiredOwnedPoll, futureOwnedPoll, otherCreatorPoll);
        await context.SaveChangesAsync();
        var service = new PollLifecycleService(context, new ManualTimeProvider(now), cache);

        var changed = await service.ApplyEffectiveStatusesForCreatorAsync(creatorId);

        changed.Should().Be(1);
        expiredOwnedPoll.Status.Should().Be(PollStatus.Closed);
        expiredOwnedPoll.ClosedAt.Should().Be(now.UtcDateTime);
        futureOwnedPoll.Status.Should().Be(PollStatus.Active);
        otherCreatorPoll.Status.Should().Be(PollStatus.Active);
    }

    private static Poll CreatePoll(Guid creatorId, DateTime closesAt) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Lifecycle",
        CreatorId = creatorId,
        Status = PollStatus.Active,
        CreatedAt = closesAt.AddDays(-1),
        ClosesAt = closesAt,
    };

    private static RcvDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RcvDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RcvDbContext(options);
    }
}

internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public ManualTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
}
