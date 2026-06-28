using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/health")]
[Authorize]
public class HealthController : ControllerBase
{
    private readonly IHealthService _health;

    public HealthController(IHealthService health) => _health = health;

    private Guid UserId => User.GetUserId();

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        => Ok(await _health.GetLogsAsync(UserId, from, to));

    [HttpGet("logs/{date}")]
    public async Task<IActionResult> GetLog(DateOnly date)
        => Ok(await _health.GetLogAsync(UserId, date));

    [HttpPut("logs/{date}")]
    public async Task<IActionResult> UpsertLog(DateOnly date, UpsertHealthLogRequest request)
        => Ok(await _health.UpsertLogAsync(UserId, date, request));

    [HttpDelete("logs/{date}")]
    public async Task<IActionResult> DeleteLog(DateOnly date)
        => await _health.DeleteLogAsync(UserId, date) ? NoContent() : NotFound();

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? date)
        => Ok(await _health.GetSummaryAsync(UserId, date ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    // --- Hoạt động & xu hướng (G5) ---

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        => Ok(await _health.GetActivitiesAsync(UserId, from, to));

    [HttpPost("activities")]
    public async Task<IActionResult> CreateActivity(SaveActivityRequest request)
        => Ok(await _health.CreateActivityAsync(UserId, request));

    [HttpPut("activities/{id:guid}")]
    public async Task<IActionResult> UpdateActivity(Guid id, SaveActivityRequest request)
    {
        var dto = await _health.UpdateActivityAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("activities/{id:guid}")]
    public async Task<IActionResult> DeleteActivity(Guid id)
        => await _health.DeleteActivityAsync(UserId, id) ? NoContent() : NotFound();

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(await _health.GetTrendsAsync(UserId, from ?? today.AddDays(-30), to ?? today));
    }
}
