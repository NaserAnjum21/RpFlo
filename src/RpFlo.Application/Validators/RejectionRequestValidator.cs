using FluentValidation;
using RpFlo.Application.DTOs;

namespace RpFlo.Application.Validators;

public sealed class RejectionRequestValidator : AbstractValidator<RejectionRequest>
{
    public RejectionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}
