using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Core.Calculators;
using Rcv.Core.Domain;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Maps persisted polls to deterministic instant-runoff calculations.
/// </summary>
public sealed class ResultsService : IResultsService
{
    private readonly RcvDbContext _context;
    private readonly IPollLifecycleService _lifecycle;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the results service.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="lifecycle">The effective-status lifecycle service.</param>
    /// <param name="cache">The results cache.</param>
    /// <param name="timeProvider">The UTC clock.</param>
    public ResultsService(
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
    public async Task<ResultsResponse> GetResultsAsync(Guid pollId, Guid? requestingUserId)
    {
        var poll = await _context.Polls
            .FirstOrDefaultAsync(p => p.Id == pollId && p.Status != PollStatus.Deleted)
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        await _lifecycle.ApplyEffectiveStatusAsync(poll);

        var isCreator = requestingUserId.HasValue && requestingUserId.Value == poll.CreatorId;
        var canView = isCreator || poll.Status == PollStatus.Closed || poll.IsResultsPublic;
        if (!canView)
            throw new UnauthorizedAccessException("Results are private while voting is in progress.");

        var key = ResultsCacheKeys.ForPoll(pollId);
        if (poll.Status == PollStatus.Closed &&
            _cache.TryGetValue(key, out ResultsResponse? cached) &&
            cached is not null)
            return cached;

        await _context.Entry(poll).Collection(p => p.Options).LoadAsync();
        await _context.Entry(poll).Collection(p => p.Votes).LoadAsync();

        var state = poll.Votes.Count == 0
            ? ResultsState.NoVotes
            : poll.Status == PollStatus.Closed ? ResultsState.Final : ResultsState.InProgress;
        var calculatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        ResultsResponse response;
        if (state == ResultsState.NoVotes)
        {
            response = new ResultsResponse(
                poll.Id,
                state,
                0,
                null,
                false,
                Array.Empty<ResultOptionResponse>(),
                Array.Empty<ResultRoundResponse>(),
                new Dictionary<Guid, int>(),
                calculatedAt);
        }
        else
        {
            var options = poll.Options
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Id)
                .Select(o => new Option(o.Id, o.OptionText))
                .ToList();
            var ballots = poll.Votes
                .OrderBy(v => v.Id)
                .Select(v => new RankedBallot(v.RankedChoices))
                .ToList();
            var seed = BitConverter.ToInt32(poll.Id.ToByteArray(), 0);
            var result = new InstantRunoffCalculator().Calculate(options, ballots, new Random(seed));

            response = new ResultsResponse(
                poll.Id,
                state,
                ballots.Count,
                result.Winner is null ? null : MapOption(result.Winner),
                result.IsTie,
                result.TiedOptions.Select(MapOption).ToList(),
                result.Rounds.Select(r => new ResultRoundResponse(
                    r.RoundNumber,
                    new Dictionary<Guid, int>(r.VoteCounts),
                    r.EliminatedOption?.Id)).ToList(),
                new Dictionary<Guid, int>(result.FinalVoteTotals),
                calculatedAt);
        }

        if (poll.Status == PollStatus.Closed)
            _cache.Set(key, response, TimeSpan.FromMinutes(5));
        return response;
    }

    private static ResultOptionResponse MapOption(Option option) => new(option.Id, option.Label);
}
