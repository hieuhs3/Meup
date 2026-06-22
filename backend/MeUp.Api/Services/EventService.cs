using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db) => _db = db;

    private static EventDto ToDto(CalendarEvent e) =>
        new(e.Id, e.Date, e.StartTime, e.EndTime, e.Title, e.Location, e.Note);

    public async Task<IReadOnlyList<EventDto>> GetEventsAsync(Guid userId, DateOnly? from, DateOnly? to)
    {
        var query = _db.CalendarEvents.Where(e => e.UserId == userId);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);

        var events = await query
            .OrderBy(e => e.Date).ThenBy(e => e.StartTime)
            .ToListAsync();
        return events.Select(ToDto).ToList();
    }

    public async Task<EventDto?> GetEventAsync(Guid userId, Guid id)
    {
        var e = await _db.CalendarEvents.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        return e is null ? null : ToDto(e);
    }

    public async Task<EventDto> CreateEventAsync(Guid userId, UpsertEventRequest request)
    {
        var ev = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title.Trim(),
            Location = Trim(request.Location),
            Note = Trim(request.Note),
        };
        _db.CalendarEvents.Add(ev);
        await _db.SaveChangesAsync();
        return ToDto(ev);
    }

    public async Task<EventDto?> UpdateEventAsync(Guid userId, Guid id, UpsertEventRequest request)
    {
        var ev = await _db.CalendarEvents.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (ev is null) return null;

        ev.Date = request.Date;
        ev.StartTime = request.StartTime;
        ev.EndTime = request.EndTime;
        ev.Title = request.Title.Trim();
        ev.Location = Trim(request.Location);
        ev.Note = Trim(request.Note);
        await _db.SaveChangesAsync();
        return ToDto(ev);
    }

    public async Task<bool> DeleteEventAsync(Guid userId, Guid id)
    {
        var ev = await _db.CalendarEvents.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (ev is null) return false;
        _db.CalendarEvents.Remove(ev);
        await _db.SaveChangesAsync();
        return true;
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
