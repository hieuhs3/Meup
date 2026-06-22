namespace MeUp.Api.Entities;

/// <summary>Sự kiện trong lịch trình của người dùng (cô lập theo UserId).</summary>
public class CalendarEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Giờ bắt đầu/kết thúc (tùy chọn — sự kiện cả ngày để trống).</summary>
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
