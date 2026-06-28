using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/work")]
[Authorize]
public class WorkController : ControllerBase
{
    private readonly IWorkService _work;

    public WorkController(IWorkService work) => _work = work;

    private Guid UserId => User.GetUserId();
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    // --- Task ---

    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks([FromQuery] string? status)
        => Ok(await _work.GetTasksAsync(UserId, status));

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
        => Ok(await _work.CreateTaskAsync(UserId, request));

    [HttpPut("tasks/{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, UpdateTaskRequest request)
    {
        var dto = await _work.UpdateTaskAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("tasks/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleTask(Guid id)
    {
        var dto = await _work.ToggleTaskAsync(UserId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("tasks/{id:guid}/status")]
    public async Task<IActionResult> SetTaskStatus(Guid id, SetTaskStatusRequest request)
    {
        var dto = await _work.SetStatusAsync(UserId, id, request.Status);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id)
        => await _work.DeleteTaskAsync(UserId, id) ? NoContent() : NotFound();

    // --- Goal ---

    [HttpGet("goals")]
    public async Task<IActionResult> GetGoals([FromQuery] string? level, [FromQuery] string? status)
        => Ok(await _work.GetGoalsAsync(UserId, level, status));

    [HttpGet("goals/tree")]
    public async Task<IActionResult> GetGoalTree()
        => Ok(await _work.GetGoalTreeAsync(UserId));

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal(CreateGoalRequest request)
    {
        try { return Ok(await _work.CreateGoalAsync(UserId, request)); }
        catch (GoalValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("goals/{id:guid}")]
    public async Task<IActionResult> UpdateGoal(Guid id, UpdateGoalRequest request)
    {
        try
        {
            var dto = await _work.UpdateGoalAsync(UserId, id, request);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (GoalValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("goals/{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id)
        => await _work.DeleteGoalAsync(UserId, id) ? NoContent() : NotFound();

    // --- Habit ---

    [HttpGet("habits")]
    public async Task<IActionResult> GetHabits([FromQuery] DateOnly? date)
        => Ok(await _work.GetHabitsAsync(UserId, date ?? Today));

    [HttpPost("habits")]
    public async Task<IActionResult> CreateHabit(CreateHabitRequest request, [FromQuery] DateOnly? date)
        => Ok(await _work.CreateHabitAsync(UserId, request, date ?? Today));

    [HttpPut("habits/{id:guid}")]
    public async Task<IActionResult> UpdateHabit(Guid id, UpdateHabitRequest request, [FromQuery] DateOnly? date)
    {
        var dto = await _work.UpdateHabitAsync(UserId, id, request, date ?? Today);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("habits/{id:guid}")]
    public async Task<IActionResult> DeleteHabit(Guid id)
        => await _work.DeleteHabitAsync(UserId, id) ? NoContent() : NotFound();

    [HttpPost("habits/{id:guid}/check")]
    public async Task<IActionResult> CheckHabit(Guid id, [FromQuery] DateOnly? date)
    {
        var dto = await _work.SetCheckAsync(UserId, id, date ?? Today, true);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("habits/{id:guid}/check")]
    public async Task<IActionResult> UncheckHabit(Guid id, [FromQuery] DateOnly? date)
    {
        var dto = await _work.SetCheckAsync(UserId, id, date ?? Today, false);
        return dto is null ? NotFound() : Ok(dto);
    }

    // --- Tổng quan ---

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? date)
        => Ok(await _work.GetSummaryAsync(UserId, date ?? Today));
}
