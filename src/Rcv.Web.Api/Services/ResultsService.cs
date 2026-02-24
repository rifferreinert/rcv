using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Rcv.Core;
using Rcv.Core.Calculators;
using Rcv.Core.Domain;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Orchestrates ranked choice voting result calculation using Rcv.Core.
/// Fetches votes from the database, maps to domain models, calculates, and returns DTOs.
/// </summary>
public class ResultsService : IResultsService
{
    private readonly RcvDbContext _context;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of <see cref="ResultsService"/>.
    /// </summary>
    public ResultsService(RcvDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<ResultResponse> CalculateResultsAsync(Guid pollId)
    {
        var cacheKey = $"results:{pollId}";

        if (_cache.TryGetValue(cacheKey, out ResultResponse? cached))
            return cached!;

        var poll = await _context.Polls
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId && p.Status != "Deleted")
            ?? throw new KeyNotFoundException($"Poll {pollId} not found.");

        var votes = await _context.Votes
            .Where(v => v.PollId == pollId)
            .ToListAsync();

        ResultResponse result;

        if (votes.Count == 0)
        {
            result = new ResultResponse
            {
                PollId = pollId,
                Winner = null,
                IsTie = false,
                TiedOptions = new List<PollOptionDto>(),
                Rounds = new List<RoundSummaryDto>(),
                FinalVoteTotals = new Dictionary<Guid, int>(),
                TotalVotes = 0,
            };
        }
        else
        {
            // Map EF entities to Rcv.Core domain models
            var options = poll.Options
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new Option(o.Id, o.OptionText))
                .ToList();

            var ballots = votes
                .Select(v => new RankedBallot(v.RankedChoices))
                .ToList();

            // Calculate using Rcv.Core
            var rcvPoll = new RankedChoicePoll(options);
            var rcvResult = rcvPoll.CalculateResult(ballots, new InstantRunoffCalculator());

            // Map domain result to DTO
            result = MapToResultResponse(pollId, poll, rcvResult, votes.Count);
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    /// <summary>
    /// Maps an <see cref="RcvResult"/> domain model to a <see cref="ResultResponse"/> DTO.
    /// </summary>
    private static ResultResponse MapToResultResponse(
        Guid pollId,
        Data.Entities.Poll poll,
        RcvResult rcvResult,
        int totalVotes)
    {
        // Build a lookup from option ID to PollOptionDto
        var optionLookup = poll.Options.ToDictionary(
            o => o.Id,
            o => new PollOptionDto(o.Id, o.OptionText, o.DisplayOrder));

        return new ResultResponse
        {
            PollId = pollId,
            Winner = rcvResult.Winner != null
                ? optionLookup.GetValueOrDefault(rcvResult.Winner.Id)
                : null,
            IsTie = rcvResult.IsTie,
            TiedOptions = rcvResult.TiedOptions
                .Select(o => optionLookup.GetValueOrDefault(o.Id)!)
                .Where(o => o != null)
                .ToList(),
            Rounds = rcvResult.Rounds
                .Select(r => new RoundSummaryDto(
                    r.RoundNumber,
                    new Dictionary<Guid, int>(r.VoteCounts),
                    r.EliminatedOption != null
                        ? optionLookup.GetValueOrDefault(r.EliminatedOption.Id)
                        : null))
                .ToList(),
            FinalVoteTotals = new Dictionary<Guid, int>(rcvResult.FinalVoteTotals),
            TotalVotes = totalVotes,
        };
    }
}
