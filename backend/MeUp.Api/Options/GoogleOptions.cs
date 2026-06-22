namespace MeUp.Api.Options;

/// <summary>Cấu hình đăng nhập Google, đọc từ section "Authentication:Google".</summary>
public class GoogleOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>OAuth 2.0 Client ID (audience hợp lệ của ID token). Để trống = tắt đăng nhập Google.</summary>
    public string ClientId { get; set; } = string.Empty;
}
