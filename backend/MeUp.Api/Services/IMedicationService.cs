using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IMedicationService
{
    Task<IReadOnlyList<MedicationDto>> GetMedicationsAsync(Guid userId, DateOnly date);
    Task<MedicationDto> CreateMedicationAsync(Guid userId, CreateMedicationRequest request, DateOnly date);
    Task<MedicationDto?> UpdateMedicationAsync(Guid userId, Guid id, UpdateMedicationRequest request, DateOnly date);
    Task<bool> DeleteMedicationAsync(Guid userId, Guid id);
    Task<MedicationDto?> SetIntakeAsync(Guid userId, Guid medicationId, DateOnly date, bool taken);
}
