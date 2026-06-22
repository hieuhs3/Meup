namespace MeUp.Api.Entities;

/// <summary>Một bài nhật ký của người dùng (cô lập theo UserId). Nội dung là HTML có định dạng.</summary>
public class JournalEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    public string? Title { get; set; }

    /// <summary>Nội dung HTML (đã soạn bằng rich editor). Sanitize khi hiển thị.</summary>
    public string ContentHtml { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
