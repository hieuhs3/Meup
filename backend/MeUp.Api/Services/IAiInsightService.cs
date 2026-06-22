using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IAiInsightService
{
    /// <summary>Trạng thái AI cho người dùng (có key riêng / key server / bật được không).</summary>
    Task<AiStatusDto> GetStatusAsync(Guid userId);

    /// <summary>Lưu (mã hóa) API key của riêng người dùng.</summary>
    Task SetUserKeyAsync(Guid userId, string apiKey);

    /// <summary>Xóa API key của người dùng (quay về dùng key server nếu có).</summary>
    Task ClearUserKeyAsync(Guid userId);

    Task<WeeklyInsightDto> GetWeeklyInsightAsync(Guid userId, DateOnly date, bool refresh = false);
    Task<CategorySuggestionDto> SuggestCategoryAsync(Guid userId, string note, string type);
}
