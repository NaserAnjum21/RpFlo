using RpFlo.Domain.Common;
using RpFlo.Domain.Enums;
using RpFlo.Domain.Events;
using RpFlo.Domain.ValueObjects;

namespace RpFlo.Domain.Entities;

public sealed class ProcurementRequest : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Department Department { get; private set; }
    public Urgency Urgency { get; private set; }
    public ProcurementStatus Status { get; private set; } = ProcurementStatus.Draft;
    public Guid RequesterId { get; private init; }
    public string? PoNumber { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<LineItem> _lineItems = [];
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    private readonly List<AuditEntry> _auditEntries = [];
    public IReadOnlyList<AuditEntry> AuditEntries => _auditEntries.AsReadOnly();

    private readonly List<Comment> _comments = [];
    public IReadOnlyList<Comment> Comments => _comments.AsReadOnly();

    public Money TotalAmount => _lineItems
        .Select(li => li.TotalPrice)
        .Aggregate(Money.Zero(), (acc, price) => acc.Add(price));

    private ProcurementRequest() { }

    public static ProcurementRequest Create(
        string title,
        string description,
        Department department,
        Urgency urgency,
        Guid requesterId) =>
        new()
        {
            Title = title,
            Description = description,
            Department = department,
            Urgency = urgency,
            RequesterId = requesterId
        };

    public Result<ProcurementRequest> Update(
        string title,
        string description,
        Department department,
        Urgency urgency)
    {
        if (Status is not ProcurementStatus.Draft)
        {
            return DomainErrors.CannotModify(Status);
        }

        Title = title;
        Description = description;
        Department = department;
        Urgency = urgency;
        Touch();
        return this;
    }

    public Result<LineItem> AddLineItem(string name, int quantity, decimal unitPrice)
    {
        if (Status is not ProcurementStatus.Draft)
        {
            return DomainErrors.CannotModify(Status);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(code: "LineItemNameRequired", message: "Line item name is required.");
        }

        if (quantity <= 0)
        {
            return Error.Validation(code: "InvalidQuantity", message: "Quantity must be greater than zero.");
        }

        if (unitPrice <= 0)
        {
            return Error.Validation(code: "InvalidUnitPrice", message: "Unit price must be greater than zero.");
        }

        var item = LineItem.Create(name, quantity, unitPrice, requestId: Id);
        _lineItems.Add(item);
        Touch();
        return item;
    }

    public Result<ProcurementRequest> RemoveLineItem(Guid lineItemId)
    {
        if (Status is not ProcurementStatus.Draft)
        {
            return DomainErrors.CannotModify(Status);
        }

        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
        {
            return Error.NotFound(code: "LineItem", message: $"Line item {lineItemId} not found.");
        }

        _lineItems.Remove(item);
        Touch();
        return this;
    }

    public Result<ProcurementRequest> Submit(Guid requesterId)
    {
        if (Status is not ProcurementStatus.Draft)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.Submitted);
        }

        if (_lineItems.Count == 0)
        {
            return Error.Validation(code: "NoLineItems", message: "Cannot submit a request with no line items.");
        }

        if (RequesterId != requesterId)
        {
            return Error.Unauthorized(code: "NotOwner", message: "Only the requester can submit this request.");
        }

        var previousStatus = Status;
        Status = ProcurementStatus.Submitted;
        Touch();
        AddAudit(requesterId, action: "Submitted", previousStatus, Status);
        RaiseDomainEvent(new ProcurementSubmitted(Id, requesterId, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> ApproveByManager(Guid approverId, string? comment = null)
    {
        if (Status is not ProcurementStatus.Submitted)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.ManagerApproved);
        }

        var previousStatus = Status;
        Status = ProcurementStatus.ManagerApproved;
        Touch();
        AddAudit(approverId, action: "Manager Approved", previousStatus, Status, comment);
        RaiseDomainEvent(new ProcurementApprovedByManager(Id, approverId, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> RejectByManager(Guid reviewerId, string reason)
    {
        if (Status is not ProcurementStatus.Submitted)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.ManagerRejected);
        }

        var previousStatus = Status;
        Status = ProcurementStatus.ManagerRejected;
        Touch();
        AddAudit(reviewerId, action: "Manager Rejected", previousStatus, Status, reason);
        RaiseDomainEvent(new ProcurementRejectedByManager(Id, reviewerId, reason, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> ApproveByFinance(Guid approverId, string? comment = null)
    {
        if (Status is not ProcurementStatus.ManagerApproved)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.FinanceApproved);
        }

        var previousStatus = Status;
        Status = ProcurementStatus.FinanceApproved;
        Touch();
        AddAudit(approverId, action: "Finance Approved", previousStatus, Status, comment);
        RaiseDomainEvent(new ProcurementApprovedByFinance(Id, approverId, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> RejectByFinance(Guid reviewerId, string reason)
    {
        if (Status is not ProcurementStatus.ManagerApproved)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.FinanceRejected);
        }

        var previousStatus = Status;
        Status = ProcurementStatus.FinanceRejected;
        Touch();
        AddAudit(reviewerId, action: "Finance Rejected", previousStatus, Status, reason);
        RaiseDomainEvent(new ProcurementRejectedByFinance(Id, reviewerId, reason, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> IssuePurchaseOrder(Guid issuerId)
    {
        if (Status is not ProcurementStatus.FinanceApproved)
        {
            return DomainErrors.InvalidTransition(Status, ProcurementStatus.PurchaseOrderIssued);
        }

        var previousStatus = Status;
        PoNumber = GeneratePoNumber();
        Status = ProcurementStatus.PurchaseOrderIssued;
        Touch();
        AddAudit(issuerId, action: "PO Issued", previousStatus, Status, $"PO Number: {PoNumber}");
        RaiseDomainEvent(new PurchaseOrderIssued(Id, PoNumber, DateTimeOffset.UtcNow));
        return this;
    }

    public Result<ProcurementRequest> ReviseToDraft(Guid requesterId)
    {
        if (Status is not (ProcurementStatus.ManagerRejected or ProcurementStatus.FinanceRejected))
        {
            return Error.Domain(code: "InvalidRevision", message: "Only rejected requests can be revised.");
        }

        if (RequesterId != requesterId)
        {
            return Error.Unauthorized(code: "NotOwner", message: "Only the requester can revise this request.");
        }

        var previousStatus = Status;
        Status = ProcurementStatus.Draft;
        Touch();
        AddAudit(requesterId, action: "Revised to Draft", previousStatus, Status);
        RaiseDomainEvent(new ProcurementRevisedToDraft(Id, requesterId, DateTimeOffset.UtcNow));
        return this;
    }

    public Comment AddComment(Guid userId, string text)
    {
        var comment = Comment.Create(requestId: Id, userId, text);
        _comments.Add(comment);
        Touch();
        return comment;
    }

    private void AddAudit(Guid userId, string action, ProcurementStatus from, ProcurementStatus to, string? comment = null) =>
        _auditEntries.Add(AuditEntry.Create(requestId: Id, userId, action, fromStatus: from, toStatus: to, comment));

    private static string GeneratePoNumber() =>
        $"PO-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

    private static class DomainErrors
    {
        public static Error InvalidTransition(ProcurementStatus from, ProcurementStatus to) =>
            Error.Domain(code: "InvalidTransition", message: $"Cannot transition from {from} to {to}.");

        public static Error CannotModify(ProcurementStatus status) =>
            Error.Domain(code: "CannotModify", message: $"Cannot modify request in {status} status.");
    }
}
