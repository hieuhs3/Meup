namespace MeUp.Api.Entities;

/// <summary>Việc cần làm của người dùng (cô lập theo UserId). Đặt tên TaskItem để tránh trùng System.Threading.Tasks.Task.</summary>
public class TaskItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }

    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Lặp lại: "none" | "daily" | "weekly" | "monthly". Khi hoàn thành sẽ tự sinh lần kế.</summary>
    public string Recurrence { get; set; } = Entities.Recurrence.None;

    /// <summary>Độ ưu tiên: "low" | "medium" | "high" | "critical".</summary>
    public string Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Trạng thái Kanban: todo|in_progress|review|done|cancelled. Đồng bộ với IsDone.</summary>
    public string Status { get; set; } = WorkTaskStatus.Todo;

    /// <summary>Mục tiêu chứa task (null = task tự do). Denormalized cho mọi cấp trong cây để truy vấn nhanh.</summary>
    public Guid? GoalId { get; set; }

    /// <summary>Task cha (null = task cấp 1 dưới mục tiêu; có giá trị = sub-task).</summary>
    public Guid? ParentTaskId { get; set; }

    public ApplicationUser? User { get; set; }
    public Goal? Goal { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();
}

/// <summary>Giá trị hợp lệ của chu kỳ lặp task.</summary>
public static class Recurrence
{
    public const string None = "none";
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";

    public static bool IsRecurring(string? v) => v is Daily or Weekly or Monthly;
}

/// <summary>Độ ưu tiên của task.</summary>
public static class TaskPriority
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static bool IsValid(string? v) => v is Low or Medium or High or Critical;
    public static string Normalize(string? v) => IsValid(v) ? v! : Medium;
    /// <summary>Thứ hạng để sắp xếp: Critical lên đầu.</summary>
    public static int Rank(string v) => v switch { Critical => 0, High => 1, Medium => 2, Low => 3, _ => 2 };
}

/// <summary>Trạng thái Kanban của task. "done" tương ứng IsDone=true.</summary>
public static class WorkTaskStatus
{
    public const string Todo = "todo";
    public const string InProgress = "in_progress";
    public const string Review = "review";
    public const string Done = "done";
    public const string Cancelled = "cancelled";

    public static bool IsValid(string? v) => v is Todo or InProgress or Review or Done or Cancelled;
    public static string Normalize(string? v) => IsValid(v) ? v! : Todo;
}

/// <summary>
/// Mục tiêu, tổ chức theo cây nhiều cấp (đời → năm → quý → tháng → tuần).
/// Tiến độ <see cref="Progress"/> KHÔNG nhập tay — server tính rollup từ goal con + task trực thuộc.
/// </summary>
public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn (tùy chọn).</summary>
    public string? Description { get; set; }

    /// <summary>Cấp mục tiêu: life|year|quarter|month|week. Mặc định year.</summary>
    public string Level { get; set; } = GoalLevel.Year;

    /// <summary>Trạng thái vòng đời: draft|active|completed|cancelled|archived. Mặc định active.</summary>
    public string Status { get; set; } = GoalStatus.Active;

    /// <summary>Ngày mục tiêu hướng tới (tùy chọn).</summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>Mục tiêu cha (null = gốc). Cha phải có cấp cao hơn (ordinal nhỏ hơn).</summary>
    public Guid? ParentGoalId { get; set; }

    /// <summary>Tiến độ 0–100 (server tính rollup; cột giữ để tương thích/cache).</summary>
    public int Progress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public Goal? Parent { get; set; }
    public ICollection<Goal> Children { get; set; } = new List<Goal>();
}

/// <summary>Cấp của mục tiêu (thứ tự ordinal: nhỏ = cao hơn trong cây).</summary>
public static class GoalLevel
{
    public const string Life = "life";
    public const string Year = "year";
    public const string Quarter = "quarter";
    public const string Month = "month";
    public const string Week = "week";

    public static bool IsValid(string? v) => v is Life or Year or Quarter or Month or Week;
    public static string Normalize(string? v) => IsValid(v) ? v! : Year;
    /// <summary>Thứ hạng cấp: life=0 (cao nhất) … week=4 (thấp nhất).</summary>
    public static int Ordinal(string v) => v switch
    {
        Life => 0, Year => 1, Quarter => 2, Month => 3, Week => 4, _ => 1,
    };
}

/// <summary>Trạng thái vòng đời của mục tiêu.</summary>
public static class GoalStatus
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string Archived = "archived";

    public static bool IsValid(string? v) => v is Draft or Active or Completed or Cancelled or Archived;
    public static string Normalize(string? v) => IsValid(v) ? v! : Active;
    /// <summary>Bị loại khỏi mẫu số khi tính rollup tiến độ của cha.</summary>
    public static bool IsExcludedFromRollup(string v) => v is Cancelled or Archived;
}

/// <summary>Thói quen cần duy trì theo ngày.</summary>
public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Tần suất: daily|weekly. Mặc định daily.</summary>
    public string Frequency { get; set; } = HabitFrequency.Daily;

    /// <summary>Mục tiêu số lần/tuần (tùy chọn, dùng cho frequency=weekly).</summary>
    public int? TargetPerWeek { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<HabitCheck> Checks { get; set; } = new List<HabitCheck>();
}

/// <summary>Tần suất thói quen.</summary>
public static class HabitFrequency
{
    public const string Daily = "daily";
    public const string Weekly = "weekly";

    public static bool IsValid(string? v) => v is Daily or Weekly;
    public static string Normalize(string? v) => IsValid(v) ? v! : Daily;
}

/// <summary>Một lần check thói quen cho một ngày cụ thể.</summary>
public class HabitCheck
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    public Habit? Habit { get; set; }
}
