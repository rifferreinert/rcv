using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Persists lazy deadline-based poll closure.
/// </summary>
public sealed class PollLifecycleService : IPollLifecycleService
{
    private readonly RcvDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes the lifecycle service.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    /// <param name="cache">The results cache to invalidate.</param>
    public PollLifecycleService(RcvDbContext context, TimeProvider timeProvider, IMemoryCache cache)
    {
        _context = context;
        _timeProvider = timeProvider;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<bool> ApplyEffectiveStatusAsync(Poll poll, CancellationToken cancellationToken = default)
    {
        var changed = Apply(poll);
        if (changed)
            await _context.SaveChangesAsync(cancellationToken);
        return changed;
    }

    /// <inheritdoc />
    public async Task<int> ApplyEffectiveStatusesForCreatorAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (_context.Database.IsRelational())
        {
            return await _context.Polls
                .Where(p =>
                    p.CreatorId == creatorId &&
                    p.Status == PollStatus.Active &&
                    p.ClosesAt != null &&
                    p.ClosesAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.Status, PollStatus.Closed)
                        .SetProperty(p => p.ClosedAt, p => p.ClosedAt ?? now),
                    cancellationToken);
        }

        var polls = await _context.Polls
            .Where(p => p.CreatorId == creatorId)
            .ToListAsync(cancellationToken);
        var changed = polls.Count(Apply);
        if (changed > 0)
            await _context.SaveChangesAsync(cancellationToken);
        return changed;
    }

    private bool Apply(Poll poll)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (poll.Status != PollStatus.Active || poll.ClosesAt is null || poll.ClosesAt.Value > now)
            return false;

        poll.Status = PollStatus.Closed;
        poll.ClosedAt ??= now;
        _cache.Remove(ResultsCacheKeys.ForPoll(poll.Id));
        return true;
    }
}
