using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IWorkService
{
    // Task
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid userId, string? status);
    Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskRequest request);
    Task<TaskDto?> UpdateTaskAsync(Guid userId, Guid id, UpdateTaskRequest request);
    Task<TaskDto?> ToggleTaskAsync(Guid userId, Guid id);
    Task<bool> DeleteTaskAsync(Guid userId, Guid id);

    // Goal
    Task<IReadOnlyList<GoalDto>> GetGoalsAsync(Guid userId);
    Task<GoalDto> CreateGoalAsync(Guid userId, CreateGoalRequest request);
    Task<GoalDto?> UpdateGoalAsync(Guid userId, Guid id, UpdateGoalRequest request);
    Task<bool> DeleteGoalAsync(Guid userId, Guid id);

    // Habit
    Task<IReadOnlyList<HabitDto>> GetHabitsAsync(Guid userId, DateOnly date);
    Task<HabitDto> CreateHabitAsync(Guid userId, CreateHabitRequest request, DateOnly date);
    Task<HabitDto?> UpdateHabitAsync(Guid userId, Guid id, UpdateHabitRequest request, DateOnly date);
    Task<bool> DeleteHabitAsync(Guid userId, Guid id);
    Task<HabitDto?> SetCheckAsync(Guid userId, Guid habitId, DateOnly date, bool isChecked);

    // Tổng quan
    Task<WorkSummaryDto> GetSummaryAsync(Guid userId, DateOnly date);
}
