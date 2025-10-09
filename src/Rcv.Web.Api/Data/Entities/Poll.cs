using System.ComponentModel.DataAnnotations;

namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a ranked choice voting poll
/// </summary>
public class Poll
{
    /// <summary>
    /// Unique identifier for the poll
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Poll title/question
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User who created this poll
    /// </summary>
    public Guid CreatorId { get; set; }

    /// <summary>
    /// When the poll was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional deadline for poll closure (null = no deadline)
    /// </summary>
    public DateTime? ClosesAt { get; set; }

    /// <summary>
    /// Actual timestamp when poll was closed (manually or automatically)
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Whether results are visible while voting is in progress
    /// </summary>
    public bool IsResultsPublic { get; set; } = true;

    /// <summary>
    /// Whether individual votes are public (false = anonymous voting)
    /// </summary>
    public bool IsVotingPublic { get; set; } = false;

    /// <summary>
    /// Current status: Active, Closed, Deleted
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
