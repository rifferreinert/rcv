namespace Rcv.Web.Api.Models.Requests;

/// <summary>
/// Request DTO for updating an existing poll. All fields are optional;
/// only non-null fields will be applied. Updates are blocked once votes have been cast.
/// </summary>
public class UpdatePollRequest
{
    /// <summary>New title. Max 500 chars.</summary>
    public string? Title { get; set; }

    /// <summary>New description.</summary>
    public string? Description { get; set; }

    /// <summary>Replacement option list. If provided, must have 2–50 items, each max 500 chars.</summary>
    public List<string>? Options { get; set; }

    /// <summary>New close deadline. Must be in the future if provided.</summary>
    public DateTime? ClosesAt { get; set; }
}
