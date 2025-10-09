using System.ComponentModel.DataAnnotations;

namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a user authenticated via external OAuth provider (Slack, Teams, Google, Apple, Microsoft)
/// </summary>
public class User
{
    /// <summary>
    /// Internal unique identifier for the user
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// External provider's unique user ID
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth provider name (e.g., "Slack", "Google", "Microsoft")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// User's email address from OAuth provider
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// User's display name from OAuth provider
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// When the user record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last time user logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Poll> CreatedPolls { get; set; } = new List<Poll>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
