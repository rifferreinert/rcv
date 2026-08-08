using FluentAssertions;
using Rcv.Web.Api.Models.Requests;
using Rcv.Web.Api.Validators;

namespace Rcv.Web.Api.Tests.Validators;

public sealed class UpdatePollRequestValidatorTests
{
    [Fact]
    public void Validate_RemoveDeadlineWithReplacementDeadline_IsInvalid()
    {
        var request = new UpdatePollRequest
        {
            ClosesAt = DateTime.UtcNow.AddDays(1),
            RemoveClosesAt = true,
        };

        var result = new UpdatePollRequestValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "ClosesAt cannot be provided when RemoveClosesAt is true.");
    }
}
