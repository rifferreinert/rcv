namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Complete ranked choice voting results for a poll.
/// </summary>
public class ResultResponse
{
    /// <summary>The poll these results belong to.</summary>
    public Guid PollId { get; set; }

    /// <summary>The winning option, or null if tied or no votes.</summary>
    public PollOptionDto? Winner { get; set; }

    /// <summary>True when the election ended in an unbreakable tie.</summary>
    public bool IsTie { get; set; }

    /// <summary>Options that tied for the win (empty when IsTie is false).</summary>
    public List<PollOptionDto> TiedOptions { get; set; } = new();

    /// <summary>Round-by-round elimination data.</summary>
    public List<RoundSummaryDto> Rounds { get; set; } = new();

    /// <summary>Final vote totals keyed by option ID.</summary>
    public Dictionary<Guid, int> FinalVoteTotals { get; set; } = new();

    /// <summary>Total number of ballots cast.</summary>
    public int TotalVotes { get; set; }
}
