using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class NoteService : INoteService
{
    private readonly AppDbContext _db;

    public NoteService(AppDbContext db) => _db = db;

    private static NoteDto ToDto(Note n) => new(n.Id, n.Content, n.CreatedAt, n.UpdatedAt);

    public async Task<IReadOnlyList<NoteDto>> GetNotesAsync(Guid userId)
        => await _db.Notes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new NoteDto(n.Id, n.Content, n.CreatedAt, n.UpdatedAt))
            .ToListAsync();

    public async Task<NoteDto> CreateNoteAsync(Guid userId, UpsertNoteRequest request)
    {
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        return ToDto(note);
    }

    public async Task<NoteDto?> UpdateNoteAsync(Guid userId, Guid id, UpsertNoteRequest request)
    {
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note is null) return null;

        note.Content = request.Content.Trim();
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(note);
    }

    public async Task<bool> DeleteNoteAsync(Guid userId, Guid id)
    {
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note is null) return false;
        _db.Notes.Remove(note);
        await _db.SaveChangesAsync();
        return true;
    }
}
