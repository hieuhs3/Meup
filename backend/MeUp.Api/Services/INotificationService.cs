using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetAsync(Guid userId);
    Task<int> UnreadCountAsync(Guid userId);
    Task<bool> MarkReadAsync(Guid userId, Guid id);
    Task MarkAllReadAsync(Guid userId);
    Task<bool> DeleteAsync(Guid userId, Guid id);

    /// <summary>Tạo thông báo. Trả null nếu trùng (dedupKey đã tồn tại cho user).</summary>
    Task<NotificationDto?> CreateAsync(Guid userId, string type, string title, string message,
        string? link = null, string? dedupKey = null);
}
