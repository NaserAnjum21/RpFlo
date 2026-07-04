using RpFlo.Domain.Enums;

namespace RpFlo.Application.DTOs;

public sealed record CreateProcurementRequest(
    string Title,
    string Description,
    Department Department,
    Urgency Urgency,
    List<CreateLineItemRequest> LineItems);

public sealed record CreateLineItemRequest(
    string Name,
    int Quantity,
    decimal UnitPrice);

public sealed record UpdateProcurementRequest(
    string Title,
    string Description,
    Department Department,
    Urgency Urgency);

public sealed record ApprovalRequest(string? Comment);
public sealed record RejectionRequest(string Reason);
public sealed record AddCommentRequest(string Text);
public sealed record AddLineItemsRequest(List<CreateLineItemRequest> LineItems);

public enum ProcurementListFilter
{
    All,
    Draft,
    Pending,
    Completed,
    Rejected
}

public sealed class ProcurementListPageQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ProcurementListFilter Filter { get; set; } = ProcurementListFilter.All;
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}

public sealed class ProcurementTaskPageQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
