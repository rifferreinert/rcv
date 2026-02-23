namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Represents a single option/candidate within a poll response.
/// </summary>
public record PollOptionDto(
    Guid Id,
    string Text,
    int DisplayOrder
);
