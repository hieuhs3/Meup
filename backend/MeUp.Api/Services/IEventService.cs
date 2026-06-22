using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetEventsAsync(Guid userId, DateOnly? from, DateOnly? to);
    Task<EventDto?> GetEventAsync(Guid userId, Guid id);
    Task<EventDto> CreateEventAsync(Guid userId, UpsertEventRequest request);
    Task<EventDto?> UpdateEventAsync(Guid userId, Guid id, UpsertEventRequest request);
    Task<bool> DeleteEventAsync(Guid userId, Guid id);
}
