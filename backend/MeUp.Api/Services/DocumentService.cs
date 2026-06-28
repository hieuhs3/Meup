using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;

    public DocumentService(AppDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Giới hạn 10MB/file.</summary>
    private const long MaxSize = 10 * 1024 * 1024;

    /// <summary>Phần mở rộng cho phép (an toàn cơ bản).</summary>
    private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".txt", ".csv", ".zip",
    };

    private static DocumentDto ToDto(Document d) =>
        new(d.Id, d.Category, d.FileName, d.ContentType, d.Size, d.UploadedAt);

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid userId, string? category)
    {
        var query = _db.Documents.Where(d => d.UserId == userId);
        if (DocumentCategory.IsValid(category)) query = query.Where(d => d.Category == category);
        return await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => ToDto(d))
            .ToListAsync();
    }

    public async Task<DocumentResult> UploadAsync(
        Guid userId, string? category, string fileName, string contentType, long size, Stream content)
    {
        if (size <= 0) return DocumentResult.Fail("File rỗng.");
        if (size > MaxSize) return DocumentResult.Fail("File vượt quá 10MB.");

        var ext = Path.GetExtension(fileName);
        if (!AllowedExt.Contains(ext)) return DocumentResult.Fail("Định dạng file không được hỗ trợ.");

        var key = await _storage.SaveAsync(content, fileName);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = DocumentCategory.Normalize(category),
            FileName = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Size = size,
            StorageKey = key,
            UploadedAt = DateTime.UtcNow,
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();
        return DocumentResult.Ok(ToDto(doc));
    }

    public async Task<(DocumentDto Meta, Stream Content)?> DownloadAsync(Guid userId, Guid id)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (doc is null) return null;

        var stream = await _storage.OpenAsync(doc.StorageKey);
        if (stream is null) return null;
        return (ToDto(doc), stream);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (doc is null) return false;

        await _storage.DeleteAsync(doc.StorageKey);
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }
}
