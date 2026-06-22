using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events) => _events = events;

    private Guid UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        => Ok(await _events.GetEventsAsync(UserId, from, to));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var dto = await _events.GetEventAsync(UserId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(UpsertEventRequest request)
        => Ok(await _events.CreateEventAsync(UserId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid id, UpsertEventRequest request)
    {
        var dto = await _events.UpdateEventAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
        => await _events.DeleteEventAsync(UserId, id) ? NoContent() : NotFound();
}
