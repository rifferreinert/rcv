namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Describes the completeness of a poll result.
/// </summary>
public enum ResultsState
{
    /// <summary>No ballots have been cast.</summary>
    NoVotes,

    /// <summary>The result is a live snapshot while voting remains open.</summary>
    InProgress,

    /// <summary>The poll is closed and the result is final.</summary>
    Final,
}
