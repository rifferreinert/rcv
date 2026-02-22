using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;
using System.Security.Claims;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// Handles OAuth2 authentication flows and user identity endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Maps lowercase provider names in the URL to the scheme name registered with AddGoogle/AddMicrosoftAccount
    private static readonly Dictionary<string, string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["google"] = "Google",
        ["microsoft"] = "Microsoft",
    };

    private const string JwtCookieName = "rcv_jwt";

    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Initiates an OAuth2 login flow for the specified provider.
    /// The browser is redirected to the provider's consent screen.
    /// </summary>
    /// <param name="provider">The OAuth provider name (e.g., "google", "microsoft").</param>
    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider)
    {
        if (!SupportedProviders.TryGetValue(provider, out var scheme))
            return BadRequest($"Unknown provider '{provider}'. Supported: {string.Join(", ", SupportedProviders.Keys)}");

        // After the provider redirects back, ASP.NET will call our /callback route
        var callbackUrl = Url.Action(nameof(Callback), new { provider })!;
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };

        return Challenge(properties, scheme);
    }

    /// <summary>
    /// Handles the OAuth2 callback after the provider redirects back.
    /// Retrieves or creates the user, issues a JWT as an httpOnly cookie,
    /// then redirects the browser to the frontend dashboard.
    /// </summary>
    /// <param name="provider">The OAuth provider name.</param>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider)
    {
        if (!SupportedProviders.ContainsKey(provider))
            return BadRequest($"Unknown provider '{provider}'.");

        // Authenticate using the temporary external cookie set by the OAuth middleware
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded)
            return Unauthorized("OAuth authentication failed.");

        // Extract identity claims provided by the OAuth provider
        var externalId = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Provider did not return a user ID claim.");
        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        var displayName = result.Principal?.FindFirstValue(ClaimTypes.Name);

        // Normalise to the canonical provider name (e.g. "Google", "Microsoft")
        var canonicalProvider = SupportedProviders[provider];

        var user = await _authService.GetOrCreateUserAsync(externalId, canonicalProvider, email, displayName);
        var jwt = _authService.GenerateJwtToken(user);

        // Issue the JWT as an httpOnly cookie so JavaScript cannot access it
        Response.Cookies.Append(JwtCookieName, jwt, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });

        // Remove the temporary external cookie
        await HttpContext.SignOutAsync("External");

        // Redirect the browser back to the frontend
        return Redirect("/dashboard");
    }

    /// <summary>
    /// Logs the user out by expiring the JWT cookie.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Append(JwtCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UnixEpoch, // Far in the past → browser deletes the cookie
        });

        return Ok();
    }

    /// <summary>
    /// Returns the currently authenticated user's profile.
    /// Requires a valid JWT cookie.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(ClaimTypes.Name);
        var provider = User.FindFirstValue("provider") ?? string.Empty;

        var response = new UserResponse(userId, email, displayName, provider);

        return Ok(response);
    }
}
