namespace MeUp.Api.Entities;

/// <summary>Thông báo in-app cho người dùng (cô lập theo UserId).</summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Type { get; set; } = "info"; // info | reminder | …
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Đường dẫn nội bộ để mở khi bấm, vd "/app/today".</summary>
    public string? Link { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Khóa chống trùng (vd "daily:2026-06-19"); null = không chống trùng.</summary>
    public string? DedupKey { get; set; }

    public ApplicationUser? User { get; set; }
}
