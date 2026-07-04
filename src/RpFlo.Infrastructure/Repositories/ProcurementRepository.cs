using Microsoft.EntityFrameworkCore;
using RpFlo.Application.DTOs;
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

        foreach (var lineItem in request.LineItems)
        {
            var entry = db.Entry(lineItem);
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

    public async Task<DashboardMetrics> GetMetricsAsync(CancellationToken ct = default)
    {
        var statusCounts = await db.ProcurementRequests
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var counts = statusCounts.ToDictionary(s => s.Status, s => s.Count);
        int total = counts.Values.Sum();

        int Count(params ProcurementStatus[] statuses) =>
            statuses.Sum(s => counts.GetValueOrDefault(s));

        var approvedStatuses = new[] { ProcurementStatus.FinanceApproved, ProcurementStatus.PurchaseOrderIssued };
        var totalApproved = await db.ProcurementRequests
            .Where(p => approvedStatuses.Contains(p.Status))
            .SelectMany(p => p.LineItems)
            .SumAsync(li => li.UnitPrice.Amount * li.Quantity, ct);

        var departmentGroups = await db.ProcurementRequests
            .GroupBy(p => p.Department)
            .Select(g => new DepartmentCount(
                g.Key.ToString(),
                g.Count(),
                g.SelectMany(p => p.LineItems).Sum(li => li.UnitPrice.Amount * li.Quantity)))
            .ToListAsync(ct);

        var avgHours = await db.ProcurementRequests
            .Where(p => p.Status == ProcurementStatus.PurchaseOrderIssued)
            .Select(p => (double?)EF.Functions.DateDiffSecond(p.CreatedAt, p.UpdatedAt))
            .AverageAsync(ct) ?? 0;

        return new DashboardMetrics(
            TotalRequests: total,
            DraftCount: Count(ProcurementStatus.Draft),
            PendingApprovalCount: Count(ProcurementStatus.Submitted, ProcurementStatus.ManagerApproved),
            ApprovedCount: Count(ProcurementStatus.FinanceApproved),
            RejectedCount: Count(ProcurementStatus.ManagerRejected, ProcurementStatus.FinanceRejected),
            PurchaseOrderCount: Count(ProcurementStatus.PurchaseOrderIssued),
            TotalApprovedAmount: totalApproved,
            AverageProcessingTimeHours: Math.Round((decimal)(avgHours / 3600.0), 1),
            StatusBreakdown: statusCounts.Select(s => new StatusCount(s.Status.ToString(), s.Count)).ToList(),
            DepartmentBreakdown: departmentGroups);
    }
}
