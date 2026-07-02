using RpFlo.Domain.Common;
using RpFlo.Domain.Enums;

namespace RpFlo.Domain.Events;

public sealed record ProcurementSubmitted(Guid RequestId, Guid RequesterId, DateTime OccurredAt) : IDomainEvent;
public sealed record ProcurementApprovedByManager(Guid RequestId, Guid ApproverId, DateTime OccurredAt) : IDomainEvent;
public sealed record ProcurementRejectedByManager(Guid RequestId, Guid ReviewerId, string Reason, DateTime OccurredAt) : IDomainEvent;
public sealed record ProcurementApprovedByFinance(Guid RequestId, Guid ApproverId, DateTime OccurredAt) : IDomainEvent;
public sealed record ProcurementRejectedByFinance(Guid RequestId, Guid ReviewerId, string Reason, DateTime OccurredAt) : IDomainEvent;
public sealed record PurchaseOrderIssued(Guid RequestId, string PoNumber, DateTime OccurredAt) : IDomainEvent;
public sealed record ProcurementRevisedToDraft(Guid RequestId, Guid RequesterId, DateTime OccurredAt) : IDomainEvent;
