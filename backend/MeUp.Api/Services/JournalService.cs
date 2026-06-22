using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class JournalService : IJournalService
{
    private readonly AppDbContext _db;

    public JournalService(AppDbContext db) => _db = db;

    private static JournalEntryDto ToDto(JournalEntry e) =>
        new(e.Id, e.Date, e.Title, e.ContentHtml, e.CreatedAt, e.UpdatedAt);

    public async Task<IReadOnlyList<JournalEntryDto>> GetEntriesAsync(Guid userId, DateOnly? from, DateOnly? to, string? q)
    {
        var query = _db.JournalEntries.Where(e => e.UserId == userId);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q}%";
            query = query.Where(e =>
                (e.Title != null && EF.Functions.ILike(e.Title, pattern)) ||
                EF.Functions.ILike(e.ContentHtml, pattern));
        }

        var entries = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.UpdatedAt)
            .ToListAsync();
        return entries.Select(ToDto).ToList();
    }

    public async Task<JournalEntryDto?> GetEntryAsync(Guid userId, Guid id)
    {
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        return entry is null ? null : ToDto(entry);
    }

    public async Task<JournalEntryDto> CreateEntryAsync(Guid userId, UpsertJournalRequest request)
    {
        var now = DateTime.UtcNow;
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            ContentHtml = request.ContentHtml ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> UpdateEntryAsync(Guid userId, Guid id, UpsertJournalRequest request)
    {
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return null;

        entry.Date = request.Date;
        entry.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        entry.ContentHtml = request.ContentHtml ?? string.Empty;
        entry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(entry);
    }

    public async Task<bool> DeleteEntryAsync(Guid userId, Guid id)
    {
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (entry is null) return false;

        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return true;
    }
}
