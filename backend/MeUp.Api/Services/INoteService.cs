using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface INoteService
{
    Task<IReadOnlyList<NoteDto>> GetNotesAsync(Guid userId, string? tag = null, string? category = null, string? q = null);
    Task<NoteDto> CreateNoteAsync(Guid userId, UpsertNoteRequest request);
    Task<NoteDto?> UpdateNoteAsync(Guid userId, Guid id, UpsertNoteRequest request);
    Task<bool> DeleteNoteAsync(Guid userId, Guid id);
}
