using Microsoft.AspNetCore.Mvc;
using RpFlo.Application.DTOs;
using RpFlo.Application.Interfaces;

namespace RpFlo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(INotificationRepository notificationRepo, IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetAll(
        [FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct)
    {
        var notifications = await notificationRepo.GetByUserIdAsync(userId, ct);
        var response = notifications
            .Select(n => new NotificationResponse(n.Id, n.Title, n.Message, n.ReferenceId, n.IsRead, n.CreatedAt))
            .ToList();
        return Ok(response);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(
        [FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct) =>
        Ok(await notificationRepo.GetUnreadCountAsync(userId, ct));

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkAsRead(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var marked = await notificationRepo.MarkAsReadAsync(id, userId, ct);
        if (!marked)
            return NotFound();

        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllAsRead(
        [FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct)
    {
        await notificationRepo.MarkAllAsReadAsync(userId, ct);
        return NoContent();
    }
}
