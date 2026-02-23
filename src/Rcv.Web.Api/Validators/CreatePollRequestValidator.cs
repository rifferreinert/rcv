using FluentValidation;
using Rcv.Web.Api.Models.Requests;

namespace Rcv.Web.Api.Validators;

/// <summary>
/// Validates <see cref="CreatePollRequest"/> before a poll is created.
/// </summary>
public class CreatePollRequestValidator : AbstractValidator<CreatePollRequest>
{
    /// <summary>
    /// Initializes a new instance with all validation rules.
    /// </summary>
    public CreatePollRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Options)
            .NotNull().WithMessage("Options are required.")
            .Must(opts => opts != null && opts.Count >= 2)
                .WithMessage("At least 2 options are required.")
            .Must(opts => opts == null || opts.Count <= 50)
                .WithMessage("A poll may have at most 50 options.");

        RuleForEach(x => x.Options)
            .NotEmpty().WithMessage("Option text must not be empty.")
            .MaximumLength(500).WithMessage("Each option must not exceed 500 characters.");

        RuleFor(x => x.ClosesAt)
            .Must(d => d == null || d.Value > DateTime.UtcNow)
            .WithMessage("ClosesAt must be a future date/time.");
    }
}
