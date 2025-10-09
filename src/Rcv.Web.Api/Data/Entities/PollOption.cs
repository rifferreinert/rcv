using System.ComponentModel.DataAnnotations;

namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a single option/candidate in a poll
/// </summary>
public class PollOption
{
    /// <summary>
    /// Unique identifier for this option
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Poll this option belongs to
    /// </summary>
    public Guid PollId { get; set; }

    /// <summary>
    /// The option text/label
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string OptionText { get; set; } = string.Empty;

    /// <summary>
    /// Display order (for consistent presentation to voters)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When this option was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Poll Poll { get; set; } = null!;
}
