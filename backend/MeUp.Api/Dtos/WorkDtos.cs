using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

// --- Task ---

public record TaskDto(
    Guid Id,
    string Title,
    bool IsDone,
    DateOnly? DueDate,
    bool IsOverdue,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    string Recurrence,
    Guid? GoalId,
    Guid? ParentTaskId,
    string Status);

public record SetTaskStatusRequest(
    [Required]
    [RegularExpression("todo|in_progress|review|done|cancelled", ErrorMessage = "Trạng thái không hợp lệ.")]
    string Status);

public record CreateTaskRequest(
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string Title,
    DateOnly? DueDate,

    [RegularExpression("none|daily|weekly|monthly", ErrorMessage = "Chu kỳ lặp không hợp lệ.")]
    string? Recurrence,

    /// <summary>Mục tiêu chứa task (task cấp 1). Bỏ trống = task tự do.</summary>
    Guid? GoalId = null,

    /// <summary>Task cha nếu đây là sub-task. Khi có, GoalId được kế thừa từ cha.</summary>
    Guid? ParentTaskId = null);

public record UpdateTaskRequest(
    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string Title,
    DateOnly? DueDate,
    bool IsDone,

    [RegularExpression("none|daily|weekly|monthly", ErrorMessage = "Chu kỳ lặp không hợp lệ.")]
    string? Recurrence);

// --- Goal ---

/// <summary>Mục tiêu (1 nút trong cây). <c>Progress</c> tính tự động (rollup từ goal con + task con).</summary>
public record GoalDto(
    Guid Id,
    string Name,
    int Progress,
    DateTime CreatedAt,
    int TaskCount,
    int DoneCount,
    string Level,
    string Status,
    string? Description,
    DateOnly? TargetDate,
    Guid? ParentGoalId,
    int ChildCount);

/// <summary>Nút cây mục tiêu (lồng nhau) cho dashboard.</summary>
public record GoalTreeNodeDto(
    Guid Id,
    string Name,
    int Progress,
    string Level,
    string Status,
    string? Description,
    DateOnly? TargetDate,
    Guid? ParentGoalId,
    int TaskCount,
    int DoneCount,
    DateTime CreatedAt,
    IReadOnlyList<GoalTreeNodeDto> Children);

public record CreateGoalRequest(
    [Required(ErrorMessage = "Tên mục tiêu là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên mục tiêu tối đa 150 ký tự.")]
    string Name,

    [RegularExpression("life|year|quarter|month|week", ErrorMessage = "Cấp mục tiêu không hợp lệ.")]
    string? Level = null,

    [RegularExpression("draft|active|completed|cancelled|archived", ErrorMessage = "Trạng thái không hợp lệ.")]
    string? Status = null,

    [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    string? Description = null,

    DateOnly? TargetDate = null,

    Guid? ParentGoalId = null);

public record UpdateGoalRequest(
    [Required(ErrorMessage = "Tên mục tiêu là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên mục tiêu tối đa 150 ký tự.")]
    string Name,

    [RegularExpression("life|year|quarter|month|week", ErrorMessage = "Cấp mục tiêu không hợp lệ.")]
    string? Level = null,

    [RegularExpression("draft|active|completed|cancelled|archived", ErrorMessage = "Trạng thái không hợp lệ.")]
    string? Status = null,

    [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    string? Description = null,

    DateOnly? TargetDate = null,

    Guid? ParentGoalId = null);

/// <summary>Lỗi nghiệp vụ khi validate mục tiêu (cấp cha sai / tạo vòng / cha không tồn tại).</summary>
public class GoalValidationException(string message) : Exception(message);

// --- Habit ---

public record HabitDto(
    Guid Id,
    string Name,
    DateOnly Date,
    bool Checked,
    int Streak,
    DateTime CreatedAt,
    string Frequency,
    int? TargetPerWeek,
    int BestStreak,
    int CompletionRate,
    IReadOnlyList<DateOnly> RecentChecks);

public record CreateHabitRequest(
    [Required(ErrorMessage = "Tên thói quen là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thói quen tối đa 150 ký tự.")]
    string Name,

    [RegularExpression("daily|weekly", ErrorMessage = "Tần suất không hợp lệ.")]
    string? Frequency = null,

    [Range(1, 7, ErrorMessage = "Mục tiêu mỗi tuần từ 1 đến 7.")]
    int? TargetPerWeek = null);

public record UpdateHabitRequest(
    [Required(ErrorMessage = "Tên thói quen là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thói quen tối đa 150 ký tự.")]
    string Name,

    [RegularExpression("daily|weekly", ErrorMessage = "Tần suất không hợp lệ.")]
    string? Frequency = null,

    [Range(1, 7, ErrorMessage = "Mục tiêu mỗi tuần từ 1 đến 7.")]
    int? TargetPerWeek = null);

// --- Tổng quan ---

public record WorkSummaryDto(
    int TasksTotal,
    int TasksDone,
    int TasksOverdue,
    int GoalsCount,
    int GoalsAvgProgress,
    int HabitsTotal,
    int HabitsCheckedToday);
