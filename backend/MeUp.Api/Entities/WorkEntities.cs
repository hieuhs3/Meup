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

/// <summary>Mục tiêu dài hạn với tiến độ 0–100%.</summary>
public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Tiến độ 0–100.</summary>
    public int Progress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Thói quen cần duy trì theo ngày.</summary>
public class Habit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<HabitCheck> Checks { get; set; } = new List<HabitCheck>();
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
