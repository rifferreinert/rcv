using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rcv.Web.Api.Models.Responses;
using Rcv.Web.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Rcv.Web.Api.Controllers;

/// <summary>
/// Handles OAuth2 authentication flows and user identity endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
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
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="configuration">Application redirect and CORS configuration.</param>
    /// <param name="environment">The current hosting environment.</param>
    public AuthController(
        IAuthService authService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Initiates an OAuth2 login flow for the specified provider.
    /// The browser is redirected to the provider's consent screen.
    /// </summary>
    /// <param name="provider">The OAuth provider name (e.g., "google", "microsoft").</param>
    /// <param name="returnUrl">Optional trusted frontend destination after authentication.</param>
    [HttpGet("login/{provider}")]
    public IActionResult Login(string provider, [FromQuery] string? returnUrl = null)
    {
        if (!SupportedProviders.TryGetValue(provider, out var scheme))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unknown OAuth provider",
                detail: $"Supported providers: {string.Join(", ", SupportedProviders.Keys)}.");

        // After the provider redirects back, ASP.NET will call our /callback route
        var callbackUrl = Url.Action(nameof(Callback), new { provider })!;
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl };
        properties.Items["returnUrl"] = GetSafeReturnUrl(returnUrl);

        return Challenge(properties, scheme);
    }

    /// <summary>
    /// Handles the OAuth2 callback after the provider redirects back.
    /// Retrieves or creates the user, issues a JWT as an httpOnly cookie,
    /// then redirects the browser to the frontend dashboard.
    /// </summary>
    /// <param name="provider">The OAuth provider name.</param>
    /// <returns>A redirect to the configured frontend destination.</returns>
    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider)
    {
        if (!SupportedProviders.ContainsKey(provider))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Unknown OAuth provider");

        // Authenticate using the temporary external cookie set by the OAuth middleware
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded)
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "OAuth authentication failed");

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
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });

        // Remove the temporary external cookie
        await HttpContext.SignOutAsync("External");

        // Redirect the browser back to the frontend
        string? returnUrl = null;
        result.Properties?.Items.TryGetValue("returnUrl", out returnUrl);
        return Redirect(GetSafeReturnUrl(returnUrl));
    }

    /// <summary>
    /// Logs the user out by expiring the JWT cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Append(JwtCookieName, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
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

    private string GetSafeReturnUrl(string? requested)
    {
        var configured = _configuration["Authentication:OAuth:ReturnUrl"] ?? "/dashboard";
        var candidate = string.IsNullOrWhiteSpace(requested) ? configured : requested;

        if (IsSafeReturnUrl(candidate))
            return candidate;

        return IsSafeReturnUrl(configured) ? configured : "/dashboard";
    }

    private bool IsSafeReturnUrl(string candidate)
    {
        if (IsSafeRelativeUrl(candidate))
            return true;

        if (Uri.TryCreate(candidate, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            if (allowedOrigins.Any(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var allowed) &&
                string.Equals(allowed.Scheme, absolute.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(allowed.Host, absolute.Host, StringComparison.OrdinalIgnoreCase) &&
                allowed.Port == absolute.Port))
                return true;
        }

        return false;
    }

    private static bool IsSafeRelativeUrl(string url) =>
        Uri.TryCreate(url, UriKind.Relative, out _) &&
        url.StartsWith('/') &&
        !url.StartsWith("//", StringComparison.Ordinal) &&
        !url.Contains('\\') &&
        !url.Any(char.IsControl);
}
