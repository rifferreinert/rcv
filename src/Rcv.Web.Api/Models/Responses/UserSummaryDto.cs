namespace Rcv.Web.Api.Models.Responses;

/// <summary>
/// Compact user representation embedded in poll responses.
/// </summary>
public record UserSummaryDto(
    Guid Id,
    string? Email,
    string? DisplayName
);
