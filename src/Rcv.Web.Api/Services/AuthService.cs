using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rcv.Web.Api.Data;
using Rcv.Web.Api.Data.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Rcv.Web.Api.Services;

/// <summary>
/// Handles user identity management and JWT token generation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly RcvDbContext _context;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="configuration">Application configuration for JWT settings.</param>
    public AuthService(RcvDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<User> GetOrCreateUserAsync(string externalId, string provider, string? email, string? displayName)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.Provider == provider);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Provider = provider,
                Email = email,
                DisplayName = displayName,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    /// <inheritdoc />
    public string GenerateJwtToken(User user)
    {
        var jwtSection = _configuration.GetSection("Authentication:Jwt");
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expirationDays = int.Parse(jwtSection["ExpirationDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.DisplayName ?? string.Empty),
            new Claim("provider", user.Provider),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
