using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Rcv.Web.Api.Infrastructure;

/// <summary>
/// Validates JWT signing key configuration before cryptographic use.
/// </summary>
public static class JwtSigningKeyValidator
{
    private const string ConfigurationPath = "Authentication:Jwt:SecretKey";

    /// <summary>
    /// Creates a signing key after enforcing production-safe configuration requirements.
    /// </summary>
    /// <param name="configuredValue">The configured signing secret.</param>
    /// <returns>A symmetric key containing at least 256 bits of configured key material.</returns>
    /// <exception cref="InvalidOperationException">The key is missing, a placeholder, or shorter than 32 bytes.</exception>
    public static SymmetricSecurityKey CreateSecurityKey(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
            throw new InvalidOperationException(
                $"{ConfigurationPath} is missing. Configure a secret containing at least 32 bytes.");

        if (IsPlaceholder(configuredValue))
            throw new InvalidOperationException(
                $"{ConfigurationPath} contains a placeholder and must be replaced with a real secret.");

        var keyBytes = Encoding.UTF8.GetBytes(configuredValue);
        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                $"{ConfigurationPath} must contain at least 32 bytes; the configured value is {keyBytes.Length} bytes.");

        return new SymmetricSecurityKey(keyBytes);
    }

    private static bool IsPlaceholder(string value) =>
        value.Contains("REPLACE", StringComparison.OrdinalIgnoreCase) ||
        (value.StartsWith('{') && value.EndsWith('}'));
}
