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

    /// <summary>Tâm trạng (tùy chọn): excellent|good|normal|bad|terrible.</summary>
    public string? Mood { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Tâm trạng của một bài nhật ký + điểm số (để vẽ xu hướng).</summary>
public static class Mood
{
    public const string Excellent = "excellent";
    public const string Good = "good";
    public const string Normal = "normal";
    public const string Bad = "bad";
    public const string Terrible = "terrible";

    public static bool IsValid(string? v) => v is Excellent or Good or Normal or Bad or Terrible;
    public static string? Normalize(string? v) => IsValid(v) ? v : null;
    /// <summary>Điểm 1–5 (terrible=1 … excellent=5) cho biểu đồ xu hướng.</summary>
    public static int Score(string v) => v switch
    {
        Excellent => 5, Good => 4, Normal => 3, Bad => 2, Terrible => 1, _ => 0,
    };
}
