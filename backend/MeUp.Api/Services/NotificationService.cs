using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt);

    public async Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId)
        => await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync();

    public Task<int> UnreadCountAsync(Guid userId)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> MarkReadAsync(Guid userId, Guid id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return false;
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return false;
        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<NotificationDto?> CreateAsync(Guid userId, string type, string title, string message,
        string? link = null, string? dedupKey = null)
    {
        if (dedupKey is not null &&
            await _db.Notifications.AnyAsync(n => n.UserId == userId && n.DedupKey == dedupKey))
            return null;

        var n = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            DedupKey = dedupKey,
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        return ToDto(n);
    }
}
