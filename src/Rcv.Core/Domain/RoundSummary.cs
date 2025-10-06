namespace Rcv.Core.Domain;

/// <summary>
/// Snapshot of vote distribution in a single elimination round.
/// Immutable after construction.
/// </summary>
public class RoundSummary
{
    /// <summary>
    /// Round number (1-indexed). Round 1 is initial vote count.
    /// </summary>
    public int RoundNumber { get; }

    /// <summary>
    /// Vote count for each remaining option (by option ID) in this round.
    /// Uses Guid keys for stability.
    /// </summary>
    public IReadOnlyDictionary<Guid, int> VoteCounts { get; }

    /// <summary>
    /// The option eliminated in this round, or null for the final round.
    /// </summary>
    public Option? EliminatedOption { get; }

    /// <summary>
    /// Constructs a round summary with validation.
    /// </summary>
    /// <param name="roundNumber">Round number (must be positive)</param>
    /// <param name="voteCounts">Vote counts by option ID</param>
    /// <param name="eliminatedOption">Option eliminated in this round (null for final round)</param>
    /// <exception cref="ArgumentException">Thrown when round number is less than 1</exception>
    /// <exception cref="ArgumentNullException">Thrown when voteCounts is null</exception>
    public RoundSummary(
        int roundNumber,
        IReadOnlyDictionary<Guid, int> voteCounts,
        Option? eliminatedOption = null)
    {
        if (roundNumber < 1)
            throw new ArgumentException("Round number must be positive", nameof(roundNumber));

        RoundNumber = roundNumber;
        VoteCounts = voteCounts ?? throw new ArgumentNullException(nameof(voteCounts));
        EliminatedOption = eliminatedOption;
    }
}
