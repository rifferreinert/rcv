using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Calculates and authorizes ranked-choice poll results.
/// </summary>
public interface IResultsService
{
    /// <summary>
    /// Gets the current result for a poll.
    /// </summary>
    /// <param name="pollId">The poll identifier.</param>
    /// <param name="requestingUserId">The authenticated user, when present.</param>
    /// <returns>The current result, including an explicit no-votes state.</returns>
    Task<ResultsResponse> GetResultsAsync(Guid pollId, Guid? requestingUserId);
}
