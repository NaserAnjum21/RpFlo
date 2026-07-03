namespace RpFlo.Application.DTOs;

public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    Guid? ReferenceId,
    bool IsRead,
    DateTimeOffset CreatedAt);
