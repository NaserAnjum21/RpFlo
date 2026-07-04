using FluentValidation.TestHelper;
using RpFlo.Application.DTOs;
using RpFlo.Application.Validators;
using RpFlo.Domain.Enums;

namespace RpFlo.Application.Tests;

public class CreateProcurementRequestValidatorTests
{
    private readonly CreateProcurementRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_ShouldPass()
    {
        var request = new CreateProcurementRequest(
            "Laptops",
            "Need new laptops",
            Department.Engineering,
            Urgency.High,
            [new("MacBook", 5, 2000m)]);

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Title_ShouldFail()
    {
        var request = new CreateProcurementRequest("", "Desc", Department.Engineering, Urgency.Low, [new("Item", 1, 10m)]);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Empty_LineItems_ShouldFail()
    {
        var request = new CreateProcurementRequest("Title", "Desc", Department.Engineering, Urgency.Low, []);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LineItems);
    }

    [Fact]
    public void Title_TooLong_ShouldFail()
    {
        var request = new CreateProcurementRequest(
            new string('A', 201), "Desc", Department.Engineering, Urgency.Low, [new("Item", 1, 10m)]);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void LineItem_ZeroQuantity_ShouldFail()
    {
        var request = new CreateProcurementRequest("Title", "Desc", Department.Engineering, Urgency.Low, [new("Item", 0, 10m)]);
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("LineItems[0].Quantity")
            .WithErrorMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void LineItem_NegativePrice_ShouldFail()
    {
        var request = new CreateProcurementRequest("Title", "Desc", Department.Engineering, Urgency.Low, [new("Item", 1, -5m)]);
        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("LineItems[0].UnitPrice")
            .WithErrorMessage("Unit price must be greater than zero.");
    }
}

public class CreateLineItemRequestValidatorTests
{
    private readonly CreateLineItemRequestValidator _validator = new();

    [Fact]
    public void Valid_LineItem_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateLineItemRequest("Monitor", 2, 300m));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Item name is required.")]
    [InlineData("   ", "Item name is required.")]
    public void Empty_Name_ShouldFail(string name, string expectedMessage)
    {
        var result = _validator.TestValidate(new CreateLineItemRequest(name, 1, 10m));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage(expectedMessage);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateLineItemRequest(new string('A', 201), 1, 10m));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Item name must not exceed 200 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositive_Quantity_ShouldFail(int quantity)
    {
        var result = _validator.TestValidate(new CreateLineItemRequest("Item", quantity, 10m));

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositive_UnitPrice_ShouldFail(decimal unitPrice)
    {
        var result = _validator.TestValidate(new CreateLineItemRequest("Item", 1, unitPrice));

        result.ShouldHaveValidationErrorFor(x => x.UnitPrice)
            .WithErrorMessage("Unit price must be greater than zero.");
    }
}

public class UpdateProcurementRequestValidatorTests
{
    private readonly UpdateProcurementRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateProcurementRequest(
            "Updated laptops",
            "Refresh engineering laptops",
            Department.Engineering,
            Urgency.High));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Title_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProcurementRequest(
            "",
            "Description",
            Department.Engineering,
            Urgency.Low));

        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void Description_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProcurementRequest(
            "Title",
            new string('A', 2001),
            Department.Engineering,
            Urgency.Low));

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters.");
    }

    [Fact]
    public void Invalid_EnumValues_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateProcurementRequest(
            "Title",
            "Description",
            (Department)999,
            (Urgency)999));

        result.ShouldHaveValidationErrorFor(x => x.Department)
            .WithErrorMessage("Invalid department.");
        result.ShouldHaveValidationErrorFor(x => x.Urgency)
            .WithErrorMessage("Invalid urgency level.");
    }
}

public class RejectionRequestValidatorTests
{
    private readonly RejectionRequestValidator _validator = new();

    [Fact]
    public void Empty_Reason_ShouldFail()
    {
        var result = _validator.TestValidate(new RejectionRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Valid_Reason_ShouldPass()
    {
        var result = _validator.TestValidate(new RejectionRequest("Too expensive"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Reason_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new RejectionRequest(new string('A', 1001)));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

public class AddLineItemsRequestValidatorTests
{
    private readonly AddLineItemsRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_ShouldPass()
    {
        var result = _validator.TestValidate(new AddLineItemsRequest([new("Monitor", 2, 300m)]));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_LineItems_ShouldFail()
    {
        var result = _validator.TestValidate(new AddLineItemsRequest([]));

        result.ShouldHaveValidationErrorFor(x => x.LineItems);
    }

    [Fact]
    public void Invalid_LineItem_ShouldFail()
    {
        var result = _validator.TestValidate(new AddLineItemsRequest([new("", 0, -1m)]));

        result.ShouldHaveValidationErrorFor("LineItems[0].Name")
            .WithErrorMessage("Item name is required.");
        result.ShouldHaveValidationErrorFor("LineItems[0].Quantity")
            .WithErrorMessage("Quantity must be greater than zero.");
        result.ShouldHaveValidationErrorFor("LineItems[0].UnitPrice")
            .WithErrorMessage("Unit price must be greater than zero.");
    }
}

public class AddCommentRequestValidatorTests
{
    private readonly AddCommentRequestValidator _validator = new();

    [Fact]
    public void Empty_Text_ShouldFail()
    {
        var result = _validator.TestValidate(new AddCommentRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Valid_Text_ShouldPass()
    {
        var result = _validator.TestValidate(new AddCommentRequest("Great idea!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Text_TooLong_ShouldFail()
    {
        var result = _validator.TestValidate(new AddCommentRequest(new string('A', 2001)));

        result.ShouldHaveValidationErrorFor(x => x.Text)
            .WithErrorMessage("Comment must not exceed 2000 characters.");
    }
}
