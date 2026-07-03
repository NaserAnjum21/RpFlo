using FluentValidation;
using RpFlo.Application.DTOs;

namespace RpFlo.Application.Validators;

public sealed class CreateLineItemRequestValidator : AbstractValidator<CreateLineItemRequest>
{
    public CreateLineItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(200).WithMessage("Item name must not exceed 200 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than zero.");
    }
}
