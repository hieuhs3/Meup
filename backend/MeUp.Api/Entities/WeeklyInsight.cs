namespace MeUp.Api.Entities;

/// <summary>
/// Tổng kết tuần do AI tạo, lưu lại theo (UserId, khoảng tuần) để không phải gọi lại Claude.
/// Cô lập dữ liệu theo UserId. Mỗi người chỉ có 1 bản ghi cho mỗi khoảng tuần.
/// </summary>
public class WeeklyInsight
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Ngày đầu tuần (to - 6 ngày).</summary>
    public DateOnly WeekFrom { get; set; }

    /// <summary>Ngày cuối tuần (ngày yêu cầu).</summary>
    public DateOnly WeekTo { get; set; }

    /// <summary>Nội dung tổng kết do Claude sinh ra.</summary>
    public string Summary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
