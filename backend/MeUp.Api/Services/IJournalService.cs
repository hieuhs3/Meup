using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IJournalService
{
    Task<IReadOnlyList<JournalEntryDto>> GetEntriesAsync(Guid userId, DateOnly? from, DateOnly? to, string? q);
    Task<JournalEntryDto?> GetEntryAsync(Guid userId, Guid id);
    Task<JournalEntryDto> CreateEntryAsync(Guid userId, UpsertJournalRequest request);
    Task<JournalEntryDto?> UpdateEntryAsync(Guid userId, Guid id, UpsertJournalRequest request);
    Task<bool> DeleteEntryAsync(Guid userId, Guid id);
}
