using FluentAssertions;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Domain.Tests;

public class ProcurementRequestTests
{
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid FinanceId = Guid.NewGuid();

    private static ProcurementRequest CreateDraftWithItems()
    {
        var pr = ProcurementRequest.Create("Test", "Description", Department.Engineering, Urgency.Medium, RequesterId);
        pr.AddLineItem("Item 1", 2, 100m);
        return pr;
    }

    [Fact]
    public void Create_ShouldSetDraftStatus()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.High, RequesterId);

        pr.Status.Should().Be(ProcurementStatus.Draft);
        pr.RequesterId.Should().Be(RequesterId);
        pr.Title.Should().Be("Test");
    }

    [Fact]
    public void AddLineItem_InDraft_ShouldSucceed()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, RequesterId);
        var result = pr.AddLineItem("Laptop", 5, 1000m);

        result.IsSuccess.Should().BeTrue();
        pr.LineItems.Should().HaveCount(1);
        pr.TotalAmount.Amount.Should().Be(5000m);
    }

    [Fact]
    public void AddLineItem_NotInDraft_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.AddLineItem("Extra", 1, 50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotModify");
    }

    [Fact]
    public void Submit_WithLineItems_ShouldTransitionToSubmitted()
    {
        var pr = CreateDraftWithItems();
        var result = pr.Submit(RequesterId);

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.Submitted);
        pr.AuditEntries.Should().HaveCount(1);
    }

    [Fact]
    public void Submit_WithoutLineItems_ShouldFail()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, RequesterId);
        var result = pr.Submit(RequesterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoLineItems");
    }

    [Fact]
    public void Submit_ByNonOwner_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        var result = pr.Submit(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotOwner");
    }

    [Fact]
    public void ApproveByManager_FromSubmitted_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.ApproveByManager(ManagerId, "Looks good");

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.ManagerApproved);
        pr.AuditEntries.Should().HaveCount(2);
    }

    [Fact]
    public void ApproveByManager_FromDraft_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        var result = pr.ApproveByManager(ManagerId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTransition");
    }

    [Fact]
    public void RejectByManager_ShouldTransitionToManagerRejected()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.RejectByManager(ManagerId, "Need more details");

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.ManagerRejected);
    }

    [Fact]
    public void ApproveByFinance_FromManagerApproved_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);
        pr.ApproveByManager(ManagerId);

        var result = pr.ApproveByFinance(FinanceId);

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.FinanceApproved);
    }

    [Fact]
    public void ApproveByFinance_FromSubmitted_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.ApproveByFinance(FinanceId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IssuePurchaseOrder_FromFinanceApproved_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);
        pr.ApproveByManager(ManagerId);
        pr.ApproveByFinance(FinanceId);

        var result = pr.IssuePurchaseOrder(FinanceId);

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.PurchaseOrderIssued);
        pr.PoNumber.Should().NotBeNullOrEmpty();
        pr.PoNumber.Should().StartWith("PO-");
    }

    [Fact]
    public void ReviseToDraft_FromManagerRejected_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);
        pr.RejectByManager(ManagerId, "Too expensive");

        var result = pr.ReviseToDraft(RequesterId);

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.Draft);
    }

    [Fact]
    public void ReviseToDraft_FromFinanceRejected_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);
        pr.ApproveByManager(ManagerId);
        pr.RejectByFinance(FinanceId, "Over budget");

        var result = pr.ReviseToDraft(RequesterId);

        result.IsSuccess.Should().BeTrue();
        pr.Status.Should().Be(ProcurementStatus.Draft);
    }

    [Fact]
    public void ReviseToDraft_FromSubmitted_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.ReviseToDraft(RequesterId);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ReviseToDraft_ByNonOwner_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);
        pr.RejectByManager(ManagerId, "No");

        var result = pr.ReviseToDraft(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotOwner");
    }

    [Fact]
    public void FullHappyPath_ShouldWorkEndToEnd()
    {
        var pr = CreateDraftWithItems();

        pr.Submit(RequesterId).IsSuccess.Should().BeTrue();
        pr.ApproveByManager(ManagerId).IsSuccess.Should().BeTrue();
        pr.ApproveByFinance(FinanceId).IsSuccess.Should().BeTrue();
        pr.IssuePurchaseOrder(FinanceId).IsSuccess.Should().BeTrue();

        pr.Status.Should().Be(ProcurementStatus.PurchaseOrderIssued);
        pr.AuditEntries.Should().HaveCount(4);
    }

    [Fact]
    public void RejectReviseResubmit_ShouldWorkEndToEnd()
    {
        var pr = CreateDraftWithItems();

        pr.Submit(RequesterId).IsSuccess.Should().BeTrue();
        pr.RejectByManager(ManagerId, "Fix it").IsSuccess.Should().BeTrue();
        pr.ReviseToDraft(RequesterId).IsSuccess.Should().BeTrue();
        pr.Submit(RequesterId).IsSuccess.Should().BeTrue();
        pr.ApproveByManager(ManagerId).IsSuccess.Should().BeTrue();
        pr.ApproveByFinance(FinanceId).IsSuccess.Should().BeTrue();
        pr.IssuePurchaseOrder(FinanceId).IsSuccess.Should().BeTrue();

        pr.Status.Should().Be(ProcurementStatus.PurchaseOrderIssued);
    }

    [Fact]
    public void AddComment_ShouldWork()
    {
        var pr = CreateDraftWithItems();
        var comment = pr.AddComment(RequesterId, "Need this urgently");

        comment.Text.Should().Be("Need this urgently");
        pr.Comments.Should().HaveCount(1);
    }

    [Fact]
    public void Update_InDraft_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        var result = pr.Update("New Title", "New Desc", Department.Marketing, Urgency.Critical);

        result.IsSuccess.Should().BeTrue();
        pr.Title.Should().Be("New Title");
        pr.Department.Should().Be(Department.Marketing);
    }

    [Fact]
    public void Update_NotInDraft_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        var result = pr.Update("New Title", "New Desc", Department.Marketing, Urgency.Critical);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveLineItem_InDraft_ShouldSucceed()
    {
        var pr = CreateDraftWithItems();
        var itemId = pr.LineItems[0].Id;

        var result = pr.RemoveLineItem(itemId);

        result.IsSuccess.Should().BeTrue();
        pr.LineItems.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLineItem_NotFound_ShouldFail()
    {
        var pr = CreateDraftWithItems();
        var result = pr.RemoveLineItem(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void DomainEvents_ShouldBeRaised()
    {
        var pr = CreateDraftWithItems();
        pr.Submit(RequesterId);

        pr.DomainEvents.Should().HaveCount(1);
        pr.ClearDomainEvents();
        pr.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void TotalAmount_ShouldSumLineItems()
    {
        var pr = ProcurementRequest.Create("Test", "Desc", Department.Engineering, Urgency.Low, RequesterId);
        pr.AddLineItem("A", 2, 100m);
        pr.AddLineItem("B", 3, 50m);

        pr.TotalAmount.Amount.Should().Be(350m);
    }
}
