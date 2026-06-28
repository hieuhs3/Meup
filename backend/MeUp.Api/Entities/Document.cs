namespace MeUp.Api.Entities;

/// <summary>Tài liệu cá nhân do người dùng tải lên (cô lập theo UserId). File lưu qua IFileStorage.</summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Phân loại: cv|certificate|contract|invoice|personal|other.</summary>
    public string Category { get; set; } = DocumentCategory.Other;

    /// <summary>Tên file gốc (hiển thị cho người dùng).</summary>
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Kích thước byte.</summary>
    public long Size { get; set; }

    /// <summary>Khóa lưu trữ trong IFileStorage (không lộ ra ngoài).</summary>
    public string StorageKey { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Phân loại tài liệu hợp lệ.</summary>
public static class DocumentCategory
{
    public const string Cv = "cv";
    public const string Certificate = "certificate";
    public const string Contract = "contract";
    public const string Invoice = "invoice";
    public const string Personal = "personal";
    public const string Other = "other";

    public static bool IsValid(string? v) => v is Cv or Certificate or Contract or Invoice or Personal or Other;
    public static string Normalize(string? v) => IsValid(v) ? v! : Other;
}
