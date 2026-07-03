using FluentAssertions;
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
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineItem_NegativePrice_ShouldFail()
    {
        var request = new CreateProcurementRequest("Title", "Desc", Department.Engineering, Urgency.Low, [new("Item", 1, -5m)]);
        var result = _validator.TestValidate(request);
        result.IsValid.Should().BeFalse();
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

        result.IsValid.Should().BeFalse();
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
}
