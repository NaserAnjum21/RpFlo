using RpFlo.Domain.Common;

namespace RpFlo.Domain.Entities;

public sealed class Comment : AuditableEntity
{
    public Guid ProcurementRequestId { get; private init; }
    public Guid UserId { get; private init; }
    public string Text { get; private init; } = string.Empty;

    private Comment() { }

    public static Comment Create(Guid requestId, Guid userId, string text) =>
        new()
        {
            ProcurementRequestId = requestId,
            UserId = userId,
            Text = text
        };
}
