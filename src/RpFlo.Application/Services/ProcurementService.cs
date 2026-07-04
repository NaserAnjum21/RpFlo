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

    public async Task<Result<ProcurementResponse>> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var userResult = await GetUserOrError(userId, ct);
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        if (!CanView(user, procurement))
            return Error.Unauthorized("AccessDenied", "You do not have access to this procurement request.");

        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementListItem>>> GetAllVisibleForUserAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var userResult = await GetUserOrError(userId, ct);
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        var procurements = await procurementRepo.GetVisibleForUserAsync(user.Id, user.Role, ct);
        var requesterIds = procurements.Select(p => p.RequesterId).Distinct().ToList();
        var requesters = requesterIds.Count == 0
            ? []
            : await userRepo.GetByIdsAsync(requesterIds, ct);
        var userMap = requesters.ToDictionary(u => u.Id);

        return procurements
            .Select(p => MapToListItem(p, userMap.GetValueOrDefault(p.RequesterId)))
            .ToList();
    }

    public async Task<Result<PagedResult<ProcurementListItem>>> GetPagedVisibleForUserAsync(
        Guid userId,
        ProcurementListPageQuery query, CancellationToken ct = default)
    {
        var userResult = await GetUserOrError(userId, ct);
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        var page = await procurementRepo.GetPagedVisibleForUserAsync(user.Id, user.Role, query, ct);
        return await MapPagedListItemsAsync(page, ct);
    }

    public async Task<Result<PagedResult<ProcurementListItem>>> GetPagedByRequesterAsync(
        Guid requesterId, ProcurementListPageQuery query, CancellationToken ct = default)
    {
        var userResult = await GetUserOrError(requesterId, ct);
        if (userResult.IsFailure) return userResult.Error;

        var page = await procurementRepo.GetPagedByRequesterIdAsync(requesterId, query, ct);
        return await MapPagedListItemsAsync(page, ct);
    }

    public async Task<Result<PagedResult<ProcurementListItem>>> GetPagedPendingForUserAsync(
        Guid userId, ProcurementTaskPageQuery query, CancellationToken ct = default)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return Error.NotFound("User", "User not found.");

        var page = await procurementRepo.GetPagedPendingForUserAsync(userId, user.Role, query, ct);
        return await MapPagedListItemsAsync(page, ct);
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
        var procResult = await GetProcurementOrError(id, ct);
        if (procResult.IsFailure) return procResult.Error;
        var procurement = procResult.Value;

        var roleResult = await RequireRole(approverId, UserRole.Manager, "NotManager", "Only managers can approve at this stage.", ct);
        if (roleResult.IsFailure) return roleResult.Error;

        var result = procurement.ApproveByManager(approverId, request.Comment);
        if (result.IsFailure) return result.Error;

        await NotifyUser(procurement.RequesterId, "Request Approved by Manager",
            $"'{procurement.Title}' approved by manager. Pending finance review.", procurement.Id, ct);
        await NotifyUsersWithRole(UserRole.Finance, "Procurement Pending Finance Review",
            $"'{procurement.Title}' awaiting finance approval.", procurement.Id, ct);
        return await SaveAndRespond(procurement, ct);
    }

    public async Task<Result<ProcurementResponse>> RejectByManagerAsync(
        Guid id, Guid reviewerId, RejectionRequest request, CancellationToken ct = default)
    {
        var procResult = await GetProcurementOrError(id, ct);
        if (procResult.IsFailure) return procResult.Error;
        var procurement = procResult.Value;

        var roleResult = await RequireRole(reviewerId, UserRole.Manager, "NotManager", "Only managers can reject at this stage.", ct);
        if (roleResult.IsFailure) return roleResult.Error;

        var result = procurement.RejectByManager(reviewerId, request.Reason);
        if (result.IsFailure) return result.Error;

        await NotifyUser(procurement.RequesterId, "Request Rejected by Manager",
            $"'{procurement.Title}' rejected: {request.Reason}", procurement.Id, ct);
        return await SaveAndRespond(procurement, ct);
    }

    public async Task<Result<ProcurementResponse>> ApproveByFinanceAsync(
        Guid id, Guid approverId, ApprovalRequest request, CancellationToken ct = default)
    {
        var procResult = await GetProcurementOrError(id, ct);
        if (procResult.IsFailure) return procResult.Error;
        var procurement = procResult.Value;

        var roleResult = await RequireRole(approverId, UserRole.Finance, "NotFinance", "Only finance can approve at this stage.", ct);
        if (roleResult.IsFailure) return roleResult.Error;

        var result = procurement.ApproveByFinance(approverId, request.Comment);
        if (result.IsFailure) return result.Error;

        await NotifyUser(procurement.RequesterId, "Request Approved by Finance",
            $"'{procurement.Title}' fully approved. Ready for PO.", procurement.Id, ct);
        return await SaveAndRespond(procurement, ct);
    }

    public async Task<Result<ProcurementResponse>> RejectByFinanceAsync(
        Guid id, Guid reviewerId, RejectionRequest request, CancellationToken ct = default)
    {
        var procResult = await GetProcurementOrError(id, ct);
        if (procResult.IsFailure) return procResult.Error;
        var procurement = procResult.Value;

        var roleResult = await RequireRole(reviewerId, UserRole.Finance, "NotFinance", "Only finance can reject at this stage.", ct);
        if (roleResult.IsFailure) return roleResult.Error;

        var result = procurement.RejectByFinance(reviewerId, request.Reason);
        if (result.IsFailure) return result.Error;

        await NotifyUser(procurement.RequesterId, "Request Rejected by Finance",
            $"'{procurement.Title}' rejected by finance: {request.Reason}", procurement.Id, ct);
        return await SaveAndRespond(procurement, ct);
    }

    public async Task<Result<ProcurementResponse>> IssuePurchaseOrderAsync(
        Guid id, Guid issuerId, CancellationToken ct = default)
    {
        var procResult = await GetProcurementOrError(id, ct);
        if (procResult.IsFailure) return procResult.Error;
        var procurement = procResult.Value;

        var roleResult = await RequireRole(issuerId, UserRole.Finance, "NotFinance", "Only finance can issue purchase orders.", ct);
        if (roleResult.IsFailure) return roleResult.Error;

        var result = procurement.IssuePurchaseOrder(issuerId);
        if (result.IsFailure) return result.Error;

        await NotifyUser(procurement.RequesterId, "Purchase Order Issued",
            $"PO #{procurement.PoNumber} issued for '{procurement.Title}'.", procurement.Id, ct);
        return await SaveAndRespond(procurement, ct);
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
        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null)
            return Error.NotFound("User", "User not found.");

        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        if (procurement is null)
            return Error.NotFound("Procurement", $"Procurement request {id} not found.");

        if (!CanView(user, procurement))
            return Error.Unauthorized("AccessDenied", "You do not have access to this procurement request.");

        var comment = procurement.AddComment(userId, request.Text);
        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CommentResponse(comment.Id, userId, user.Name, comment.Text, comment.CreatedAt);
    }

    public async Task<Result<DashboardMetrics>> GetMetricsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var userResult = await GetUserOrError(userId, ct);
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        return await procurementRepo.GetMetricsAsync(user.Id, user.Role, ct);
    }

    private async Task<Result<User>> GetUserOrError(Guid userId, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        return user is null
            ? Error.NotFound("User", "User not found.")
            : user;
    }

    private static bool CanView(User user, ProcurementRequest procurement) =>
        user.Role switch
        {
            UserRole.Requester => procurement.RequesterId == user.Id,
            UserRole.Manager => procurement.RequesterId == user.Id || procurement.Status != ProcurementStatus.Draft,
            UserRole.Finance => procurement.RequesterId == user.Id ||
                procurement.Status is ProcurementStatus.ManagerApproved or
                    ProcurementStatus.FinanceApproved or
                    ProcurementStatus.FinanceRejected or
                    ProcurementStatus.PurchaseOrderIssued,
            UserRole.Admin => true,
            _ => false
        };

    private async Task<Result<ProcurementRequest>> GetProcurementOrError(Guid id, CancellationToken ct)
    {
        var procurement = await procurementRepo.GetByIdAsync(id, ct);
        return procurement is null
            ? Error.NotFound("Procurement", $"Procurement request {id} not found.")
            : procurement;
    }

    private async Task<Result<User>> RequireRole(Guid userId, UserRole requiredRole, string errorCode, string errorMessage, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        return user?.Role is var role && (role == requiredRole || role == UserRole.Admin)
            ? user!
            : Error.Unauthorized(errorCode, errorMessage);
    }

    private async Task<Result<ProcurementResponse>> SaveAndRespond(ProcurementRequest procurement, CancellationToken ct)
    {
        await procurementRepo.UpdateAsync(procurement, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var requester = await userRepo.GetByIdAsync(procurement.RequesterId, ct);
        return await MapToResponseAsync(procurement, requester!, ct);
    }

    private async Task<ProcurementResponse> MapToResponseAsync(ProcurementRequest p, User requester, CancellationToken ct = default)
    {
        var userIds = p.AuditEntries
            .Select(a => a.UserId)
            .Concat(p.Comments.Select(c => c.UserId))
            .Where(id => id != requester.Id)
            .Distinct()
            .ToList();

        var userLookup = new Dictionary<Guid, string> { [requester.Id] = requester.Name };
        if (userIds.Count > 0)
        {
            var users = await userRepo.GetByIdsAsync(userIds, ct);
            foreach (var user in users)
                userLookup[user.Id] = user.Name;
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

    private async Task<PagedResult<ProcurementListItem>> MapPagedListItemsAsync(
        PagedResult<ProcurementRequest> page,
        CancellationToken ct)
    {
        var requesterIds = page.Items.Select(p => p.RequesterId).Distinct().ToList();
        IReadOnlyList<User> requesters = requesterIds.Count == 0
            ? []
            : await userRepo.GetByIdsAsync(requesterIds, ct);
        var requesterMap = requesters.ToDictionary(u => u.Id);

        return new PagedResult<ProcurementListItem>(
            page.Items.Select(p => MapToListItem(p, requesterMap.GetValueOrDefault(p.RequesterId))).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems,
            page.TotalPages);
    }

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
