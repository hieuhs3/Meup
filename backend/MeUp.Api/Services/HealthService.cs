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
        new(l.Date, l.Weight, l.HeightCm, Bmi(l.Weight, l.HeightCm),
            l.SleepHours, l.WaterMl, l.WorkoutMinutes, l.Note, l.UpdatedAt);

    /// <summary>BMI = cân nặng(kg) / chiều cao(m)². Null nếu thiếu dữ liệu.</summary>
    private static decimal? Bmi(decimal? weightKg, decimal? heightCm)
    {
        if (weightKg is null || heightCm is null || heightCm <= 0) return null;
        var m = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (m * m), 1);
    }

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
        log.HeightCm = request.HeightCm;
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

    // --- Hoạt động & xu hướng (G5) ---

    private static ActivityDto ToDto(Activity a) =>
        new(a.Id, a.Date, a.Type, a.DurationMin, a.Calories, a.Note, a.CreatedAt);

    public async Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(Guid userId, DateOnly? from, DateOnly? to)
    {
        var query = _db.Activities.Where(a => a.UserId == userId);
        if (from is not null) query = query.Where(a => a.Date >= from);
        if (to is not null) query = query.Where(a => a.Date <= to);
        return await query
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    public async Task<ActivityDto> CreateActivityAsync(Guid userId, SaveActivityRequest request)
    {
        var a = new Activity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            Type = ActivityType.Normalize(request.Type),
            DurationMin = request.DurationMin,
            Calories = request.Calories,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };
        _db.Activities.Add(a);
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task<ActivityDto?> UpdateActivityAsync(Guid userId, Guid id, SaveActivityRequest request)
    {
        var a = await _db.Activities.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (a is null) return null;
        a.Date = request.Date;
        a.Type = ActivityType.Normalize(request.Type);
        a.DurationMin = request.DurationMin;
        a.Calories = request.Calories;
        a.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await _db.SaveChangesAsync();
        return ToDto(a);
    }

    public async Task<bool> DeleteActivityAsync(Guid userId, Guid id)
    {
        var a = await _db.Activities.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (a is null) return false;
        _db.Activities.Remove(a);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<HealthTrendDto> GetTrendsAsync(Guid userId, DateOnly from, DateOnly to)
    {
        var logs = await _db.HealthLogs
            .Where(l => l.UserId == userId && l.Date >= from && l.Date <= to)
            .OrderBy(l => l.Date)
            .Select(l => new { l.Date, l.Weight, l.HeightCm })
            .ToListAsync();

        var weight = logs.Where(l => l.Weight != null)
            .Select(l => new TrendPointDto(l.Date, l.Weight)).ToList();
        var bmi = logs.Select(l => new { l.Date, B = Bmi(l.Weight, l.HeightCm) })
            .Where(x => x.B != null)
            .Select(x => new TrendPointDto(x.Date, x.B)).ToList();

        var calories = (await _db.Activities
                .Where(a => a.UserId == userId && a.Date >= from && a.Date <= to && a.Calories != null)
                .GroupBy(a => a.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Calories!.Value) })
                .ToListAsync())
            .OrderBy(x => x.Date)
            .Select(x => new TrendPointDto(x.Date, x.Total))
            .ToList();

        return new HealthTrendDto(weight, bmi, calories);
    }
}
