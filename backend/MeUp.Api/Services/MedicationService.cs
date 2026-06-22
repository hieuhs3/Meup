using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class MedicationService : IMedicationService
{
    private readonly AppDbContext _db;

    public MedicationService(AppDbContext db) => _db = db;

    private static MedicationDto ToDto(Medication m, DateOnly date, bool taken) =>
        new(m.Id, m.Name, m.Dosage, m.Note, date, taken, m.CreatedAt);

    public async Task<IReadOnlyList<MedicationDto>> GetMedicationsAsync(Guid userId, DateOnly date)
    {
        var meds = await _db.Medications
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Name)
            .ToListAsync();
        if (meds.Count == 0) return [];

        var takenIds = (await _db.MedicationIntakes
                .Where(i => i.UserId == userId && i.Date == date)
                .Select(i => i.MedicationId)
                .ToListAsync())
            .ToHashSet();

        return meds.Select(m => ToDto(m, date, takenIds.Contains(m.Id))).ToList();
    }

    public async Task<MedicationDto> CreateMedicationAsync(Guid userId, CreateMedicationRequest request, DateOnly date)
    {
        var med = new Medication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Dosage = Trim(request.Dosage),
            Note = Trim(request.Note),
        };
        _db.Medications.Add(med);
        await _db.SaveChangesAsync();
        return ToDto(med, date, false);
    }

    public async Task<MedicationDto?> UpdateMedicationAsync(Guid userId, Guid id, UpdateMedicationRequest request, DateOnly date)
    {
        var med = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (med is null) return null;

        med.Name = request.Name.Trim();
        med.Dosage = Trim(request.Dosage);
        med.Note = Trim(request.Note);
        await _db.SaveChangesAsync();

        var taken = await _db.MedicationIntakes.AnyAsync(i => i.MedicationId == id && i.Date == date);
        return ToDto(med, date, taken);
    }

    public async Task<bool> DeleteMedicationAsync(Guid userId, Guid id)
    {
        var med = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        if (med is null) return false;
        _db.Medications.Remove(med);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MedicationDto?> SetIntakeAsync(Guid userId, Guid medicationId, DateOnly date, bool taken)
    {
        var med = await _db.Medications.FirstOrDefaultAsync(m => m.Id == medicationId && m.UserId == userId);
        if (med is null) return null;

        var existing = await _db.MedicationIntakes
            .FirstOrDefaultAsync(i => i.MedicationId == medicationId && i.Date == date);

        if (taken && existing is null)
        {
            _db.MedicationIntakes.Add(new MedicationIntake
            {
                Id = Guid.NewGuid(),
                MedicationId = medicationId,
                UserId = userId,
                Date = date,
            });
            await _db.SaveChangesAsync();
        }
        else if (!taken && existing is not null)
        {
            _db.MedicationIntakes.Remove(existing);
            await _db.SaveChangesAsync();
        }

        return ToDto(med, date, taken);
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
