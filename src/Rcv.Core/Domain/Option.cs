namespace Rcv.Core.Domain;

/// <summary>
/// Represents a poll option/candidate.
/// Immutable record with value equality based on Id and Label.
/// </summary>
/// <param name="Id">Unique identifier for this option</param>
/// <param name="Label">Human-readable name/description</param>
public record Option(Guid Id, string Label);
