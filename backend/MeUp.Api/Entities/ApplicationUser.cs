using Microsoft.AspNetCore.Identity;

namespace MeUp.Api.Entities;

/// <summary>
/// Người dùng của hệ thống. Mở rộng IdentityUser với khóa kiểu Guid và vài trường hồ sơ.
/// Mật khẩu được Identity băm và lưu ở PasswordHash (không bao giờ lưu dạng thô).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Tài khoản bị admin khóa thì không đăng nhập được.</summary>
    public bool IsLocked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Hồ sơ mở rộng (F0E). Tất cả tùy chọn, không phá dữ liệu cũ. ---
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>"male" | "female" | "other".</summary>
    public string? Gender { get; set; }

    public string? Bio { get; set; }

    /// <summary>Đường dẫn tương đối tới ảnh đại diện, vd "/uploads/avatars/{id}.png".</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>IANA time zone, vd "Asia/Ho_Chi_Minh".</summary>
    public string? TimeZone { get; set; }

    /// <summary>Mã ngôn ngữ, vd "vi".</summary>
    public string? Locale { get; set; }

    /// <summary>Bật nhận email báo cáo tổng kết cuối ngày (gửi 21:00 theo múi giờ của user). Mặc định tắt.</summary>
    public bool DailyReportEnabled { get; set; }

    /// <summary>
    /// API key Claude của riêng người dùng (BYO), đã MÃ HÓA bằng Data Protection.
    /// Không bao giờ lưu/trả về dạng thô. Null = dùng key server (nếu có) hoặc tắt AI.
    /// </summary>
    public string? EncryptedAiApiKey { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
