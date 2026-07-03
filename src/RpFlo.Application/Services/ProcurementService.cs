using RpFlo.Application.DTOs;
using RpFlo.Application.Interfaces;
using RpFlo.Domain.Common;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Application.Services;

public sealed class ProcurementService(
    IProcurementRepository procurementRepo,
    IUserRepository userRepo,
    INotificationRepository notificationRepo,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<ProcurementResponse>> CreateAsync(
        CreateProcurementRequest request,
        Guid requesterId,
        CancellationToken ct = default)
    {
        var user = await userRepo.GetByIdAsync(requesterId, ct);
        if (user is null)
            return Error.NotFound("User", "User not found.");

        var procurement = ProcurementRequest.Create(
            request.Title,
            request.Description,
            request.Department,
            request.Urgency,
            requesterId);

        foreach (var item in request.LineItems)
        {
            var result = procurement.AddLineItem(item.Name, item.Quantity, item.UnitPrice);
            if (result.IsFailure) return result.Error;
        }

        await procurementRepo.AddAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return await MapToResponseAsync(procurement, user, ct);
    }

    public async Task<Result<ProcurementResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<IReadOnlyList<ProcurementListItem>> GetAllAsync(CancellationToken ct = default)
    {
        var procurements = await procurementRepo.GetAllAsync(ct);
        var users = await userRepo.GetAllAsync(ct);
        var userMap = users.ToDictionary(u => u.Id);

        return procurements
            .Select(p => MapToListItem(p, userMap.GetValueOrDefault(p.RequesterId)))
            .ToList();
    }

    public async Task<IReadOnlyList<ProcurementListItem>> GetByRequesterAsync(
        Guid requesterId, CancellationToken ct = default)
    {
        var procurements = await procurementRepo.GetByRequesterIdAsync(requesterId, ct);
        var user = await userRepo.GetByIdAsync(requesterId, ct);

        return procurements
            .Select(p => MapToListItem(p, user))
            .ToList();
    }

    public async Task<IReadOnlyList<ProcurementListItem>> GetPendingForRoleAsync(
        UserRole role, CancellationToken ct = default)
    {
        var status = role switch
        {
            UserRole.Manager => ProcurementStatus.Submitted,
            UserRole.Finance => ProcurementStatus.ManagerApproved,
            _ => (ProcurementStatus?)null
        };

        if (status is null) return [];

        var procurements = await procurementRepo.GetByStatusAsync(status.Value, ct);
        var users = await userRepo.GetAllAsync(ct);
        var userMap = users.ToDictionary(u => u.Id);

        return procurements
            .Select(p => MapToListItem(p, userMap.GetValueOrDefault(p.RequesterId)))
            .ToList();
    }

    public async Task<Result<ProcurementResponse>> UpdateAsync(
        Guid id,
        UpdateProcurementRequest request,
        Guid requesterId,
        CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        if (procurement.RequesterId != requesterId)
            return Error.Unauthorized("NotOwner", "Only the requester can update this request.");

        var result = procurement.Update(request.Title, request.Description, request.Department, request.Urgency);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> AddLineItemsAsync(
        Guid id,
        AddLineItemsRequest request,
        Guid requesterId,
        CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        if (procurement.RequesterId != requesterId)
            return Error.Unauthorized("NotOwner", "Only the requester can modify line items.");

        foreach (var item in request.LineItems)
        {
            var result = procurement.AddLineItem(item.Name, item.Quantity, item.UnitPrice);
            if (result.IsFailure) return result.Error;
        }

        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> RemoveLineItemAsync(
        Guid id,
        Guid lineItemId,
        Guid requesterId,
        CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        if (procurement.RequesterId != requesterId)
            return Error.Unauthorized("NotOwner", "Only the requester can modify line items.");

        var result = procurement.RemoveLineItem(lineItemId);
        if (result.IsFailure) return result.Error;

        await procurementRepo.DeleteLineItemAsync(lineItemId, ct);
        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> SubmitAsync(
        Guid id, Guid requesterId, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var result = procurement.Submit(requesterId);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUsersWithRole(UserRole.Manager, "New Procurement Request",
            $"'{procurement.Title}' submitted for review.", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> ApproveByManagerAsync(
        Guid id, Guid approverId, ApprovalRequest request, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var approver = await userRepo.GetByIdAsync(approverId, ct);
        if (approver?.Role is not UserRole.Manager and not UserRole.Admin)
            return Error.Unauthorized("NotManager", "Only managers can approve at this stage.");

        var result = procurement.ApproveByManager(approverId, request.Comment);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUser(procurement.RequesterId, "Request Approved by Manager",
            $"'{procurement.Title}' approved by manager. Pending finance review.", procurement.Id, ct);
        await NotifyUsersWithRole(UserRole.Finance, "Procurement Pending Finance Review",
            $"'{procurement.Title}' awaiting finance approval.", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> RejectByManagerAsync(
        Guid id, Guid reviewerId, RejectionRequest request, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var reviewer = await userRepo.GetByIdAsync(reviewerId, ct);
        if (reviewer?.Role is not UserRole.Manager and not UserRole.Admin)
            return Error.Unauthorized("NotManager", "Only managers can reject at this stage.");

        var result = procurement.RejectByManager(reviewerId, request.Reason);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUser(procurement.RequesterId, "Request Rejected by Manager",
            $"'{procurement.Title}' rejected: {request.Reason}", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> ApproveByFinanceAsync(
        Guid id, Guid approverId, ApprovalRequest request, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var approver = await userRepo.GetByIdAsync(approverId, ct);
        if (approver?.Role is not UserRole.Finance and not UserRole.Admin)
            return Error.Unauthorized("NotFinance", "Only finance can approve at this stage.");

        var result = procurement.ApproveByFinance(approverId, request.Comment);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUser(procurement.RequesterId, "Request Approved by Finance",
            $"'{procurement.Title}' fully approved. Ready for PO.", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> RejectByFinanceAsync(
        Guid id, Guid reviewerId, RejectionRequest request, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var reviewer = await userRepo.GetByIdAsync(reviewerId, ct);
        if (reviewer?.Role is not UserRole.Finance and not UserRole.Admin)
            return Error.Unauthorized("NotFinance", "Only finance can reject at this stage.");

        var result = procurement.RejectByFinance(reviewerId, request.Reason);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUser(procurement.RequesterId, "Request Rejected by Finance",
            $"'{procurement.Title}' rejected by finance: {request.Reason}", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> IssuePurchaseOrderAsync(
        Guid id, Guid issuerId, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var issuer = await userRepo.GetByIdAsync(issuerId, ct);
        if (issuer?.Role is not UserRole.Finance and not UserRole.Admin)
            return Error.Unauthorized("NotFinance", "Only finance can issue purchase orders.");

        var result = procurement.IssuePurchaseOrder(issuerId);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await NotifyUser(procurement.RequesterId, "Purchase Order Issued",
            $"PO #{procurement.PoNumber} issued for '{procurement.Title}'.", procurement.Id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<ProcurementResponse>> ReviseToDraftAsync(
        Guid id, Guid requesterId, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var result = procurement.ReviseToDraft(requesterId);
        if (result.IsFailure) return result.Error;

        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<CommentResponse>> AddCommentAsync(
        Guid id, Guid userId, AddCommentRequest request, CancellationToken ct = default)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return Error.NotFound("User", "User not found.");

        var comment = procurement.AddComment(userId, request.Text);
        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CommentResponse(comment.Id, userId, user.Name, comment.Text, comment.CreatedAt);
    }

    public async Task<DashboardMetrics> GetMetricsAsync(CancellationToken ct = default)
    {
        var all = await procurementRepo.GetAllAsync(ct);

        var statusGroups = all
            .GroupBy(p => p.Status)
            .Select(g => new StatusCount(g.Key.ToString(), g.Count()))
            .ToList();

        var departmentGroups = all
            .GroupBy(p => p.Department)
            .Select(g => new DepartmentCount(
                g.Key.ToString(),
                g.Count(),
                g.Sum(p => p.TotalAmount.Amount)))
            .ToList();

        var completedRequests = all
            .Where(p => p.Status == ProcurementStatus.PurchaseOrderIssued)
            .ToList();

        var avgProcessingHours = completedRequests.Count > 0
            ? completedRequests.Average(p => (p.UpdatedAt - p.CreatedAt).TotalHours)
            : 0;

        return new DashboardMetrics(
            TotalRequests: all.Count,
            DraftCount: all.Count(p => p.Status == ProcurementStatus.Draft),
            PendingApprovalCount: all.Count(p => p.Status is ProcurementStatus.Submitted or ProcurementStatus.ManagerApproved),
            ApprovedCount: all.Count(p => p.Status == ProcurementStatus.FinanceApproved),
            RejectedCount: all.Count(p => p.Status is ProcurementStatus.ManagerRejected or ProcurementStatus.FinanceRejected),
            PurchaseOrderCount: all.Count(p => p.Status == ProcurementStatus.PurchaseOrderIssued),
            TotalApprovedAmount: all
                .Where(p => p.Status is ProcurementStatus.FinanceApproved or ProcurementStatus.PurchaseOrderIssued)
                .Sum(p => p.TotalAmount.Amount),
            AverageProcessingTimeHours: (decimal)Math.Round(avgProcessingHours, 1),
            StatusBreakdown: statusGroups,
            DepartmentBreakdown: departmentGroups);
    }

    private async Task<ProcurementResponse> MapToResponseAsync(ProcurementRequest p, User requester, CancellationToken ct = default)
    {
        var userIds = p.AuditEntries
            .Select(a => a.UserId)
            .Concat(p.Comments.Select(c => c.UserId))
            .Distinct()
            .ToList();

        var userLookup = new Dictionary<Guid, string> { [requester.Id] = requester.Name };
        foreach (var uid in userIds.Where(id => !userLookup.ContainsKey(id)))
        {
            var user = await userRepo.GetByIdAsync(uid, ct);
            userLookup[uid] = user?.Name ?? "Unknown";
        }

        return new ProcurementResponse(
            p.Id,
            p.Title,
            p.Description,
            p.Department.ToString(),
            p.Urgency.ToString(),
            p.Status.ToString(),
            p.TotalAmount.Amount,
            p.TotalAmount.Currency,
            p.PoNumber,
            new RequesterInfo(requester.Id, requester.Name, requester.Email, requester.Department.ToString()),
            p.LineItems.Select(li => new LineItemResponse(
                li.Id, li.Name, li.Quantity, li.UnitPrice.Amount, li.TotalPrice.Amount)).ToList(),
            p.AuditEntries.Select(a => new AuditEntryResponse(
                a.Id, a.UserId, userLookup.GetValueOrDefault(a.UserId, "Unknown"), a.Action, a.FromStatus.ToString(), a.ToStatus.ToString(),
                a.Comment, a.CreatedAt)).ToList(),
            p.Comments.Select(c => new CommentResponse(
                c.Id, c.UserId, userLookup.GetValueOrDefault(c.UserId, "Unknown"), c.Text, c.CreatedAt)).ToList(),
            p.CreatedAt,
            p.UpdatedAt);
    }

    private static ProcurementListItem MapToListItem(ProcurementRequest p, User? requester) =>
        new(p.Id, p.Title, p.Department.ToString(), p.Urgency.ToString(), p.Status.ToString(),
            p.TotalAmount.Amount, p.TotalAmount.Currency,
            requester?.Name ?? "Unknown", p.CreatedAt, p.UpdatedAt);

    private async Task NotifyUsersWithRole(
        UserRole role, string title, string message, Guid referenceId, CancellationToken ct)
    {
        var users = await userRepo.GetByRoleAsync(role, ct);
        foreach (var user in users)
            await notificationRepo.AddAsync(
                Notification.Create(user.Id, title, message, referenceId), ct);
    }

    private async Task NotifyUser(
        Guid userId, string title, string message, Guid referenceId, CancellationToken ct) =>
        await notificationRepo.AddAsync(
            Notification.Create(userId, title, message, referenceId), ct);
}
