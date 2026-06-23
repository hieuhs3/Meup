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
    Guid? ParentTaskId);

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

/// <summary>Mục tiêu. <c>Progress</c> tính tự động = % task con đã xong.</summary>
public record GoalDto(Guid Id, string Name, int Progress, DateTime CreatedAt, int TaskCount, int DoneCount);

public record CreateGoalRequest(
    [Required(ErrorMessage = "Tên mục tiêu là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên mục tiêu tối đa 150 ký tự.")]
    string Name);

public record UpdateGoalRequest(
    [Required(ErrorMessage = "Tên mục tiêu là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên mục tiêu tối đa 150 ký tự.")]
    string Name);

// --- Habit ---

public record HabitDto(
    Guid Id,
    string Name,
    DateOnly Date,
    bool Checked,
    int Streak,
    DateTime CreatedAt);

public record CreateHabitRequest(
    [Required(ErrorMessage = "Tên thói quen là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thói quen tối đa 150 ký tự.")]
    string Name);

public record UpdateHabitRequest(
    [Required(ErrorMessage = "Tên thói quen là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thói quen tối đa 150 ký tự.")]
    string Name);

// --- Tổng quan ---

public record WorkSummaryDto(
    int TasksTotal,
    int TasksDone,
    int TasksOverdue,
    int GoalsCount,
    int GoalsAvgProgress,
    int HabitsTotal,
    int HabitsCheckedToday);
