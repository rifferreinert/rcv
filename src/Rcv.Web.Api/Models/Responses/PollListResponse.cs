namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Paginated list of polls returned by the API.
/// </summary>
public class PollListResponse
{
    /// <summary>The polls on this page.</summary>
    public List<PollResponse> Items { get; set; } = new();

    /// <summary>Total number of polls matching the query (across all pages).</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Maximum number of items per page.</summary>
    public int PageSize { get; set; }
}
