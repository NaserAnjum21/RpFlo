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

public sealed record ProcurementResponse(
    Guid Id,
    string Title,
    string Description,
    string Department,
    string Urgency,
    string Status,
    decimal TotalAmount,
    string Currency,
    string? PoNumber,
    RequesterInfo Requester,
    List<LineItemResponse> LineItems,
    List<AuditEntryResponse> AuditTrail,
    List<CommentResponse> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record RequesterInfo(Guid Id, string Name, string Email, string Department);

public sealed record LineItemResponse(
    Guid Id,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record AuditEntryResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string Action,
    string FromStatus,
    string ToStatus,
    string? Comment,
    DateTime CreatedAt);

public sealed record CommentResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string Text,
    DateTime CreatedAt);

public sealed record ProcurementListItem(
    Guid Id,
    string Title,
    string Department,
    string Urgency,
    string Status,
    decimal TotalAmount,
    string Currency,
    string RequesterName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DashboardMetrics(
    int TotalRequests,
    int DraftCount,
    int PendingApprovalCount,
    int ApprovedCount,
    int RejectedCount,
    int PurchaseOrderCount,
    decimal TotalApprovedAmount,
    decimal AverageProcessingTimeHours,
    List<StatusCount> StatusBreakdown,
    List<DepartmentCount> DepartmentBreakdown);

public sealed record StatusCount(string Status, int Count);
public sealed record DepartmentCount(string Department, int Count, decimal TotalAmount);

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Department);

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    Guid? ReferenceId,
    bool IsRead,
    DateTime CreatedAt);
