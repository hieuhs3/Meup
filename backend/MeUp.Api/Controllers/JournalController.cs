using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/journal")]
[Authorize]
public class JournalController : ControllerBase
{
    private readonly IJournalService _journal;

    public JournalController(IJournalService journal) => _journal = journal;

    private Guid UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetEntries(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? q)
        => Ok(await _journal.GetEntriesAsync(UserId, from, to, q));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEntry(Guid id)
    {
        var dto = await _journal.GetEntryAsync(UserId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry(UpsertJournalRequest request)
        => Ok(await _journal.CreateEntryAsync(UserId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid id, UpsertJournalRequest request)
    {
        var dto = await _journal.UpdateEntryAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
        => await _journal.DeleteEntryAsync(UserId, id) ? NoContent() : NotFound();
}
