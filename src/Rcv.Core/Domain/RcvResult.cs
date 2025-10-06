namespace Rcv.Core.Domain;

/// <summary>
/// Complete results of a ranked choice election.
/// Immutable after construction.
/// </summary>
public class RcvResult
{
    /// <summary>
    /// The winning option, or null if election ended in tie.
    /// </summary>
    public Option? Winner { get; }

    /// <summary>
    /// Round-by-round elimination data showing vote transfers.
    /// Always contains at least one round.
    /// </summary>
    public IReadOnlyList<RoundSummary> Rounds { get; }

    /// <summary>
    /// True when election ended in unbreakable tie, false otherwise.
    /// </summary>
    public bool IsTie { get; }

    /// <summary>
    /// Options that tied for the win (empty list when IsTie is false).
    /// </summary>
    public IReadOnlyList<Option> TiedOptions { get; }

    /// <summary>
    /// Final vote counts by option ID after all elimination rounds.
    /// Uses Guid keys for stability (not affected by Option property changes).
    /// </summary>
    public IReadOnlyDictionary<Guid, int> FinalVoteTotals { get; }

    /// <summary>
    /// Constructs election results with validation.
    /// Ensures logical consistency: either there's a winner OR there's a tie with tied options.
    /// </summary>
    /// <param name="winner">Winning option (required when not a tie)</param>
    /// <param name="rounds">Round-by-round data (must contain at least one round)</param>
    /// <param name="finalVoteTotals">Final vote counts by option ID</param>
    /// <param name="tiedOptions">Options that tied (only when there's a tie)</param>
    /// <exception cref="ArgumentNullException">Thrown when rounds or finalVoteTotals is null</exception>
    /// <exception cref="ArgumentException">Thrown when winner is null and no tied options provided, or when rounds is empty</exception>
    public RcvResult(
        Option? winner,
        IEnumerable<RoundSummary> rounds,
        IReadOnlyDictionary<Guid, int> finalVoteTotals,
        IEnumerable<Option>? tiedOptions = null)
    {
        if (rounds == null)
            throw new ArgumentNullException(nameof(rounds));

        FinalVoteTotals = finalVoteTotals ?? throw new ArgumentNullException(nameof(finalVoteTotals));

        var roundsList = rounds.ToList();
        if (roundsList.Count == 0)
            throw new ArgumentException("Result must contain at least one round", nameof(rounds));

        Rounds = roundsList.AsReadOnly();

        var tiedList = tiedOptions?.ToList() ?? new List<Option>();

        // Ensure logical consistency: either winner OR tie
        if (tiedList.Count > 0)
        {
            // It's a tie
            IsTie = true;
            Winner = null;
            TiedOptions = tiedList.AsReadOnly();
        }
        else
        {
            // Not a tie - must have a winner
            if (winner == null)
                throw new ArgumentException("Winner is required when there are no tied options", nameof(winner));

            IsTie = false;
            Winner = winner;
            TiedOptions = new List<Option>().AsReadOnly();
        }
    }
}
