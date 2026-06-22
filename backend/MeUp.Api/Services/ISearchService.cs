using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(Guid userId, string? q);
}
