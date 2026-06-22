using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly IReminderService _reminders;

    public NotificationsController(INotificationService notifications, IReminderService reminders)
    {
        _notifications = notifications;
        _reminders = reminders;
    }

    private Guid UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _notifications.GetAsync(UserId));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount() => Ok(new { count = await _notifications.UnreadCountAsync(UserId) });

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
        => await _notifications.MarkReadAsync(UserId, id) ? NoContent() : NotFound();

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifications.MarkAllReadAsync(UserId);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _notifications.DeleteAsync(UserId, id) ? NoContent() : NotFound();

    /// <summary>Chạy nhắc thủ công cho người dùng hiện tại (nút "Nhắc ngay").</summary>
    [HttpPost("run-reminders")]
    public async Task<IActionResult> RunReminders()
    {
        var dto = await _reminders.GenerateForUserAsync(UserId, DateOnly.FromDateTime(DateTime.UtcNow));
        return Ok(new { created = dto is not null, notification = dto });
    }
}
