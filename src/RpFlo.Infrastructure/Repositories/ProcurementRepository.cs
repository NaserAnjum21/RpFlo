using Microsoft.EntityFrameworkCore;
using RpFlo.Application.Interfaces;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;
using RpFlo.Infrastructure.Persistence;

namespace RpFlo.Infrastructure.Repositories;

public sealed class ProcurementRepository(AppDbContext db) : IProcurementRepository
{
    public async Task<ProcurementRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.ProcurementRequests
            .Include(p => p.LineItems)
            .Include(p => p.AuditEntries)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<ProcurementRequest>> GetAllAsync(CancellationToken ct = default) =>
        await db.ProcurementRequests
            .Include(p => p.LineItems)
            .Include(p => p.AuditEntries)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProcurementRequest>> GetByRequesterIdAsync(
        Guid requesterId, CancellationToken ct = default) =>
        await db.ProcurementRequests
            .Include(p => p.LineItems)
            .Include(p => p.AuditEntries)
            .Where(p => p.RequesterId == requesterId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProcurementRequest>> GetByStatusAsync(
        ProcurementStatus status, CancellationToken ct = default) =>
        await db.ProcurementRequests
            .Include(p => p.LineItems)
            .Include(p => p.AuditEntries)
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProcurementRequest>> GetByDepartmentAsync(
        Department department, CancellationToken ct = default) =>
        await db.ProcurementRequests
            .Include(p => p.LineItems)
            .Where(p => p.Department == department)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<ProcurementRequest> AddAsync(ProcurementRequest request, CancellationToken ct = default)
    {
        var entry = await db.ProcurementRequests.AddAsync(request, ct);
        return entry.Entity;
    }

    public Task UpdateAsync(ProcurementRequest request, CancellationToken ct = default)
    {
        foreach (var audit in request.AuditEntries)
        {
            var entry = db.Entry(audit);
            if (entry.State is not EntityState.Unchanged)
                entry.State = EntityState.Added;
        }

        foreach (var comment in request.Comments)
        {
            var entry = db.Entry(comment);
            if (entry.State is not EntityState.Unchanged)
                entry.State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public async Task DeleteLineItemAsync(Guid lineItemId, CancellationToken ct = default)
    {
        var item = await db.LineItems.FindAsync([lineItemId], ct);
        if (item is not null)
            db.LineItems.Remove(item);
    }
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Users.FindAsync([id], ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await db.Users.OrderBy(u => u.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default) =>
        await db.Users.Where(u => u.Role == role).ToListAsync(ct);
}

public sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await db.Notifications.AddAsync(notification, ct);

    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await db.Notifications.FindAsync([notificationId], ct);
        notification?.MarkAsRead();
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
}

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
