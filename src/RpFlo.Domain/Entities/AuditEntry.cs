using RpFlo.Domain.Common;
using RpFlo.Domain.Enums;

namespace RpFlo.Domain.Entities;

public sealed class AuditEntry : Entity
{
    public Guid ProcurementRequestId { get; private init; }
    public Guid UserId { get; private init; }
    public string Action { get; private init; } = string.Empty;
    public ProcurementStatus FromStatus { get; private init; }
    public ProcurementStatus ToStatus { get; private init; }
    public string? Comment { get; private init; }

    private AuditEntry() { }

    public static AuditEntry Create(
        Guid requestId,
        Guid userId,
        string action,
        ProcurementStatus fromStatus,
        ProcurementStatus toStatus,
        string? comment = null) =>
        new()
        {
            ProcurementRequestId = requestId,
            UserId = userId,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Comment = comment
        };
}
