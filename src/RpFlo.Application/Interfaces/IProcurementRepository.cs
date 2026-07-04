using RpFlo.Application.DTOs;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Application.Interfaces;

public interface IProcurementRepository
{
    Task<ProcurementRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetByRequesterIdAsync(Guid requesterId, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetByStatusAsync(ProcurementStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ProcurementRequest>> GetByDepartmentAsync(Department department, CancellationToken ct = default);
    Task<ProcurementRequest> AddAsync(ProcurementRequest request, CancellationToken ct = default);
    Task UpdateAsync(ProcurementRequest request, CancellationToken ct = default);
    Task DeleteLineItemAsync(Guid lineItemId, CancellationToken ct = default);
    Task<DashboardMetrics> GetMetricsAsync(Guid? userId = null, CancellationToken ct = default);
}
