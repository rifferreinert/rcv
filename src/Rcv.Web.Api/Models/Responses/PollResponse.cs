namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Full poll representation returned by the API.
/// </summary>
public class PollResponse
{
    /// <summary>Unique poll identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Poll title/question.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>The user who created this poll.</summary>
    public UserSummaryDto Creator { get; set; } = null!;

    /// <summary>The poll's voting options in display order.</summary>
    public List<PollOptionDto> Options { get; set; } = new();

    /// <summary>Current status: "Active", "Closed", or "Deleted".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the poll was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Optional deadline (null = no deadline).</summary>
    public DateTime? ClosesAt { get; set; }

    /// <summary>Actual timestamp when the poll was closed (null if still open).</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Whether live results are visible while voting is in progress.</summary>
    public bool IsResultsPublic { get; set; }

    /// <summary>Whether individual votes are public (false = anonymous).</summary>
    public bool IsVotingPublic { get; set; }

    /// <summary>Total number of votes cast so far.</summary>
    public int VoteCount { get; set; }
}
