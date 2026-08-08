namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Describes the current user's participation and revision ability.
/// </summary>
public sealed record VoteStatusResponse(
    bool HasVoted,
    bool CanChange,
    DateTime? CastAt,
    DateTime? UpdatedAt,
    IReadOnlyList<Guid> RankedOptionIds);
