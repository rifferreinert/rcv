namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Response DTO returned after casting or retrieving a vote.
/// </summary>
public class VoteResponse
{
    /// <summary>Unique identifier for this vote.</summary>
    public Guid Id { get; set; }

    /// <summary>The poll this vote belongs to.</summary>
    public Guid PollId { get; set; }

    /// <summary>Ordered list of option IDs representing the voter's ranked preferences.</summary>
    public List<Guid> RankedChoices { get; set; } = new();

    /// <summary>When the vote was originally cast.</summary>
    public DateTime CastAt { get; set; }

    /// <summary>When the vote was last updated, or null if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
