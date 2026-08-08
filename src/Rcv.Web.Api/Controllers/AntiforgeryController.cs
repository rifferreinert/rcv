using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// Issues antiforgery tokens for browser clients.
/// </summary>
[ApiController]
[Route("api/auth/csrf")]
public sealed class AntiforgeryController : ControllerBase
{
    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// Initializes the antiforgery controller.
    /// </summary>
    /// <param name="antiforgery">The ASP.NET Core antiforgery service.</param>
    public AntiforgeryController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Creates an antiforgery cookie and returns its matching request token.
    /// </summary>
    /// <returns>The request token to send in the X-XSRF-TOKEN header.</returns>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}
