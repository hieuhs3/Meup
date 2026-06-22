using MeUp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _db;

    public ExportService(AppDbContext db) => _db = db;

    public async Task<object> ExportAsync(Guid userId)
    {
        return new
        {
            exportedAt = DateTime.UtcNow,
            categories = await _db.Categories.Where(x => x.UserId == userId)
                .Select(x => new { x.Name, x.Type, x.Color }).ToListAsync(),
            transactions = await _db.Transactions.Where(x => x.UserId == userId)
                .Select(x => new { x.Type, x.Amount, x.Date, x.Note, Category = x.Category!.Name }).ToListAsync(),
            budgets = await _db.Budgets.Where(x => x.UserId == userId)
                .Select(x => new { Category = x.Category!.Name, x.Amount }).ToListAsync(),
            healthLogs = await _db.HealthLogs.Where(x => x.UserId == userId)
                .Select(x => new { x.Date, x.Weight, x.SleepHours, x.WaterMl, x.WorkoutMinutes, x.Note }).ToListAsync(),
            medications = await _db.Medications.Where(x => x.UserId == userId)
                .Select(x => new { x.Name, x.Dosage, x.Note }).ToListAsync(),
            tasks = await _db.Tasks.Where(x => x.UserId == userId)
                .Select(x => new { x.Title, x.IsDone, x.DueDate, x.Recurrence }).ToListAsync(),
            goals = await _db.Goals.Where(x => x.UserId == userId)
                .Select(x => new { x.Name, x.Progress }).ToListAsync(),
            habits = await _db.Habits.Where(x => x.UserId == userId)
                .Select(x => new { x.Name }).ToListAsync(),
            events = await _db.CalendarEvents.Where(x => x.UserId == userId)
                .Select(x => new { x.Date, x.StartTime, x.EndTime, x.Title, x.Location, x.Note }).ToListAsync(),
            journal = await _db.JournalEntries.Where(x => x.UserId == userId)
                .Select(x => new { x.Date, x.Title, x.ContentHtml }).ToListAsync(),
            notes = await _db.Notes.Where(x => x.UserId == userId)
                .Select(x => new { x.Content, x.UpdatedAt }).ToListAsync(),
        };
    }
}
