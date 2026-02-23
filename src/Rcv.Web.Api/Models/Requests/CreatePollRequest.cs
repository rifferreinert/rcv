namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request DTO for creating a new poll.
/// </summary>
public class CreatePollRequest
{
    /// <summary>Title/question for the poll. Required, max 500 chars.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description providing more context.</summary>
    public string? Description { get; set; }

    /// <summary>List of option texts. Must have 2–50 items, each max 500 chars.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Optional deadline. Must be in the future if provided.</summary>
    public DateTime? ClosesAt { get; set; }

    /// <summary>Whether live results are visible while voting is in progress. Defaults to true.</summary>
    public bool IsResultsPublic { get; set; } = true;

    /// <summary>Whether individual votes are public (false = anonymous). Defaults to false.</summary>
    public bool IsVotingPublic { get; set; } = false;
}
