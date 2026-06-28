using Microsoft.Extensions.Hosting;

namespace MeUp.Api.Services;

/// <summary>
/// Lưu file vào thư mục cục bộ (mặc định: ContentRoot/storage). KHÔNG đặt trong wwwroot
/// để file không bị phục vụ tĩnh — tải về phải qua controller có kiểm quyền sở hữu.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "storage", "documents");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(_root, key);
        await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fs, ct);
        return key;
    }

    public Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default)
    {
        var path = SafePath(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream s = new FileStream(path, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(s);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var path = SafePath(storageKey);
        if (path is not null && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Chống path traversal: chỉ cho phép tên file thuần nằm trong thư mục gốc.</summary>
    private string? SafePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey != Path.GetFileName(storageKey))
            return null;
        return Path.Combine(_root, storageKey);
    }
}
