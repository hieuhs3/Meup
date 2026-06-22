using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IHealthService
{
    Task<HealthLogDto?> GetLogAsync(Guid userId, DateOnly date);
    Task<IReadOnlyList<HealthLogDto>> GetLogsAsync(Guid userId, DateOnly? from, DateOnly? to);
    Task<HealthLogDto> UpsertLogAsync(Guid userId, DateOnly date, UpsertHealthLogRequest request);
    Task<bool> DeleteLogAsync(Guid userId, DateOnly date);
    Task<HealthSummaryDto> GetSummaryAsync(Guid userId, DateOnly date);
}
