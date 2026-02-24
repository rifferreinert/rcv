using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Services;
using System.Security.Claims;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// API endpoints for retrieving ranked choice voting results.
/// </summary>
[ApiController]
[Route("api/polls/{pollId:guid}/results")]
public class ResultsController : ControllerBase
{
    private readonly IResultsService _resultsService;
    private readonly IPollService _pollService;

    /// <summary>
    /// Initializes a new instance of <see cref="ResultsController"/>.
    /// </summary>
    public ResultsController(IResultsService resultsService, IPollService pollService)
    {
        _resultsService = resultsService;
        _pollService = pollService;
    }

    /// <summary>
    /// Returns the ranked choice voting results for a poll.
    /// Visibility rules: creator can always see results; anyone can see results of closed polls;
    /// others can see live results only if IsResultsPublic is true.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(Guid pollId)
    {
        try
        {
            var poll = await _pollService.GetPollByIdAsync(pollId);
            if (poll is null)
                return NotFound();

            var userId = GetCurrentUserId();
            var isCreator = userId != null && userId == poll.Creator.Id;

            // Authorization: creator always sees results; anyone if closed; otherwise check IsResultsPublic
            if (!isCreator && poll.Status != "Closed" && !poll.IsResultsPublic)
                return Forbid();

            var result = await _resultsService.CalculateResultsAsync(pollId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Returns live results for an active poll.
    /// Only available if the poll has IsResultsPublic enabled, or if the requester is the creator.
    /// </summary>
    [HttpGet("live")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLiveResults(Guid pollId)
    {
        try
        {
            var poll = await _pollService.GetPollByIdAsync(pollId);
            if (poll is null)
                return NotFound();

            var userId = GetCurrentUserId();
            var isCreator = userId != null && userId == poll.Creator.Id;

            if (!isCreator && !poll.IsResultsPublic)
                return Forbid();

            var result = await _resultsService.CalculateResultsAsync(pollId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

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
