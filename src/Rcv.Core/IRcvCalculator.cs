using Rcv.Core.Domain;

namespace Rcv.Core;

/// <summary>
/// Interface for ranked choice voting calculation algorithms.
/// Allows different RCV algorithms to be plugged in (e.g., Instant Runoff, Borda Count, STV).
/// </summary>
public interface IRcvCalculator
{
    /// <summary>
    /// Calculate election results from ballots using a specific RCV algorithm.
    /// </summary>
    /// <param name="options">The available options/candidates in the election</param>
    /// <param name="ballots">The ranked ballots from voters</param>
    /// <param name="random">Optional Random instance for algorithms that need tie-breaking. If null, uses new Random()</param>
    /// <returns>Complete election results including winner/tie, rounds, and statistics</returns>
    /// <exception cref="InvalidOperationException">Thrown when calculation cannot proceed (e.g., no ballots)</exception>
    RcvResult Calculate(IReadOnlyList<Option> options, IEnumerable<RankedBallot> ballots, Random? random = null);
}
