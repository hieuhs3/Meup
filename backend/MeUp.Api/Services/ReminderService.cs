using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class ReminderService : IReminderService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IEmailSender _email;

    public ReminderService(AppDbContext db, INotificationService notifications, IEmailSender email)
    {
        _db = db;
        _notifications = notifications;
        _email = email;
    }

    public async Task<NotificationDto?> GenerateForUserAsync(Guid userId, DateOnly date)
    {
        var overdue = await _db.Tasks.CountAsync(t =>
            t.UserId == userId && !t.IsDone && t.DueDate != null && t.DueDate < date);
        var events = await _db.CalendarEvents.CountAsync(e => e.UserId == userId && e.Date == date);
        var medsTotal = await _db.Medications.CountAsync(m => m.UserId == userId);
        var medsTaken = await _db.MedicationIntakes.CountAsync(i => i.UserId == userId && i.Date == date);
        var medsUntaken = Math.Max(0, medsTotal - medsTaken);

        if (overdue == 0 && events == 0 && medsUntaken == 0) return null;

        var parts = new List<string>();
        if (events > 0) parts.Add($"{events} sự kiện hôm nay");
        if (overdue > 0) parts.Add($"{overdue} việc quá hạn");
        if (medsUntaken > 0) parts.Add($"{medsUntaken} thuốc chưa uống");
        var message = string.Join(" · ", parts) + ".";

        var dto = await _notifications.CreateAsync(
            userId, "reminder", "Nhắc việc hôm nay", message, "/app/today",
            dedupKey: $"daily:{date:yyyy-MM-dd}");

        if (dto is not null)
        {
            var email = await _db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(email))
                await _email.SendAsync(email, "MeUp — Nhắc việc hôm nay", $"<p>{message}</p><p>Xem chi tiết trong mục Hôm nay.</p>");
        }
        return dto;
    }

    public async Task GenerateForAllAsync(DateOnly date)
    {
        var userIds = await _db.Users.Select(u => u.Id).ToListAsync();
        foreach (var id in userIds)
            await GenerateForUserAsync(id, date);
    }
}
