using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid userId, string? category);
    Task<DocumentResult> UploadAsync(Guid userId, string? category, string fileName, string contentType, long size, Stream content);
    /// <summary>Trả (metadata, luồng nội dung) nếu thuộc về user; ngược lại null.</summary>
    Task<(DocumentDto Meta, Stream Content)?> DownloadAsync(Guid userId, Guid id);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
