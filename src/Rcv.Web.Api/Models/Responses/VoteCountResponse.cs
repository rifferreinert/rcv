namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Response DTO for poll participation statistics.
/// </summary>
public record VoteCountResponse(
    /// <summary>Total number of votes cast in the poll.</summary>
    int TotalVotes,
    /// <summary>Number of unique voters (same as TotalVotes given one-vote-per-user constraint).</summary>
    int UniqueVoters
);
