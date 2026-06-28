namespace MeUp.Api.Entities;

/// <summary>
/// Ghi chú (cô lập theo UserId). Ban đầu là ghi chú nhanh (chỉ Content); G6 nâng thành
/// kiến thức có tiêu đề, nhóm, thẻ và liên kết [[tiêu đề]] (backlinks kiểu Obsidian).
/// </summary>
public class Note
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Tiêu đề (tùy chọn) — dùng làm đích cho liên kết [[tiêu đề]].</summary>
    public string? Title { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>Nhóm/thư mục (tùy chọn).</summary>
    public string? Category { get; set; }

    /// <summary>Thẻ (lưu mảng text[] trong Postgres).</summary>
    public List<string> Tags { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
