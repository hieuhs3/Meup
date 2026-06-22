using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class HealthService : IHealthService
{
    private readonly AppDbContext _db;

    public HealthService(AppDbContext db) => _db = db;

    private static HealthLogDto ToDto(HealthLog l) =>
        new(l.Date, l.Weight, l.SleepHours, l.WaterMl, l.WorkoutMinutes, l.Note, l.UpdatedAt);

    public async Task<HealthLogDto?> GetLogAsync(Guid userId, DateOnly date)
    {
        var log = await _db.HealthLogs
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date);
        return log is null ? null : ToDto(log);
    }

    public async Task<IReadOnlyList<HealthLogDto>> GetLogsAsync(Guid userId, DateOnly? from, DateOnly? to)
    {
        var query = _db.HealthLogs.Where(l => l.UserId == userId);
        if (from is not null) query = query.Where(l => l.Date >= from);
        if (to is not null) query = query.Where(l => l.Date <= to);

        var logs = await query
            .OrderByDescending(l => l.Date)
            .ToListAsync();
        return logs.Select(ToDto).ToList();
    }

    public async Task<HealthLogDto> UpsertLogAsync(Guid userId, DateOnly date, UpsertHealthLogRequest request)
    {
        var log = await _db.HealthLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date);
        var now = DateTime.UtcNow;

        if (log is null)
        {
            log = new HealthLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                CreatedAt = now,
            };
            _db.HealthLogs.Add(log);
        }

        log.Weight = request.Weight;
        log.SleepHours = request.SleepHours;
        log.WaterMl = request.WaterMl;
        log.WorkoutMinutes = request.WorkoutMinutes;
        log.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        log.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return ToDto(log);
    }

    public async Task<bool> DeleteLogAsync(Guid userId, DateOnly date)
    {
        var log = await _db.HealthLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date);
        if (log is null) return false;

        _db.HealthLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<HealthSummaryDto> GetSummaryAsync(Guid userId, DateOnly date)
    {
        var today = await _db.HealthLogs
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == date);

        var previous = await _db.HealthLogs
            .Where(l => l.UserId == userId && l.Date < date)
            .OrderByDescending(l => l.Date)
            .FirstOrDefaultAsync();

        return new HealthSummaryDto(
            date,
            today is null ? null : ToDto(today),
            previous is null ? null : ToDto(previous));
    }
}
