namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// A single elimination round in the RCV results.
/// </summary>
public record RoundSummaryDto(
    int RoundNumber,
    Dictionary<Guid, int> VoteCounts,
    PollOptionDto? EliminatedOption
);
