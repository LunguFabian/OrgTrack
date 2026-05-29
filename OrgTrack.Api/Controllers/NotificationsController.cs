using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Api.Extensions;
using OrgTrack.Application.UseCases;

namespace OrgTrack.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(NotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 50)
    {
        var userId = User.GetUserId();
        var notifications = await notificationService.GetUserNotificationsAsync(userId, limit);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        var count = await notificationService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await notificationService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        await notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }
}
