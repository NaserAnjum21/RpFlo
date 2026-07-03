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
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
