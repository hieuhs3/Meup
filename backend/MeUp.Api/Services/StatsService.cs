using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class StatsService : IStatsService
{
    private readonly AppDbContext _db;

    public StatsService(AppDbContext db) => _db = db;

    public async Task<StatsDto> GetStatsAsync(Guid userId, DateOnly from, DateOnly to)
    {
        return new StatsDto(from, to,
            await FinanceAsync(userId, from, to),
            await HealthAsync(userId, from, to),
            await WorkAsync(userId, from, to));
    }

    private async Task<FinanceStats> FinanceAsync(Guid userId, DateOnly from, DateOnly to)
    {
        var txs = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
            .Select(t => new
            {
                t.Type,
                t.Amount,
                t.Date,
                CatName = t.Category == null ? null : t.Category.Name,
                CatColor = t.Category == null ? null : t.Category.Color,
            })
            .ToListAsync();

        var totalIncome = txs.Where(t => t.Type == FinanceType.Income).Sum(t => t.Amount);
        var totalExpense = txs.Where(t => t.Type == FinanceType.Expense).Sum(t => t.Amount);

        var byCategory = txs
            .GroupBy(t => new { Name = t.CatName ?? "Không phân loại", t.CatColor, t.Type })
            .Select(g => new CategoryStat(g.Key.Name, g.Key.CatColor, g.Key.Type, g.Sum(x => x.Amount)))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var daily = txs
            .GroupBy(t => t.Date)
            .Select(g => new DailyNet(
                g.Key,
                g.Where(x => x.Type == FinanceType.Income).Sum(x => x.Amount),
                g.Where(x => x.Type == FinanceType.Expense).Sum(x => x.Amount)))
            .OrderBy(d => d.Date)
            .ToList();

        return new FinanceStats(totalIncome, totalExpense, byCategory, daily);
    }

    private async Task<HealthStats> HealthAsync(Guid userId, DateOnly from, DateOnly to)
    {
        var logs = await _db.HealthLogs
            .Where(l => l.UserId == userId && l.Date >= from && l.Date <= to)
            .OrderBy(l => l.Date)
            .ToListAsync();

        decimal? Avg(IEnumerable<decimal?> xs)
        {
            var vals = xs.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return vals.Count > 0 ? Math.Round(vals.Average(), 1) : null;
        }
        int? AvgInt(IEnumerable<int?> xs)
        {
            var vals = xs.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            return vals.Count > 0 ? (int)Math.Round(vals.Average()) : null;
        }

        var weightSeries = logs
            .Where(l => l.Weight.HasValue)
            .Select(l => new WeightPoint(l.Date, l.Weight!.Value))
            .ToList();

        return new HealthStats(
            Avg(logs.Select(l => l.Weight)),
            Avg(logs.Select(l => l.SleepHours)),
            AvgInt(logs.Select(l => l.WaterMl)),
            AvgInt(logs.Select(l => l.WorkoutMinutes)),
            logs.Count,
            weightSeries);
    }

    private async Task<WorkStats> WorkAsync(Guid userId, DateOnly from, DateOnly to)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var tasksTotal = await _db.Tasks.CountAsync(t => t.UserId == userId);
        var tasksDone = await _db.Tasks.CountAsync(t =>
            t.UserId == userId && t.IsDone && t.CompletedAt != null
            && t.CompletedAt >= fromDt && t.CompletedAt < toDt);

        var progresses = await _db.Goals.Where(g => g.UserId == userId).Select(g => g.Progress).ToListAsync();
        var goalsAvg = progresses.Count > 0 ? (int)Math.Round(progresses.Average()) : 0;

        var habitsTotal = await _db.Habits.CountAsync(h => h.UserId == userId);
        var habitChecks = await _db.HabitChecks.CountAsync(c => c.UserId == userId && c.Date >= from && c.Date <= to);

        return new WorkStats(tasksTotal, tasksDone, progresses.Count, goalsAvg, habitsTotal, habitChecks);
    }
}
