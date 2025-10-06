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
    /// Calculate election results using instant-runoff voting algorithm.
    /// Pure function - produces same result for same ballots every time.
    /// </summary>
    /// <param name="ballots">Voter preferences as ranked lists of option IDs</param>
    /// <returns>Complete election results including winner/tie, round-by-round data, and statistics</returns>
    /// <exception cref="ArgumentNullException">Thrown when ballots is null</exception>
    /// <exception cref="ArgumentException">Thrown when ballots contain invalid option IDs or duplicate rankings</exception>
    public RcvResult CalculateResult(IEnumerable<RankedBallot> ballots)
    {
        if (ballots == null)
            throw new ArgumentNullException(nameof(ballots));

        // TODO: Implement RCV algorithm
        // For now, return a stub result to make it compile
        throw new NotImplementedException("RCV calculation not yet implemented");
    }
}
