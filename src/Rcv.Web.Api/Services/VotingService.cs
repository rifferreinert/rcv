using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Implements vote casting, retrieval, and participation statistics.
/// </summary>
public class VotingService : IVotingService
{
    private readonly RcvDbContext _context;
    private readonly IPollLifecycleService _lifecycle;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the service with system lifecycle dependencies.
    /// </summary>
    /// <param name="context">The database context.</param>
    public VotingService(RcvDbContext context)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _context = context;
        _cache = cache;
        _timeProvider = TimeProvider.System;
        _lifecycle = new PollLifecycleService(context, _timeProvider, cache);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VotingService"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="lifecycle">The effective-status lifecycle service.</param>
    /// <param name="cache">The poll results cache.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    public VotingService(
        RcvDbContext context,
        IPollLifecycleService lifecycle,
        IMemoryCache cache,
        TimeProvider timeProvider)
    {
        _context = context;
        _lifecycle = lifecycle;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<(VoteResponse Vote, bool IsNew)> CastVoteAsync(Guid pollId, Guid userId, List<Guid> rankedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(rankedOptionIds);
        if (rankedOptionIds.Count == 0 ||
            rankedOptionIds.Any(id => id == Guid.Empty) ||
            rankedOptionIds.Count != rankedOptionIds.Distinct().Count())
            throw new ArgumentException(
                "Ranked option IDs must be non-empty, unique, and contain at least one option.",
                nameof(rankedOptionIds));

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        await EnsurePollIsActiveAsync(poll, transaction);

        // Validate all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        foreach (var optionId in rankedOptionIds)
        {
            if (!validOptionIds.Contains(optionId))
                throw new ArgumentException($"Option ID {optionId} does not belong to this poll.");
        }

        // Check for existing vote (upsert)
        var existingVote = _context.Database.IsSqlServer()
            ? await _context.Votes
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM [Votes] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [PollId] = {pollId} AND [VoterId] = {userId}
                    """)
                .SingleOrDefaultAsync()
            : await _context.Votes
                .FirstOrDefaultAsync(v => v.PollId == pollId && v.VoterId == userId);

        if (existingVote != null)
        {
            await EnsurePollIsActiveAsync(poll, transaction);
            existingVote.RankedChoices = rankedOptionIds;
            existingVote.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync();
            if (transaction is not null)
                await transaction.CommitAsync();
            _cache.Remove(ResultsCacheKeys.ForPoll(pollId));
            return (MapToVoteResponse(existingVote), false);
        }

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            VoterId = userId,
            RankedChoices = rankedOptionIds,
            CastAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        await EnsurePollIsActiveAsync(poll, transaction);
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        _cache.Remove(ResultsCacheKeys.ForPoll(pollId));
        return (MapToVoteResponse(vote), true);
    }

    /// <inheritdoc />
    public async Task<VoteStatusResponse> GetUserVoteAsync(Guid pollId, Guid userId)
    {
        var poll = await GetPollAsync(pollId);
        await _lifecycle.ApplyEffectiveStatusAsync(poll);

        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.PollId == pollId && v.VoterId == userId);

        return new VoteStatusResponse(
            vote is not null,
            vote is not null && poll.Status == PollStatus.Active,
            vote?.CastAt,
            vote?.UpdatedAt,
            vote is null ? Array.Empty<Guid>() : vote.RankedChoices);
    }

    /// <inheritdoc />
    public async Task<VoteCountResponse> GetVoteCountAsync(Guid pollId)
    {
        var poll = await GetPollAsync(pollId);
        await _lifecycle.ApplyEffectiveStatusAsync(poll);

        var totalVotes = await _context.Votes.CountAsync(v => v.PollId == pollId);
        var uniqueVoters = await _context.Votes
            .Where(v => v.PollId == pollId)
            .Select(v => v.VoterId)
            .Distinct()
            .CountAsync();

        return new VoteCountResponse(totalVotes, uniqueVoters);
    }

    /// <summary>
    /// Ensures the poll exists (non-deleted). Throws <see cref="KeyNotFoundException"/> otherwise.
    /// </summary>
    private async Task<Poll> GetPollAsync(Guid pollId)
    {
        var poll = await _context.Polls.FirstOrDefaultAsync(
            p => p.Id == pollId && p.Status != PollStatus.Deleted);
        if (poll is null)
            throw new KeyNotFoundException($"Poll {pollId} not found.");
        return poll;
    }

    private async Task EnsurePollIsActiveAsync(Poll poll, IDbContextTransaction? transaction)
    {
        await _lifecycle.ApplyEffectiveStatusAsync(poll);
        if (poll.Status == PollStatus.Active)
            return;

        if (transaction is not null)
            await transaction.CommitAsync();
        throw new InvalidOperationException($"Cannot vote because the poll status is '{poll.Status}'.");
    }

    /// <summary>
    /// Maps a <see cref="Vote"/> entity to a <see cref="VoteResponse"/> DTO.
    /// </summary>
    private static VoteResponse MapToVoteResponse(Vote vote) => new()
    {
        Id = vote.Id,
        PollId = vote.PollId,
        RankedChoices = vote.RankedChoices,
        CastAt = vote.CastAt,
        UpdatedAt = vote.UpdatedAt,
    };
}
