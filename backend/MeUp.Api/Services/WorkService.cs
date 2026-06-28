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
            t.CompletedAt, t.CreatedAt, t.Recurrence, t.GoalId, t.ParentTaskId, t.Status);

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
        // Sub-task: kế thừa GoalId từ task cha. Task cấp 1: dùng GoalId yêu cầu (nếu mục tiêu hợp lệ).
        Guid? goalId = null;
        Guid? parentId = null;
        if (request.ParentTaskId is Guid pid)
        {
            var parent = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == pid && t.UserId == userId);
            if (parent is not null) { parentId = parent.Id; goalId = parent.GoalId; }
        }
        else if (request.GoalId is Guid gid && await _db.Goals.AnyAsync(g => g.Id == gid && g.UserId == userId))
        {
            goalId = gid;
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            Recurrence = NormalizeRecurrence(request.Recurrence),
            GoalId = goalId,
            ParentTaskId = parentId,
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

        // Hoàn thành task lặp lại → tự sinh lần kế tiếp.
        if (!wasDone && task.IsDone) SpawnRecurrence(userId, task);

        await _db.SaveChangesAsync();
        return ToDto(task);
    }

    public async Task<TaskDto?> SetStatusAsync(Guid userId, Guid id, string status)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (task is null) return null;

        var wasDone = task.IsDone;
        task.Status = WorkTaskStatus.Normalize(status);
        SetDone(task, task.Status == WorkTaskStatus.Done);

        if (!wasDone && task.IsDone) SpawnRecurrence(userId, task);

        await _db.SaveChangesAsync();
        return ToDto(task);
    }

    /// <summary>Task lặp lại hoàn thành → thêm lần kế tiếp theo chu kỳ.</summary>
    private void SpawnRecurrence(Guid userId, TaskItem task)
    {
        if (!Recurrence.IsRecurring(task.Recurrence) || task.DueDate is null) return;
        _db.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = task.Title,
            DueDate = NextDate(task.DueDate.Value, task.Recurrence),
            Recurrence = task.Recurrence,
            Priority = task.Priority,
            GoalId = task.GoalId,
            ParentTaskId = task.ParentTaskId,
        });
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
        // Đồng bộ trạng thái Kanban với cờ hoàn thành.
        if (done) task.Status = WorkTaskStatus.Done;
        else if (task.Status == WorkTaskStatus.Done) task.Status = WorkTaskStatus.Todo;
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

    private static int ProgressOf(int done, int total) => total > 0 ? (int)Math.Round(done * 100.0 / total) : 0;

    /// <summary>Ảnh chụp toàn bộ cây mục tiêu của user + tổng hợp task, tính rollup trong bộ nhớ (tránh N+1).</summary>
    private sealed class GoalGraph
    {
        public required List<Goal> Goals { get; init; }
        public required Dictionary<Guid, (int Total, int Done)> TaskAgg { get; init; }
        public required ILookup<Guid?, Goal> ChildrenByParent { get; init; }
        private readonly Dictionary<Guid, int> _memo = new();

        public int Effective(Goal g) => g.Status == GoalStatus.Completed ? 100 : Rollup(g.Id);
        public int TaskTotal(Guid id) => TaskAgg.TryGetValue(id, out var a) ? a.Total : 0;
        public int TaskDone(Guid id) => TaskAgg.TryGetValue(id, out var a) ? a.Done : 0;
        public int ChildCount(Guid id) => ChildrenByParent[id].Count();

        /// <summary>Trung bình đơn: mỗi goal con (không bị loại) + "rổ task con" = 1 phần tử.</summary>
        public int Rollup(Guid id)
        {
            if (_memo.TryGetValue(id, out var cached)) return cached;
            _memo[id] = 0; // chặn vòng (phòng thủ) trong lúc tính

            var buckets = new List<int>();
            foreach (var child in ChildrenByParent[id])
            {
                if (GoalStatus.IsExcludedFromRollup(child.Status)) continue;
                buckets.Add(Effective(child));
            }
            var total = TaskTotal(id);
            if (total > 0) buckets.Add(ProgressOf(TaskDone(id), total));

            var result = buckets.Count > 0 ? (int)Math.Round(buckets.Average()) : 0;
            _memo[id] = result;
            return result;
        }
    }

    private async Task<GoalGraph> LoadGraphAsync(Guid userId)
    {
        var goals = await _db.Goals.Where(g => g.UserId == userId).ToListAsync();
        var agg = (await _db.Tasks
                .Where(t => t.UserId == userId && t.GoalId != null)
                .GroupBy(t => t.GoalId!.Value)
                .Select(g => new { GoalId = g.Key, Total = g.Count(), Done = g.Count(t => t.IsDone) })
                .ToListAsync())
            .ToDictionary(x => x.GoalId, x => (x.Total, x.Done));
        return new GoalGraph
        {
            Goals = goals,
            TaskAgg = agg,
            ChildrenByParent = goals.ToLookup(g => g.ParentGoalId),
        };
    }

    private static GoalDto ToDto(GoalGraph graph, Goal g) =>
        new(g.Id, g.Name, graph.Effective(g), g.CreatedAt,
            graph.TaskTotal(g.Id), graph.TaskDone(g.Id),
            g.Level, g.Status, g.Description, g.TargetDate, g.ParentGoalId, graph.ChildCount(g.Id));

    public async Task<IReadOnlyList<GoalDto>> GetGoalsAsync(Guid userId, string? level = null, string? status = null)
    {
        var graph = await LoadGraphAsync(userId);
        return graph.Goals
            .Where(g => (level is null || g.Level == level) && (status is null || g.Status == status))
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => ToDto(graph, g))
            .ToList();
    }

    public async Task<IReadOnlyList<GoalTreeNodeDto>> GetGoalTreeAsync(Guid userId)
    {
        var graph = await LoadGraphAsync(userId);

        GoalTreeNodeDto Build(Goal g) => new(
            g.Id, g.Name, graph.Effective(g), g.Level, g.Status, g.Description, g.TargetDate, g.ParentGoalId,
            graph.TaskTotal(g.Id), graph.TaskDone(g.Id), g.CreatedAt,
            graph.ChildrenByParent[g.Id]
                .OrderByDescending(c => c.CreatedAt)
                .Select(Build)
                .ToList());

        return graph.ChildrenByParent[null]
            .OrderByDescending(g => g.CreatedAt)
            .Select(Build)
            .ToList();
    }

    public async Task<GoalDto> CreateGoalAsync(Guid userId, CreateGoalRequest request)
    {
        var level = GoalLevel.Normalize(request.Level);
        await ValidateParentAsync(userId, childId: null, request.ParentGoalId, level);

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Level = level,
            Status = GoalStatus.Normalize(request.Status),
            TargetDate = request.TargetDate,
            ParentGoalId = request.ParentGoalId,
            Progress = 0, // tiến độ tính tự động (rollup)
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();

        var graph = await LoadGraphAsync(userId);
        return ToDto(graph, goal);
    }

    public async Task<GoalDto?> UpdateGoalAsync(Guid userId, Guid id, UpdateGoalRequest request)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return null;

        var level = GoalLevel.Normalize(request.Level ?? goal.Level);
        await ValidateParentAsync(userId, childId: id, request.ParentGoalId, level);

        goal.Name = request.Name.Trim();
        goal.Description = request.Description?.Trim();
        goal.Level = level;
        goal.Status = GoalStatus.Normalize(request.Status ?? goal.Status);
        goal.TargetDate = request.TargetDate;
        goal.ParentGoalId = request.ParentGoalId;
        await _db.SaveChangesAsync();

        var graph = await LoadGraphAsync(userId);
        return ToDto(graph, goal);
    }

    public async Task<bool> DeleteGoalAsync(Guid userId, Guid id)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (goal is null) return false;
        _db.Goals.Remove(goal); // cascade: cây con + task thuộc các goal đó
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Validate mục tiêu cha: tồn tại + thuộc user + cấp cao hơn + không tạo vòng. Lỗi → <see cref="GoalValidationException"/>.</summary>
    private async Task ValidateParentAsync(Guid userId, Guid? childId, Guid? parentGoalId, string childLevel)
    {
        if (parentGoalId is not Guid pid) return;

        if (childId is Guid cid && pid == cid)
            throw new GoalValidationException("Không thể đặt mục tiêu làm cha của chính nó.");

        var parents = await _db.Goals
            .Where(g => g.UserId == userId)
            .Select(g => new { g.Id, g.Level, g.ParentGoalId })
            .ToListAsync();
        var byId = parents.ToDictionary(g => g.Id);

        if (!byId.TryGetValue(pid, out var parent))
            throw new GoalValidationException("Mục tiêu cha không tồn tại.");

        if (GoalLevel.Ordinal(parent.Level) >= GoalLevel.Ordinal(childLevel))
            throw new GoalValidationException("Mục tiêu cha phải có cấp cao hơn mục tiêu con.");

        // Chống vòng: đi ngược từ cha lên gốc; nếu gặp lại chính mục tiêu con → tạo vòng.
        if (childId is Guid self)
        {
            var cursor = parent.ParentGoalId;
            while (cursor is Guid c)
            {
                if (c == self) throw new GoalValidationException("Không thể tạo vòng lặp trong cây mục tiêu.");
                cursor = byId.TryGetValue(c, out var node) ? node.ParentGoalId : null;
            }
        }
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

    /// <summary>Cửa sổ heatmap (ngày) + cửa sổ tính tỷ lệ hoàn thành.</summary>
    private const int HeatmapDays = 84;   // 12 tuần
    private const int CompletionWindow = 30;

    private static HabitDto BuildHabitDto(Habit h, HashSet<DateOnly> dates, DateOnly date)
    {
        // Streak hiện tại: đếm lùi liên tiếp từ ngày tham chiếu.
        var streak = 0;
        var d = date;
        while (dates.Contains(d)) { streak++; d = d.AddDays(-1); }

        // Best streak: chuỗi liên tiếp dài nhất trong toàn bộ lịch sử.
        var bestStreak = 0;
        var ordered = dates.OrderBy(x => x).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var run = 1;
            while (i + 1 < ordered.Count && ordered[i + 1] == ordered[i].AddDays(1)) { run++; i++; }
            if (run > bestStreak) bestStreak = run;
        }

        // Tỷ lệ hoàn thành: số ngày check trong cửa sổ gần nhất / số ngày cửa sổ.
        var winStart = date.AddDays(-(CompletionWindow - 1));
        var checkedInWindow = dates.Count(x => x >= winStart && x <= date);
        var completionRate = (int)Math.Round(checkedInWindow * 100.0 / CompletionWindow);

        // Dữ liệu heatmap: các ngày được check trong 12 tuần gần nhất.
        var heatStart = date.AddDays(-(HeatmapDays - 1));
        var recentChecks = dates.Where(x => x >= heatStart && x <= date).OrderBy(x => x).ToList();

        return new HabitDto(h.Id, h.Name, date, dates.Contains(date), streak, h.CreatedAt,
            h.Frequency, h.TargetPerWeek, bestStreak, completionRate, recentChecks);
    }

    public async Task<HabitDto> CreateHabitAsync(Guid userId, CreateHabitRequest request, DateOnly date)
    {
        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Frequency = HabitFrequency.Normalize(request.Frequency),
            TargetPerWeek = request.TargetPerWeek,
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
        habit.Frequency = HabitFrequency.Normalize(request.Frequency);
        habit.TargetPerWeek = request.TargetPerWeek;
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

        // Tiến độ mục tiêu tính tự động từ task con.
        var goals = await GetGoalsAsync(userId);
        var goalsAvg = goals.Count > 0 ? (int)Math.Round(goals.Average(g => g.Progress)) : 0;

        var habitsTotal = await _db.Habits.CountAsync(h => h.UserId == userId);
        var habitsChecked = await _db.HabitChecks.CountAsync(c => c.UserId == userId && c.Date == date);

        return new WorkSummaryDto(
            tasksTotal, tasksDone, tasksOverdue,
            goals.Count, goalsAvg,
            habitsTotal, habitsChecked);
    }
}
