using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IHealthService
{
    Task<HealthLogDto?> GetLogAsync(Guid userId, DateOnly date);
    Task<IReadOnlyList<HealthLogDto>> GetLogsAsync(Guid userId, DateOnly? from, DateOnly? to);
    Task<HealthLogDto> UpsertLogAsync(Guid userId, DateOnly date, UpsertHealthLogRequest request);
    Task<bool> DeleteLogAsync(Guid userId, DateOnly date);
    Task<HealthSummaryDto> GetSummaryAsync(Guid userId, DateOnly date);

    // Hoạt động & xu hướng (G5)
    Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(Guid userId, DateOnly? from, DateOnly? to);
    Task<ActivityDto> CreateActivityAsync(Guid userId, SaveActivityRequest request);
    Task<ActivityDto?> UpdateActivityAsync(Guid userId, Guid id, SaveActivityRequest request);
    Task<bool> DeleteActivityAsync(Guid userId, Guid id);
    Task<HealthTrendDto> GetTrendsAsync(Guid userId, DateOnly from, DateOnly to);
}
