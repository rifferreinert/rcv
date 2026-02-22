namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Response DTO returned by the <c>GET /api/auth/me</c> endpoint.
/// Exposes only the fields safe to share with the client.
/// </summary>
/// <param name="Id">The user's internal GUID.</param>
/// <param name="Email">The user's email address, if available.</param>
/// <param name="DisplayName">The user's display name from their OAuth provider.</param>
/// <param name="Provider">The OAuth provider the user authenticated with (e.g., "Google").</param>
public record UserResponse(Guid Id, string? Email, string? DisplayName, string Provider);
