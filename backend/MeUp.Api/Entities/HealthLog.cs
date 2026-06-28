namespace MeUp.Api.Entities;

/// <summary>Nhật ký sức khỏe theo ngày của một người dùng (tối đa 1 bản ghi/ngày, cô lập theo UserId).</summary>
public class HealthLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Cân nặng (kg).</summary>
    public decimal? Weight { get; set; }

    /// <summary>Chiều cao (cm) — dùng tính BMI.</summary>
    public decimal? HeightCm { get; set; }

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

/// <summary>Một buổi hoạt động thể chất theo ngày (cô lập theo UserId).</summary>
public class Activity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Loại: running|walking|gym|swimming|cycling|other.</summary>
    public string Type { get; set; } = ActivityType.Other;

    /// <summary>Thời lượng (phút).</summary>
    public int DurationMin { get; set; }

    /// <summary>Calo tiêu hao (tùy chọn).</summary>
    public int? Calories { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Loại hoạt động hợp lệ.</summary>
public static class ActivityType
{
    public const string Running = "running";
    public const string Walking = "walking";
    public const string Gym = "gym";
    public const string Swimming = "swimming";
    public const string Cycling = "cycling";
    public const string Other = "other";

    public static bool IsValid(string? v) => v is Running or Walking or Gym or Swimming or Cycling or Other;
    public static string Normalize(string? v) => IsValid(v) ? v! : Other;
}
