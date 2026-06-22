namespace MeUp.Api.Entities;

/// <summary>Nhật ký sức khỏe theo ngày của một người dùng (tối đa 1 bản ghi/ngày, cô lập theo UserId).</summary>
public class HealthLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Cân nặng (kg).</summary>
    public decimal? Weight { get; set; }

    /// <summary>Giờ ngủ.</summary>
    public decimal? SleepHours { get; set; }

    /// <summary>Lượng nước (ml).</summary>
    public int? WaterMl { get; set; }

    /// <summary>Thời gian tập (phút).</summary>
    public int? WorkoutMinutes { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
