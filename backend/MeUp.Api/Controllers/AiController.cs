using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiInsightService _ai;

    public AiController(IAiInsightService ai) => _ai = ai;

    private Guid UserId => User.GetUserId();

    [HttpGet("status")]
    public IActionResult Status() => Ok(new { enabled = _ai.Enabled });

    [HttpGet("weekly-insight")]
    public async Task<IActionResult> WeeklyInsight([FromQuery] DateOnly? date, [FromQuery] bool refresh = false)
        => Ok(await _ai.GetWeeklyInsightAsync(UserId, date ?? DateOnly.FromDateTime(DateTime.UtcNow), refresh));

    [HttpPost("categorize")]
    public async Task<IActionResult> Categorize(CategorizeRequest request)
        => Ok(await _ai.SuggestCategoryAsync(UserId, request.Note, request.Type));
}
