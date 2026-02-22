using Rcv.Web.Api.Data.Entities;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Service responsible for user identity management and JWT token generation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Finds an existing user by their external OAuth identity, or creates a new one.
    /// Also updates <see cref="User.LastLoginAt"/> on every call.
    /// </summary>
    /// <param name="externalId">The unique user ID issued by the OAuth provider (e.g., Google subject).</param>
    /// <param name="provider">The provider name (e.g., "Google", "Microsoft").</param>
    /// <param name="email">The user's email address from the OAuth profile, if available.</param>
    /// <param name="displayName">The user's display name from the OAuth profile, if available.</param>
    /// <returns>The matched or newly created <see cref="User"/> entity.</returns>
    Task<User> GetOrCreateUserAsync(string externalId, string provider, string? email, string? displayName);

    /// <summary>
    /// Generates a signed JWT for the given user, containing their ID, email, and display name as claims.
    /// </summary>
    /// <param name="user">The user to generate a token for.</param>
    /// <returns>A signed JWT string.</returns>
    string GenerateJwtToken(User user);
}
