namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents a single user's vote in a poll
/// Stores ranked choices as JSON array of option IDs
/// </summary>
public class Vote
{
    /// <summary>
    /// Unique identifier for this vote
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Poll this vote belongs to
    /// </summary>
    public Guid PollId { get; set; }

    /// <summary>
    /// User who cast this vote
    /// </summary>
    public Guid VoterId { get; set; }

    /// <summary>
    /// Ordered list of option IDs representing voter's ranked preferences
    /// Stored as JSON in database: ["guid1", "guid2", "guid3"]
    /// EF Core 9.0 natively maps this to Azure SQL's JSON type
    /// </summary>
    public List<Guid> RankedChoices { get; set; } = new();

    /// <summary>
    /// When the vote was originally cast
    /// </summary>
    public DateTime CastAt { get; set; }

    /// <summary>
    /// Last time the vote was updated (if vote changes are allowed)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Poll Poll { get; set; } = null!;
    public User Voter { get; set; } = null!;
}
