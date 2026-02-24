using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Service responsible for calculating and returning ranked choice voting results.
/// </summary>
public interface IResultsService
{
    /// <summary>
    /// Calculates the ranked choice voting results for the specified poll.
    /// Returns cached results when available.
    /// </summary>
    /// <param name="pollId">The poll to calculate results for.</param>
    /// <returns>The complete election results including winner, rounds, and statistics.</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    Task<ResultResponse> CalculateResultsAsync(Guid pollId);
}
