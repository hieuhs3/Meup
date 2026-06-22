using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class WorkService : IWorkService
{
    private readonly AppDbContext _db;

    public WorkService(AppDbContext db) => _db = db;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    // --- Task ---

    private static TaskDto ToDto(TaskItem t) =>
        new(t.Id, t.Title, t.IsDone, t.DueDate,
            !t.IsDone && t.DueDate is not null && t.DueDate < Today,
            t.CompletedAt, t.CreatedAt, t.Recurrence);

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid userId, string? status)
    {
        var query = _db.Tasks.Where(t => t.UserId == userId);
        if (status == "active") query = query.Where(t => !t.IsDone);
        else if (status == "done") query = query.Where(t => t.IsDone);

        var tasks = await query
            .OrderBy(t => t.IsDone)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
        return tasks.Select(ToDto).ToList();
    }

    public async Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            Recurrence = NormalizeRecurrence(request.Recurrence),
        };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return ToDto(task);
    }

    public async Task<TaskDto?> UpdateTaskAsync(Guid userId, Guid id, UpdateTaskRequest request)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return null;

        task.Title = request.Title.Trim();
        task.DueDate = request.DueDate;
        task.Recurrence = NormalizeRecurrence(request.Recurrence);
        SetDone(task, request.IsDone);
        await _db.SaveChangesAsync();
        return ToDto(task);
    }

    public async Task<TaskDto?> ToggleTaskAsync(Guid userId, Guid id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return null;

        var wasDone = task.IsDone;
        SetDone(task, !task.IsDone);

        // Hoàn thành task lặp lại → tự sinh lần kế tiếp (chưa làm).
        if (!wasDone && task.IsDone && Recurrence.IsRecurring(task.Recurrence) && task.DueDate is not null)
        {
            _db.Tasks.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = task.Title,
                DueDate = NextDate(task.DueDate.Value, task.Recurrence),
                Recurrence = task.Recurrence,
            });
        }

        await _db.SaveChangesAsync();
        return ToDto(task);
    }

    private static string NormalizeRecurrence(string? r) =>
        Recurrence.IsRecurring(r) ? r! : Recurrence.None;

    private static DateOnly NextDate(DateOnly d, string recurrence) => recurrence switch
    {
        Recurrence.Daily => d.AddDays(1),
        Recurrence.Weekly => d.AddDays(7),
        Recurrence.Monthly => d.AddMonths(1),
        _ => d,
    };

    private static void SetDone(TaskItem task, bool done)
    {
        if (done && !task.IsDone) task.CompletedAt = DateTime.UtcNow;
        else if (!done) task.CompletedAt = null;
        task.IsDone = done;
    }

    public async Task<bool> DeleteTaskAsync(Guid userId, Guid id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return false;
        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Goal ---

    public async Task<IReadOnlyList<GoalDto>> GetGoalsAsync(Guid userId)
    {
        return await _db.Goals
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GoalDto(g.Id, g.Name, g.Progress, g.CreatedAt))
            .ToListAsync();
    }

    public async Task<GoalDto> CreateGoalAsync(Guid userId, CreateGoalRequest request)
    {
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Progress = request.Progress,
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return new GoalDto(goal.Id, goal.Name, goal.Progress, goal.CreatedAt);
    }

    public async Task<GoalDto?> UpdateGoalAsync(Guid userId, Guid id, UpdateGoalRequest request)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return null;

        goal.Name = request.Name.Trim();
        goal.Progress = request.Progress;
        await _db.SaveChangesAsync();
        return new GoalDto(goal.Id, goal.Name, goal.Progress, goal.CreatedAt);
    }

    public async Task<bool> DeleteGoalAsync(Guid userId, Guid id)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return false;
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Habit ---

    public async Task<IReadOnlyList<HabitDto>> GetHabitsAsync(Guid userId, DateOnly date)
    {
        var habits = await _db.Habits
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

        var checks = await _db.HabitChecks
            .Where(c => c.UserId == userId)
            .Select(c => new { c.HabitId, c.Date })
            .ToListAsync();

        var byHabit = checks
            .GroupBy(c => c.HabitId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Date).ToHashSet());

        return habits
            .Select(h => BuildHabitDto(h, byHabit.GetValueOrDefault(h.Id) ?? [], date))
            .ToList();
    }

    private static HabitDto BuildHabitDto(Habit h, HashSet<DateOnly> dates, DateOnly date)
    {
        var streak = 0;
        var d = date;
        while (dates.Contains(d)) { streak++; d = d.AddDays(-1); }
        return new HabitDto(h.Id, h.Name, date, dates.Contains(date), streak, h.CreatedAt);
    }

    public async Task<HabitDto> CreateHabitAsync(Guid userId, CreateHabitRequest request, DateOnly date)
    {
        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
        };
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();
        return BuildHabitDto(habit, [], date);
    }

    public async Task<HabitDto?> UpdateHabitAsync(Guid userId, Guid id, UpdateHabitRequest request, DateOnly date)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (habit is null) return null;

        habit.Name = request.Name.Trim();
        await _db.SaveChangesAsync();
        return await LoadHabitDtoAsync(userId, habit, date);
    }

    public async Task<bool> DeleteHabitAsync(Guid userId, Guid id)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (habit is null) return false;
        _db.Habits.Remove(habit); // HabitChecks cascade
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<HabitDto?> SetCheckAsync(Guid userId, Guid habitId, DateOnly date, bool isChecked)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null) return null;

        var existing = await _db.HabitChecks
            .FirstOrDefaultAsync(c => c.HabitId == habitId && c.Date == date);

        if (isChecked && existing is null)
        {
            _db.HabitChecks.Add(new HabitCheck
            {
                Id = Guid.NewGuid(),
                HabitId = habitId,
                UserId = userId,
                Date = date,
            });
            await _db.SaveChangesAsync();
        }
        else if (!isChecked && existing is not null)
        {
            _db.HabitChecks.Remove(existing);
            await _db.SaveChangesAsync();
        }

        return await LoadHabitDtoAsync(userId, habit, date);
    }

    private async Task<HabitDto> LoadHabitDtoAsync(Guid userId, Habit habit, DateOnly date)
    {
        var dates = (await _db.HabitChecks
                .Where(c => c.HabitId == habit.Id && c.UserId == userId)
                .Select(c => c.Date)
                .ToListAsync())
            .ToHashSet();
        return BuildHabitDto(habit, dates, date);
    }

    // --- Tổng quan ---

    public async Task<WorkSummaryDto> GetSummaryAsync(Guid userId, DateOnly date)
    {
        var tasks = await _db.Tasks.Where(t => t.UserId == userId)
            .Select(t => new { t.IsDone, t.DueDate })
            .ToListAsync();
        var tasksTotal = tasks.Count;
        var tasksDone = tasks.Count(t => t.IsDone);
        var tasksOverdue = tasks.Count(t => !t.IsDone && t.DueDate is not null && t.DueDate < date);

        var progresses = await _db.Goals.Where(g => g.UserId == userId)
            .Select(g => g.Progress).ToListAsync();
        var goalsAvg = progresses.Count > 0 ? (int)Math.Round(progresses.Average()) : 0;

        var habitsTotal = await _db.Habits.CountAsync(h => h.UserId == userId);
        var habitsChecked = await _db.HabitChecks.CountAsync(c => c.UserId == userId && c.Date == date);

        return new WorkSummaryDto(
            tasksTotal, tasksDone, tasksOverdue,
            progresses.Count, goalsAvg,
            habitsTotal, habitsChecked);
    }
}
