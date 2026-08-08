namespace Rcv.Web.Api.Data.Entities;

/// <summary>
/// Represents the persisted lifecycle state of a poll.
/// </summary>
public enum PollStatus
{
    /// <summary>The poll accepts votes.</summary>
    Active,

    /// <summary>The poll no longer accepts votes.</summary>
    Closed,

    /// <summary>The poll has been soft deleted.</summary>
    Deleted,
}
