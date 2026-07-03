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

public sealed class AddLineItemsRequestValidator : AbstractValidator<AddLineItemsRequest>
{
    public AddLineItemsRequestValidator()
    {
        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.LineItems).SetValidator(new CreateLineItemRequestValidator());
    }
}

public sealed class UpdateProcurementRequestValidator : AbstractValidator<UpdateProcurementRequest>
{
    public UpdateProcurementRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Department).IsInEnum().WithMessage("Invalid department.");
        RuleFor(x => x.Urgency).IsInEnum().WithMessage("Invalid urgency level.");
    }
}

public sealed class RejectionRequestValidator : AbstractValidator<RejectionRequest>
{
    public RejectionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
    }
}

public sealed class AddCommentRequestValidator : AbstractValidator<AddCommentRequest>
{
    public AddCommentRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text is required.")
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters.");
    }
}
