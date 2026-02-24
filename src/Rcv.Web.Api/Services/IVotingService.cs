using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Service responsible for vote casting, retrieval, and participation statistics.
/// </summary>
public interface IVotingService
{
    /// <summary>
    /// Casts a new vote or updates an existing one for the specified poll.
    /// </summary>
    /// <param name="pollId">The poll to vote in.</param>
    /// <param name="userId">The authenticated user casting the vote.</param>
    /// <param name="rankedOptionIds">Ordered list of option IDs representing ranked preferences.</param>
    /// <returns>The vote response and whether this was a new vote (true) or an update (false).</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    /// <exception cref="InvalidOperationException">Poll is closed or deleted.</exception>
    /// <exception cref="ArgumentException">Option IDs are invalid (unknown or duplicated).</exception>
    Task<(VoteResponse Vote, bool IsNew)> CastVoteAsync(Guid pollId, Guid userId, List<Guid> rankedOptionIds);

    /// <summary>
    /// Retrieves the current user's vote for a poll, if one exists.
    /// </summary>
    /// <param name="pollId">The poll to look up.</param>
    /// <param name="userId">The user whose vote to retrieve.</param>
    /// <returns>The vote response, or <c>null</c> if the user has not voted.</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    Task<VoteResponse?> GetUserVoteAsync(Guid pollId, Guid userId);

    /// <summary>
    /// Returns participation statistics for a poll.
    /// </summary>
    /// <param name="pollId">The poll to count votes for.</param>
    /// <returns>Total votes and unique voter count.</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    Task<VoteCountResponse> GetVoteCountAsync(Guid pollId);
}
