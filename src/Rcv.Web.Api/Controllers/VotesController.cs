using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Services;
using System.Security.Claims;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// API endpoints for casting and retrieving votes in ranked choice voting polls.
/// </summary>
[ApiController]
[Route("api/polls/{pollId:guid}/votes")]
public class VotesController : ControllerBase
{
    private readonly IVotingService _votingService;

    /// <summary>
    /// Initializes a new instance of <see cref="VotesController"/>.
    /// </summary>
    /// <param name="votingService">The voting service.</param>
    public VotesController(IVotingService votingService)
    {
        _votingService = votingService;
    }

    /// <summary>
    /// Casts a new vote or updates an existing one. Requires authentication.
    /// </summary>
    /// <param name="pollId">The poll to vote in.</param>
    /// <param name="request">The ranked option IDs.</param>
    /// <returns>The vote with 201 Created (new) or 200 OK (updated).</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CastVote(Guid pollId, [FromBody] CastVoteRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var (vote, isNew) = await _votingService.CastVoteAsync(pollId, userId.Value, request.RankedOptionIds);

            if (isNew)
                return CreatedAtAction(nameof(GetMyVote), new { pollId }, vote);

            return Ok(vote);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the current user's vote for a poll. Requires authentication.
    /// </summary>
    /// <param name="pollId">The poll to look up.</param>
    /// <returns>The vote, or 404 if not found.</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyVote(Guid pollId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var vote = await _votingService.GetUserVoteAsync(pollId, userId.Value);
            if (vote is null)
                return NotFound();
            return Ok(vote);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Returns participation statistics for a poll. Public endpoint.
    /// </summary>
    /// <param name="pollId">The poll to count votes for.</param>
    /// <returns>Total votes and unique voter count.</returns>
    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVoteCount(Guid pollId)
    {
        try
        {
            var count = await _votingService.GetVoteCountAsync(pollId);
            return Ok(count);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Extracts the authenticated user's ID from the JWT claims.
    /// Returns null if the claim is missing or malformed.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
