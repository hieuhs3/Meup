namespace MeUp.Api.Services;

/// <summary>Thông tin lấy được từ Google ID token sau khi xác thực.</summary>
public record GoogleUserInfo(string Subject, string Email, bool EmailVerified, string? Name, string? Picture);

/// <summary>Xác thực Google ID token. Tách interface để test mock được.</summary>
public interface IGoogleTokenValidator
{
    /// <summary>Trả thông tin user nếu token hợp lệ (đúng audience, còn hạn); null nếu không.</summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default);
}
