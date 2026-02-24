namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request DTO for casting or updating a vote in a poll.
/// </summary>
public class CastVoteRequest
{
    /// <summary>
    /// Ordered list of option IDs representing the voter's ranked preferences.
    /// Must contain at least one ID, no duplicates, and all IDs must belong to the target poll.
    /// </summary>
    public List<Guid> RankedOptionIds { get; set; } = new();
}
