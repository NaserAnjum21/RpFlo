using RpFlo.Application.DTOs;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Application.Interfaces;

public interface IProcurementRepository
{
    Task<ProcurementRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetVisibleForUserAsync(Guid userId, UserRole role, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementListItem>> GetVisibleListItemsForUserAsync(Guid userId, UserRole role, CancellationToken ct = default);
    Task<PagedResult<ProcurementListItem>> GetPagedAsync(ProcurementListPageQuery query, CancellationToken ct = default);
    Task<PagedResult<ProcurementListItem>> GetPagedVisibleForUserAsync(Guid userId, UserRole role, ProcurementListPageQuery query, CancellationToken ct = default);
    Task<PagedResult<ProcurementListItem>> GetPagedByRequesterIdAsync(Guid requesterId, ProcurementListPageQuery query, CancellationToken ct = default);
    Task<PagedResult<ProcurementListItem>> GetPagedPendingForUserAsync(Guid userId, UserRole role, ProcurementTaskPageQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetByDepartmentAsync(Department department, CancellationToken ct = default);
    Task<ProcurementRequest> AddAsync(ProcurementRequest request, CancellationToken ct = default);
    Task UpdateAsync(ProcurementRequest request, CancellationToken ct = default);
    Task DeleteLineItemAsync(Guid lineItemId, CancellationToken ct = default);
    Task<DashboardMetrics> GetMetricsAsync(Guid userId, UserRole role, CancellationToken ct = default);
}
