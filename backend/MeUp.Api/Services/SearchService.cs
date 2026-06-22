using System.Text.RegularExpressions;
using MeUp.Api.Data;
using MeUp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public partial class SearchService : ISearchService
{
    private const int PerType = 15;
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db) => _db = db;

    public async Task<SearchResultDto> SearchAsync(Guid userId, string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return new SearchResultDto([], 0);
        var p = $"%{q.Trim()}%";
        var hits = new List<SearchHit>();

        // Giao dịch (theo ghi chú)
        hits.AddRange(await _db.Transactions
            .Where(t => t.UserId == userId && t.Note != null && EF.Functions.ILike(t.Note, p))
            .OrderByDescending(t => t.Date).Take(PerType)
            .Select(t => new SearchHit("transaction", t.Id, t.Note!,
                (t.Type == "income" ? "+" : "−") + t.Amount.ToString("0"), t.Date))
            .ToListAsync());

        // Nhật ký (tiêu đề + nội dung)
        var journals = await _db.JournalEntries
            .Where(j => j.UserId == userId &&
                ((j.Title != null && EF.Functions.ILike(j.Title, p)) || EF.Functions.ILike(j.ContentHtml, p)))
            .OrderByDescending(j => j.Date).Take(PerType)
            .Select(j => new { j.Id, j.Title, j.ContentHtml, j.Date })
            .ToListAsync();
        hits.AddRange(journals.Select(j => new SearchHit(
            "journal", j.Id, j.Title ?? "(Nhật ký)", Snippet(j.ContentHtml), j.Date)));

        // Task (theo tiêu đề)
        hits.AddRange(await _db.Tasks
            .Where(t => t.UserId == userId && EF.Functions.ILike(t.Title, p))
            .OrderByDescending(t => t.CreatedAt).Take(PerType)
            .Select(t => new SearchHit("task", t.Id, t.Title, t.IsDone ? "đã xong" : "chưa xong", t.DueDate))
            .ToListAsync());

        // Sự kiện (tiêu đề + địa điểm + ghi chú)
        hits.AddRange(await _db.CalendarEvents
            .Where(e => e.UserId == userId &&
                (EF.Functions.ILike(e.Title, p)
                 || (e.Location != null && EF.Functions.ILike(e.Location, p))
                 || (e.Note != null && EF.Functions.ILike(e.Note, p))))
            .OrderByDescending(e => e.Date).Take(PerType)
            .Select(e => new SearchHit("event", e.Id, e.Title, e.Location, e.Date))
            .ToListAsync());

        return new SearchResultDto(hits, hits.Count);
    }

    private static string Snippet(string html)
    {
        var text = TagRegex().Replace(html, " ").Replace("&nbsp;", " ");
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text.Length > 120 ? text[..120] + "…" : text;
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
