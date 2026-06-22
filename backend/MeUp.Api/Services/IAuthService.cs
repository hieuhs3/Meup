using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

/// <summary>
/// Kết quả thao tác auth: thành công kèm token, hoặc lỗi kèm thông báo tiếng Việt,
/// hoặc yêu cầu bước 2FA (kèm token thử thách).
/// </summary>
public record AuthResult(
    bool Succeeded,
    string? Error = null,
    AuthResponse? Response = null,
    bool RequiresTwoFactor = false,
    string? TwoFactorToken = null)
{
    public static AuthResult Ok(AuthResponse response) => new(true, null, response);
    public static AuthResult Fail(string error) => new(false, error, null);
    public static AuthResult TwoFactor(string twoFactorToken) =>
        new(true, null, null, RequiresTwoFactor: true, TwoFactorToken: twoFactorToken);
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> LoginTwoFactorAsync(LoginTwoFactorRequest request);
    Task<AuthResult> GoogleLoginAsync(GoogleLoginRequest request);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);

    // C2 — vòng đời tài khoản
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ResendConfirmationAsync(ResendConfirmationRequest request);
}
