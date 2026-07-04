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

    public async Task<IReadOnlyList<ProcurementRequest>> GetVisibleForUserAsync(
        Guid userId, UserRole role, CancellationToken ct = default) =>
        await ApplyVisibilityFilter(db.ProcurementRequests, userId, role)
            .Include(p => p.LineItems)
            .Include(p => p.AuditEntries)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<PagedResult<ProcurementRequest>> GetPagedAsync(
        ProcurementListPageQuery query, CancellationToken ct = default) =>
        await ToPagedResultAsync(
            ApplyListFilters(db.ProcurementRequests.AsNoTracking(), query),
            query.Page,
            query.PageSize,
            q => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            ct);

    public async Task<PagedResult<ProcurementRequest>> GetPagedVisibleForUserAsync(
        Guid userId, UserRole role, ProcurementListPageQuery query, CancellationToken ct = default) =>
        await ToPagedResultAsync(
            ApplyListFilters(ApplyVisibilityFilter(db.ProcurementRequests.AsNoTracking(), userId, role), query),
            query.Page,
            query.PageSize,
            q => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            ct);

    public async Task<PagedResult<ProcurementRequest>> GetPagedByRequesterIdAsync(
        Guid requesterId, ProcurementListPageQuery query, CancellationToken ct = default) =>
        await ToPagedResultAsync(
            ApplyListFilters(db.ProcurementRequests.AsNoTracking().Where(p => p.RequesterId == requesterId), query),
            query.Page,
            query.PageSize,
            q => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            ct);

    public async Task<PagedResult<ProcurementRequest>> GetPagedPendingForUserAsync(
        Guid userId, UserRole role, ProcurementTaskPageQuery query, CancellationToken ct = default)
    {
        var procurements = db.ProcurementRequests.AsNoTracking();

        var filtered = role switch
        {
            UserRole.Requester => procurements.Where(p =>
                p.RequesterId == userId &&
                (p.Status == ProcurementStatus.Draft ||
                 p.Status == ProcurementStatus.ManagerRejected ||
                 p.Status == ProcurementStatus.FinanceRejected)),
            UserRole.Manager => procurements.Where(p => p.Status == ProcurementStatus.Submitted),
            UserRole.Finance => procurements.Where(p =>
                p.Status == ProcurementStatus.ManagerApproved ||
                p.Status == ProcurementStatus.FinanceApproved),
            UserRole.Admin => procurements.Where(p =>
                p.Status == ProcurementStatus.Submitted ||
                p.Status == ProcurementStatus.ManagerApproved ||
                p.Status == ProcurementStatus.FinanceApproved),
            _ => procurements.Where(_ => false)
        };

        return await ToPagedResultAsync(
            ApplyDateFilters(filtered, query.DateFrom, query.DateTo),
            query.Page,
            query.PageSize,
            q => q.OrderBy(p =>
                    p.Urgency == Urgency.Critical ? 0 :
                    p.Urgency == Urgency.High ? 1 :
                    p.Urgency == Urgency.Medium ? 2 : 3)
                .ThenBy(p => p.CreatedAt)
                .ThenBy(p => p.Id),
            ct);
    }

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

    public async Task<DashboardMetrics> GetMetricsAsync(Guid userId, UserRole role, CancellationToken ct = default)
    {
        var visibleRequests = ApplyVisibilityFilter(db.ProcurementRequests.AsNoTracking(), userId, role);

        var statusCounts = await visibleRequests
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var counts = statusCounts.ToDictionary(s => s.Status, s => s.Count);
        int total = counts.Values.Sum();

        int Count(params ProcurementStatus[] statuses) =>
            statuses.Sum(s => counts.GetValueOrDefault(s));

        var approvedStatuses = new[] { ProcurementStatus.FinanceApproved, ProcurementStatus.PurchaseOrderIssued };
        var totalApproved = await visibleRequests
            .Where(p => approvedStatuses.Contains(p.Status))
            .SelectMany(p => p.LineItems)
            .SumAsync(li => li.UnitPrice.Amount * li.Quantity, ct);

        var departmentGroups = await visibleRequests
            .GroupBy(p => p.Department)
            .Select(g => new DepartmentCount(
                g.Key.ToString(),
                g.Count(),
                g.SelectMany(p => p.LineItems).Sum(li => li.UnitPrice.Amount * li.Quantity)))
            .ToListAsync(ct);

        var avgHours = await visibleRequests
            .Where(p => p.Status == ProcurementStatus.PurchaseOrderIssued)
            .Select(p => (double?)EF.Functions.DateDiffSecond(p.CreatedAt, p.UpdatedAt))
            .AverageAsync(ct) ?? 0;

        var roleMetrics = await ComputeRoleMetrics(userId, role, ct);

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
            DepartmentBreakdown: departmentGroups,
            RoleMetrics: roleMetrics);
    }

    private static IQueryable<ProcurementRequest> ApplyVisibilityFilter(
        IQueryable<ProcurementRequest> query,
        Guid userId,
        UserRole role) =>
        role switch
        {
            UserRole.Requester => query.Where(p => p.RequesterId == userId),
            UserRole.Manager => query.Where(p => p.RequesterId == userId || p.Status != ProcurementStatus.Draft),
            UserRole.Finance => query.Where(p =>
                p.RequesterId == userId ||
                p.Status == ProcurementStatus.ManagerApproved ||
                p.Status == ProcurementStatus.FinanceApproved ||
                p.Status == ProcurementStatus.FinanceRejected ||
                p.Status == ProcurementStatus.PurchaseOrderIssued),
            UserRole.Admin => query,
            _ => query.Where(_ => false)
        };

    private async Task<RoleMetrics> ComputeRoleMetrics(Guid userId, UserRole role, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return new RoleMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, [], []);

        role = user.Role;
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var myActive = await db.ProcurementRequests
            .CountAsync(p => p.RequesterId == userId &&
                p.Status != ProcurementStatus.PurchaseOrderIssued &&
                p.Status != ProcurementStatus.ManagerRejected &&
                p.Status != ProcurementStatus.FinanceRejected, ct);

        var myPending = await db.ProcurementRequests
            .CountAsync(p => p.RequesterId == userId &&
                (p.Status == ProcurementStatus.Submitted || p.Status == ProcurementStatus.ManagerApproved), ct);

        var myApproved = await db.ProcurementRequests
            .Where(p => p.RequesterId == userId &&
                (p.Status == ProcurementStatus.FinanceApproved || p.Status == ProcurementStatus.PurchaseOrderIssued))
            .SelectMany(p => p.LineItems)
            .SumAsync(li => (decimal?)li.UnitPrice.Amount * li.Quantity, ct) ?? 0;

        var myAvgHours = await db.ProcurementRequests
            .Where(p => p.RequesterId == userId && p.Status == ProcurementStatus.PurchaseOrderIssued)
            .Select(p => (double?)EF.Functions.DateDiffSecond(p.CreatedAt, p.UpdatedAt))
            .AverageAsync(ct) ?? 0;

        var pendingManagerReview = role is UserRole.Manager or UserRole.Admin
            ? await db.ProcurementRequests.CountAsync(p => p.Status == ProcurementStatus.Submitted, ct)
            : 0;

        var pendingFinanceReview = role is UserRole.Finance or UserRole.Admin
            ? await db.ProcurementRequests.CountAsync(p => p.Status == ProcurementStatus.ManagerApproved, ct)
            : 0;

        var approvedThisMonth = role is UserRole.Manager or UserRole.Finance or UserRole.Admin
            ? await db.ProcurementRequests.CountAsync(p => p.Status == ProcurementStatus.PurchaseOrderIssued && p.UpdatedAt >= monthStart, ct)
            : 0;

        var readyForPo = role is UserRole.Finance or UserRole.Admin
            ? await db.ProcurementRequests.CountAsync(p => p.Status == ProcurementStatus.FinanceApproved, ct)
            : 0;

        var pendingFinanceStatuses = new[] { ProcurementStatus.ManagerApproved, ProcurementStatus.FinanceApproved };
        var totalValuePending = role is UserRole.Finance or UserRole.Admin
            ? await db.ProcurementRequests
                .Where(p => pendingFinanceStatuses.Contains(p.Status))
                .SelectMany(p => p.LineItems)
                .SumAsync(li => (decimal?)li.UnitPrice.Amount * li.Quantity, ct) ?? 0
            : 0;

        var monthlySpend = role is UserRole.Finance or UserRole.Admin
            ? await db.ProcurementRequests
                .Where(p => p.Status == ProcurementStatus.PurchaseOrderIssued && p.UpdatedAt >= monthStart)
                .SelectMany(p => p.LineItems)
                .SumAsync(li => (decimal?)li.UnitPrice.Amount * li.Quantity, ct) ?? 0
            : 0;

        var myStatusCounts = await db.ProcurementRequests
            .Where(p => p.RequesterId == userId)
            .GroupBy(p => p.Status)
            .Select(g => new StatusCount(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        var myDeptCounts = await db.ProcurementRequests
            .Where(p => p.RequesterId == userId)
            .GroupBy(p => p.Department)
            .Select(g => new DepartmentCount(
                g.Key.ToString(),
                g.Count(),
                g.SelectMany(p => p.LineItems).Sum(li => li.UnitPrice.Amount * li.Quantity)))
            .ToListAsync(ct);

        return new RoleMetrics(
            MyActiveRequests: myActive,
            MyPendingApproval: myPending,
            MyApprovedAmount: myApproved,
            MyAvgProcessingHours: Math.Round((decimal)(myAvgHours / 3600.0), 1),
            PendingMyReview: pendingManagerReview + pendingFinanceReview,
            ApprovedThisMonth: approvedThisMonth,
            ReadyForPo: readyForPo,
            TotalValuePending: totalValuePending,
            MonthlySpendApproved: monthlySpend,
            MyStatusBreakdown: myStatusCounts,
            MyDepartmentBreakdown: myDeptCounts);
    }

    private static IQueryable<ProcurementRequest> ApplyListFilters(
        IQueryable<ProcurementRequest> query,
        ProcurementListPageQuery pageQuery)
    {
        ProcurementStatus[] statuses = pageQuery.Filter switch
        {
            ProcurementListFilter.Draft => [ProcurementStatus.Draft],
            ProcurementListFilter.Pending => [ProcurementStatus.Submitted, ProcurementStatus.ManagerApproved],
            ProcurementListFilter.Completed => [ProcurementStatus.PurchaseOrderIssued],
            ProcurementListFilter.Rejected => [ProcurementStatus.ManagerRejected, ProcurementStatus.FinanceRejected],
            _ => Array.Empty<ProcurementStatus>()
        };

        if (statuses.Length > 0)
            query = query.Where(p => statuses.Contains(p.Status));

        return ApplyDateFilters(query, pageQuery.DateFrom, pageQuery.DateTo);
    }

    private static IQueryable<ProcurementRequest> ApplyDateFilters(
        IQueryable<ProcurementRequest> query,
        DateOnly? dateFrom,
        DateOnly? dateTo)
    {
        if (dateFrom.HasValue)
        {
            var from = ToUtcStart(dateFrom.Value);
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (dateTo.HasValue)
        {
            var toExclusive = ToUtcStart(dateTo.Value.AddDays(1));
            query = query.Where(p => p.CreatedAt < toExclusive);
        }

        return query;
    }

    private static DateTimeOffset ToUtcStart(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), TimeSpan.Zero);

    private static async Task<PagedResult<ProcurementRequest>> ToPagedResultAsync(
        IQueryable<ProcurementRequest> query,
        int requestedPage,
        int requestedPageSize,
        Func<IQueryable<ProcurementRequest>, IOrderedQueryable<ProcurementRequest>> orderBy,
        CancellationToken ct)
    {
        var pageSize = Math.Clamp(requestedPageSize, 1, 100);
        var totalItems = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var page = Math.Clamp(requestedPage, 1, totalPages);

        var items = await orderBy(query)
            .Include(p => p.LineItems)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ProcurementRequest>(items, page, pageSize, totalItems, totalPages);
    }
}
