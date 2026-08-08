namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Represents one instant-runoff elimination round.
/// </summary>
public sealed record ResultRoundResponse(
    int RoundNumber,
    IReadOnlyDictionary<Guid, int> VoteCounts,
    Guid? EliminatedOptionId);
