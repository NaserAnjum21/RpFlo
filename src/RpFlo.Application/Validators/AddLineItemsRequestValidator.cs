using FluentValidation;
using RpFlo.Application.DTOs;

namespace RpFlo.Application.Validators;

public sealed class AddLineItemsRequestValidator : AbstractValidator<AddLineItemsRequest>
{
    public AddLineItemsRequestValidator()
    {
        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.LineItems).SetValidator(new CreateLineItemRequestValidator());
    }
}
