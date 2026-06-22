using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IReminderService
{
    /// <summary>Sinh digest nhắc cho một user trong ngày (chống trùng). Trả null nếu không có gì để nhắc/đã nhắc.</summary>
    Task<NotificationDto?> GenerateForUserAsync(Guid userId, DateOnly date);

    /// <summary>Sinh nhắc cho tất cả user (dùng bởi background service).</summary>
    Task GenerateForAllAsync(DateOnly date);
}
