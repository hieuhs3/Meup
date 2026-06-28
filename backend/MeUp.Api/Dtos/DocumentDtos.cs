namespace MeUp.Api.Dtos;

public record DocumentDto(
    Guid Id,
    string Category,
    string FileName,
    string ContentType,
    long Size,
    DateTime UploadedAt);

/// <summary>Kết quả nghiệp vụ upload (lỗi validate type/size).</summary>
public record DocumentResult(DocumentDto? Value, string? Error = null)
{
    public bool Succeeded => Error is null;
    public static DocumentResult Ok(DocumentDto v) => new(v);
    public static DocumentResult Fail(string e) => new(null, e);
}
