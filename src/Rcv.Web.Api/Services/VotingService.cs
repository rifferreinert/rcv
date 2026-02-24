using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Initializes a new instance of <see cref="VotingService"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    public VotingService(RcvDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(VoteResponse Vote, bool IsNew)> CastVoteAsync(Guid pollId, Guid userId, List<Guid> rankedOptionIds)
    {
        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        if (poll.Status != "Active")
            throw new InvalidOperationException($"Cannot vote because the poll status is '{poll.Status}'.");

        // Validate all option IDs belong to this poll
        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        foreach (var optionId in rankedOptionIds)
        {
            if (!validOptionIds.Contains(optionId))
                throw new ArgumentException($"Option ID {optionId} does not belong to this poll.");
        }

        // Check for existing vote (upsert)
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.PollId == pollId && v.VoterId == userId);

        if (existingVote != null)
        {
            existingVote.RankedChoices = rankedOptionIds;
            existingVote.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (MapToVoteResponse(existingVote), false);
        }

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            VoterId = userId,
            RankedChoices = rankedOptionIds,
            CastAt = DateTime.UtcNow,
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();
        return (MapToVoteResponse(vote), true);
    }

    /// <inheritdoc />
    public async Task<VoteResponse?> GetUserVoteAsync(Guid pollId, Guid userId)
    {
        await EnsurePollExistsAsync(pollId);

        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.PollId == pollId && v.VoterId == userId);

        return vote is null ? null : MapToVoteResponse(vote);
    }

    /// <inheritdoc />
    public async Task<VoteCountResponse> GetVoteCountAsync(Guid pollId)
    {
        await EnsurePollExistsAsync(pollId);

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
    private async Task EnsurePollExistsAsync(Guid pollId)
    {
        var exists = await _context.Polls.AnyAsync(p => p.Id == pollId && p.Status != "Deleted");
        if (!exists)
            throw new KeyNotFoundException($"Poll {pollId} not found.");
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
