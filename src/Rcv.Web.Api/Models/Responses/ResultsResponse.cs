namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Represents the calculated ranked-choice result for a poll.
/// </summary>
public sealed record ResultsResponse(
    Guid PollId,
    ResultsState State,
    int TotalVotes,
    ResultOptionResponse? Winner,
    bool IsTie,
    IReadOnlyList<ResultOptionResponse> TiedOptions,
    IReadOnlyList<ResultRoundResponse> Rounds,
    IReadOnlyDictionary<Guid, int> FinalVoteTotals,
    DateTime CalculatedAt);
