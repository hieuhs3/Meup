using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _notes;

    public NotesController(INoteService notes) => _notes = notes;

    private Guid UserId => User.GetUserId();

    [HttpGet]
    public async Task<IActionResult> GetNotes() => Ok(await _notes.GetNotesAsync(UserId));

    [HttpPost]
    public async Task<IActionResult> CreateNote(UpsertNoteRequest request)
        => Ok(await _notes.CreateNoteAsync(UserId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateNote(Guid id, UpsertNoteRequest request)
    {
        var dto = await _notes.UpdateNoteAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id)
        => await _notes.DeleteNoteAsync(UserId, id) ? NoContent() : NotFound();
}
