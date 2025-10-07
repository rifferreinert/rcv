using Rcv.Core.Domain;

namespace Rcv.Core;

/// <summary>
/// Provides ranked choice voting (instant-runoff) calculation for a set of options.
/// This class is immutable and thread-safe after construction.
/// </summary>
public class RankedChoicePoll
{
    private readonly IReadOnlyList<Option> _options;

    /// <summary>
    /// Initialize with poll options. Requires minimum 2 options with unique IDs.
    /// </summary>
    /// <param name="options">The candidates/options voters can rank</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="ArgumentException">Thrown when less than 2 options or duplicate IDs provided</exception>
    public RankedChoicePoll(IEnumerable<Option> options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var optionList = options.ToList();

        // Validate minimum options
        if (optionList.Count < 2)
        {
            throw new ArgumentException("Poll must have at least 2 options", nameof(options));
        }

        // Validate unique IDs
        var uniqueIds = optionList.Select(o => o.Id).Distinct().Count();
        if (uniqueIds != optionList.Count)
        {
            throw new ArgumentException("All option IDs must be unique", nameof(options));
        }

        _options = optionList.AsReadOnly();
    }

    /// <summary>
    /// Calculate election results using the provided RCV calculator algorithm.
    /// Allows different calculation strategies to be plugged in (e.g., Instant Runoff, Borda Count).
    /// </summary>
    /// <param name="ballots">Voter preferences as ranked lists of option IDs</param>
    /// <param name="calculator">The RCV calculation algorithm to use</param>
    /// <param name="random">Optional Random instance for algorithms that need tie-breaking. If null, uses new Random()</param>
    /// <returns>Complete election results including winner/tie, round-by-round data, and statistics</returns>
    /// <exception cref="ArgumentNullException">Thrown when ballots or calculator is null</exception>
    /// <exception cref="ArgumentException">Thrown when a ballot contains unknown option IDs</exception>
    public RcvResult CalculateResult(IEnumerable<RankedBallot> ballots, IRcvCalculator calculator, Random? random = null)
    {
        if (ballots == null)
            throw new ArgumentNullException(nameof(ballots));

        if (calculator == null)
            throw new ArgumentNullException(nameof(calculator));

        // Validate that all ballot option IDs exist in poll options
        var validOptionIds = _options.Select(o => o.Id).ToHashSet();
        var ballotList = ballots.ToList();

        foreach (var ballot in ballotList)
        {
            foreach (var optionId in ballot.RankedOptionIds)
            {
                if (!validOptionIds.Contains(optionId))
                {
                    throw new ArgumentException(
                        $"Ballot contains unknown option ID: {optionId}. All option IDs in ballots must exist in the poll options.",
                        nameof(ballots));
                }
            }
        }

        return calculator.Calculate(_options, ballotList, random);
    }
}
