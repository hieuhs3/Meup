using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IAiInsightService
{
    bool Enabled { get; }
    Task<WeeklyInsightDto> GetWeeklyInsightAsync(Guid userId, DateOnly date, bool refresh = false);
    Task<CategorySuggestionDto> SuggestCategoryAsync(Guid userId, string note, string type);
}
