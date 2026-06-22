namespace MeUp.Api.Dtos;

/// <summary>Một kết quả tìm kiếm. <see cref="Type"/>: transaction | journal | task | event.</summary>
public record SearchHit(string Type, Guid Id, string Title, string? Snippet, DateOnly? Date);

public record SearchResultDto(IReadOnlyList<SearchHit> Items, int Total);
