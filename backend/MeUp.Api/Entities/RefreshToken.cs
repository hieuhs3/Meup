namespace MeUp.Api.Entities;

/// <summary>
/// Refresh token để cấp lại access token mà không cần đăng nhập lại.
/// Chỉ lưu HASH của token (không lưu token thô) để nếu lộ DB cũng không dùng được.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>SHA-256 hash của token thô đã phát cho client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Token còn dùng được: chưa thu hồi và chưa hết hạn.</summary>
    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}
