using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IStatsService
{
    Task<StatsDto> GetStatsAsync(Guid userId, DateOnly from, DateOnly to);
}
