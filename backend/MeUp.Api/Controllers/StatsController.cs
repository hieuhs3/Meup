using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly IStatsService _stats;

    public StatsController(IStatsService stats) => _stats = stats;

    /// <summary>Thống kê F1/F2/F3 trong khoảng [from, to]. Mặc định: tháng hiện tại.</summary>
    [HttpGet]
    public async Task<IActionResult> GetStats([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? new DateOnly(today.Year, today.Month, 1);
        var t = to ?? today;
        return Ok(await _stats.GetStatsAsync(User.GetUserId(), f, t));
    }
}
