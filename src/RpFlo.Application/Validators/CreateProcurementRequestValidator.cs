using FluentValidation;
using RpFlo.Application.DTOs;

namespace RpFlo.Application.Validators;

public sealed class CreateProcurementRequestValidator : AbstractValidator<CreateProcurementRequest>
{
    public CreateProcurementRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Department).IsInEnum().WithMessage("Invalid department.");
        RuleFor(x => x.Urgency).IsInEnum().WithMessage("Invalid urgency level.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.LineItems).SetValidator(new CreateLineItemRequestValidator());
    }
}
