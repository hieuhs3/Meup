using System.Text.RegularExpressions;
using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public partial class NoteService : INoteService
{
    private readonly AppDbContext _db;

    public NoteService(AppDbContext db) => _db = db;

    /// <summary>Khớp liên kết [[tiêu đề]] trong nội dung.</summary>
    [GeneratedRegex(@"\[\[([^\[\]]+)\]\]")]
    private static partial Regex LinkRegex();

    private static List<string> ParseOutLinks(string content) =>
        LinkRegex().Matches(content)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Dựng DTO cho 1 note dựa trên toàn bộ note của user (để tính backlinks).</summary>
    private static NoteDto ToDto(Note n, IReadOnlyList<Note> all)
    {
        var outLinks = ParseOutLinks(n.Content);

        var backlinks = new List<NoteRefDto>();
        if (!string.IsNullOrWhiteSpace(n.Title))
        {
            foreach (var other in all)
            {
                if (other.Id == n.Id) continue;
                var refs = ParseOutLinks(other.Content);
                if (refs.Any(r => string.Equals(r, n.Title, StringComparison.OrdinalIgnoreCase)))
                    backlinks.Add(new NoteRefDto(other.Id, other.Title ?? other.Content[..Math.Min(40, other.Content.Length)]));
            }
        }

        return new NoteDto(n.Id, n.Title, n.Content, n.Category, n.Tags, outLinks, backlinks, n.CreatedAt, n.UpdatedAt);
    }

    public async Task<IReadOnlyList<NoteDto>> GetNotesAsync(Guid userId, string? tag = null, string? category = null, string? q = null)
    {
        var all = await _db.Notes.Where(n => n.UserId == userId).ToListAsync();

        IEnumerable<Note> filtered = all;
        if (!string.IsNullOrWhiteSpace(tag))
            filtered = filtered.Where(n => n.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(n => string.Equals(n.Category, category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(q))
            filtered = filtered.Where(n =>
                n.Content.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (n.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));

        return filtered
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => ToDto(n, all))
            .ToList();
    }

    private static List<string> CleanTags(List<string>? tags) =>
        (tags ?? [])
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

    public async Task<NoteDto> CreateNoteAsync(Guid userId, UpsertNoteRequest request)
    {
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            Content = request.Content.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Tags = CleanTags(request.Tags),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync();
        var all = await _db.Notes.Where(n => n.UserId == userId).ToListAsync();
        return ToDto(note, all);
    }

    public async Task<NoteDto?> UpdateNoteAsync(Guid userId, Guid id, UpsertNoteRequest request)
    {
        var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note is null) return null;

        note.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        note.Content = request.Content.Trim();
        note.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        note.Tags = CleanTags(request.Tags);
        note.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var all = await _db.Notes.Where(n => n.UserId == userId).ToListAsync();
        return ToDto(note, all);
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
