using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Models.Responses;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Service responsible for poll lifecycle management: creation, retrieval, updates, and closure.
/// </summary>
public interface IPollService
{
    /// <summary>
    /// Creates a new poll with the specified options.
    /// </summary>
    /// <param name="creatorId">ID of the authenticated user creating the poll.</param>
    /// <param name="request">The poll creation parameters.</param>
    /// <returns>The newly created poll.</returns>
    Task<PollResponse> CreatePollAsync(Guid creatorId, CreatePollRequest request);

    /// <summary>
    /// Retrieves a poll by its ID, including its options and vote count.
    /// </summary>
    /// <param name="pollId">The poll's unique identifier.</param>
    /// <returns>The poll, or <c>null</c> if not found (or soft-deleted).</returns>
    Task<PollResponse?> GetPollByIdAsync(Guid pollId);

    /// <summary>
    /// Lists all non-deleted polls created by the specified user, ordered by creation date descending.
    /// </summary>
    /// <param name="creatorId">The creator's user ID.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum results per page.</param>
    /// <returns>A paginated list of the creator's polls.</returns>
    Task<PollListResponse> GetPollsByCreatorAsync(Guid creatorId, int page, int pageSize);

    /// <summary>
    /// Lists all currently active (non-closed, non-deleted) polls, ordered by creation date descending.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum results per page.</param>
    /// <returns>A paginated list of active polls.</returns>
    Task<PollListResponse> GetActivePollsAsync(int page, int pageSize);

    /// <summary>
    /// Closes a poll early so no further votes can be cast.
    /// </summary>
    /// <param name="pollId">The poll to close.</param>
    /// <param name="requestingUserId">The user requesting the close; must be the poll creator.</param>
    /// <returns>The updated poll with Status = "Closed".</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Requesting user is not the creator.</exception>
    /// <exception cref="InvalidOperationException">Poll is already closed or deleted.</exception>
    Task<PollResponse> ClosePollAsync(Guid pollId, Guid requestingUserId);

    /// <summary>
    /// Soft-deletes a poll by setting its status to "Deleted".
    /// </summary>
    /// <param name="pollId">The poll to delete.</param>
    /// <param name="requestingUserId">The user requesting deletion; must be the poll creator.</param>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Requesting user is not the creator.</exception>
    Task DeletePollAsync(Guid pollId, Guid requestingUserId);

    /// <summary>
    /// Updates poll metadata. Only allowed before any votes have been cast.
    /// </summary>
    /// <param name="pollId">The poll to update.</param>
    /// <param name="requestingUserId">The user requesting the update; must be the poll creator.</param>
    /// <param name="request">The fields to update (null fields are left unchanged).</param>
    /// <returns>The updated poll.</returns>
    /// <exception cref="KeyNotFoundException">Poll not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Requesting user is not the creator.</exception>
    /// <exception cref="InvalidOperationException">Votes have already been cast, or poll is closed/deleted.</exception>
    Task<PollResponse> UpdatePollAsync(Guid pollId, Guid requestingUserId, UpdatePollRequest request);
}
