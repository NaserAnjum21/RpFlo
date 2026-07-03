using RpFlo.Domain.Common;
using RpFlo.Domain.Enums;

namespace RpFlo.Domain.Events;

public sealed record ProcurementSubmitted(Guid RequestId, Guid RequesterId, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProcurementApprovedByManager(Guid RequestId, Guid ApproverId, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProcurementRejectedByManager(Guid RequestId, Guid ReviewerId, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProcurementApprovedByFinance(Guid RequestId, Guid ApproverId, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProcurementRejectedByFinance(Guid RequestId, Guid ReviewerId, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record PurchaseOrderIssued(Guid RequestId, string PoNumber, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProcurementRevisedToDraft(Guid RequestId, Guid RequesterId, DateTimeOffset OccurredAt) : IDomainEvent;
