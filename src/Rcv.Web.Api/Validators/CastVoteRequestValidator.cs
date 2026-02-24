using FluentValidation;
using Rcv.Web.Api.Models.Requests;

namespace Rcv.Web.Api.Validators;

/// <summary>
/// Validates <see cref="CastVoteRequest"/> before a vote is cast.
/// </summary>
public class CastVoteRequestValidator : AbstractValidator<CastVoteRequest>
{
    /// <summary>
    /// Initializes a new instance with all validation rules.
    /// </summary>
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.RankedOptionIds)
            .NotNull().WithMessage("RankedOptionIds is required.")
            .Must(ids => ids != null && ids.Count >= 1)
                .WithMessage("At least 1 ranked option is required.")
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
                .WithMessage("RankedOptionIds must not contain duplicates.");

        RuleForEach(x => x.RankedOptionIds)
            .NotEqual(Guid.Empty).WithMessage("Option IDs must not be empty GUIDs.");
    }
}
