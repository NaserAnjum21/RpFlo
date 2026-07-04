using RpFlo.Domain.Common;

namespace RpFlo.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    public Guid UserId { get; private init; }
    public string Title { get; private init; } = string.Empty;
    public string Message { get; private init; } = string.Empty;
    public Guid? ReferenceId { get; private init; }
    public bool IsRead { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid userId,
        string title,
        string message,
        Guid? referenceId = null) =>
        new()
        {
            UserId = userId,
            Title = title,
            Message = message,
            ReferenceId = referenceId
        };

    public void MarkAsRead()
    {
        IsRead = true;
        Touch();
    }
}
