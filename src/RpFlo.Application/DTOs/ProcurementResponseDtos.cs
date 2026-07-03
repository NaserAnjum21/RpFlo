namespace RpFlo.Application.DTOs;

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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

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
    DateTimeOffset CreatedAt);

public sealed record CommentResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    string Text,
    DateTimeOffset CreatedAt);

public sealed record ProcurementListItem(
    Guid Id,
    string Title,
    string Department,
    string Urgency,
    string Status,
    decimal TotalAmount,
    string Currency,
    string RequesterName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
