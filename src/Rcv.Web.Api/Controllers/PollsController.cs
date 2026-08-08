using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Rcv.Web.Api.Data.Entities;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// API endpoints for creating and managing ranked choice voting polls.
/// </summary>
[ApiController]
[Route("api/polls")]
public class PollsController : ControllerBase
{
    private readonly IPollService _pollService;

    /// <summary>
    /// Initializes a new instance of <see cref="PollsController"/>.
    /// </summary>
    /// <param name="pollService">The poll management service.</param>
    public PollsController(IPollService pollService)
    {
        _pollService = pollService;
    }

    /// <summary>
    /// Creates a new poll. Requires authentication.
    /// </summary>
    /// <param name="request">The poll creation parameters.</param>
    /// <returns>The newly created poll with HTTP 201.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePoll([FromBody] CreatePollRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var poll = await _pollService.CreatePollAsync(userId.Value, request);
        return CreatedAtAction(nameof(GetPoll), new { id = poll.Id }, poll);
    }

    /// <summary>
    /// Retrieves a poll by its ID. Public endpoint.
    /// </summary>
    /// <param name="id">The poll's unique identifier.</param>
    /// <returns>The poll details, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPoll(Guid id)
    {
        var poll = await _pollService.GetPollByIdAsync(id);
        if (poll is null)
            return NotFound();
        return Ok(poll);
    }

    /// <summary>
    /// Lists polls with optional filtering and pagination.
    /// </summary>
    /// <param name="status">Optional lifecycle status filter.</param>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (default 20).</param>
    /// <returns>A paginated list of polls.</returns>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ListPolls(
        [FromQuery] PollStatus? status = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();
        if (status.HasValue &&
            (!Enum.IsDefined(status.Value) || status == PollStatus.Deleted))
        {
            ModelState.AddModelError(nameof(status), "Status must be Active or Closed.");
            return ValidationProblem(ModelState);
        }
        return Ok(await _pollService.GetPollsByCreatorAsync(userId.Value, page, pageSize, status));
    }

    /// <summary>
    /// Updates a poll's metadata. Only the creator may update, and only before votes are cast.
    /// </summary>
    /// <param name="id">The poll's unique identifier.</param>
    /// <param name="request">The fields to update.</param>
    /// <returns>The updated poll, or an error status.</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdatePoll(Guid id, [FromBody] UpdatePollRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        return Ok(await _pollService.UpdatePollAsync(id, userId.Value, request));
    }

    /// <summary>
    /// Soft-deletes a poll. Only the creator may delete.
    /// </summary>
    /// <param name="id">The poll's unique identifier.</param>
    /// <returns>204 No Content on success, or an error status.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeletePoll(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _pollService.DeletePollAsync(id, userId.Value);
        return NoContent();
    }

    /// <summary>
    /// Closes a poll early so no further votes can be cast. Only the creator may close.
    /// </summary>
    /// <param name="id">The poll's unique identifier.</param>
    /// <returns>The updated poll with Status = "Closed", or an error status.</returns>
    [HttpPost("{id:guid}/close")]
    [Authorize]
    public async Task<IActionResult> ClosePoll(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        return Ok(await _pollService.ClosePollAsync(id, userId.Value));
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
