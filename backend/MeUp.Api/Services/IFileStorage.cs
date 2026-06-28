namespace MeUp.Api.Services;

/// <summary>
/// Trừu tượng lưu trữ file nhị phân. Bản mặc định lưu đĩa local; sau có thể cắm MinIO/S3
/// mà không đổi DocumentService.
/// </summary>
public interface IFileStorage
{
    /// <summary>Lưu nội dung, trả về khóa lưu trữ (storage key) để truy xuất/xóa sau.</summary>
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default);

    /// <summary>Mở luồng đọc theo khóa; null nếu không tồn tại.</summary>
    Task<Stream?> OpenAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
