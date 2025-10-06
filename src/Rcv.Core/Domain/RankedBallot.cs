namespace Rcv.Core.Domain;

/// <summary>
/// Represents a single voter's ranked preferences.
/// Immutable after construction.
/// </summary>
public class RankedBallot
{
    /// <summary>
    /// Ordered list of option IDs from most to least preferred.
    /// Voters may rank all or a subset of available options (partial ballots supported).
    /// </summary>
    public IReadOnlyList<Guid> RankedOptionIds { get; }

    /// <summary>
    /// Constructs a ranked ballot with validation.
    /// </summary>
    /// <param name="rankedOptionIds">Ordered preference list, cannot be null or contain duplicates</param>
    /// <exception cref="ArgumentNullException">Thrown when rankedOptionIds is null</exception>
    /// <exception cref="ArgumentException">Thrown when rankedOptionIds contains duplicate IDs</exception>
    public RankedBallot(IEnumerable<Guid> rankedOptionIds)
    {
        if (rankedOptionIds == null)
            throw new ArgumentNullException(nameof(rankedOptionIds));

        var optionList = rankedOptionIds.ToList();

        // Check for duplicates
        if (optionList.Count != optionList.Distinct().Count())
        {
            throw new ArgumentException("Ballot cannot contain duplicate option IDs", nameof(rankedOptionIds));
        }

        RankedOptionIds = optionList.AsReadOnly();
    }
}
