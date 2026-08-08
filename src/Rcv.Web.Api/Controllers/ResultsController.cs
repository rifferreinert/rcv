using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Services;
using System.Security.Claims;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// Exposes ranked-choice results for a poll.
/// </summary>
[ApiController]
[Route("api/polls/{pollId:guid}/results")]
public sealed class ResultsController : ControllerBase
{
    private readonly IResultsService _resultsService;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    /// <param name="resultsService">The poll results service.</param>
    public ResultsController(IResultsService resultsService)
    {
        _resultsService = resultsService;
    }

    /// <summary>
    /// Gets live or final results when the poll visibility permits it.
    /// </summary>
    /// <param name="pollId">The poll identifier.</param>
    /// <returns>The authorized live or final result.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(Guid pollId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(claim, out var parsed) ? parsed : (Guid?)null;
        return Ok(await _resultsService.GetResultsAsync(pollId, userId));
    }
}
