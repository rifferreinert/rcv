using Rcv.Web.Api.Data.Entities;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Resolves and persists time-based poll lifecycle transitions.
/// </summary>
public interface IPollLifecycleService
{
    /// <summary>
    /// Closes an active poll whose deadline has been reached.
    /// </summary>
    /// <param name="poll">The tracked poll entity.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns><see langword="true"/> when this call persisted a transition.</returns>
    Task<bool> ApplyEffectiveStatusAsync(Poll poll, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies deadline transitions to active polls owned by a creator.
    /// </summary>
    /// <param name="creatorId">The poll creator identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The number of polls transitioned.</returns>
    Task<int> ApplyEffectiveStatusesForCreatorAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default);
}
