using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record RegisterRequest(
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    string Email,

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    string Password,

    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [MaxLength(100)]
    string DisplayName);

public record LoginRequest(
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    string Email,

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    string Password);

public record RefreshRequest(
    [Required] string RefreshToken);

public record LogoutRequest(
    [Required] string RefreshToken);

/// <summary>Đăng nhập bằng Google ID token (lấy từ Google Identity Services ở client).</summary>
public record GoogleLoginRequest(
    [Required(ErrorMessage = "Thiếu Google ID token.")]
    string IdToken);

/// <summary>Bước 2 của đăng nhập 2FA: token thử thách + mã TOTP hoặc mã khôi phục.</summary>
public record LoginTwoFactorRequest(
    [Required] string TwoFactorToken,
    [Required(ErrorMessage = "Mã xác thực là bắt buộc.")] string Code);

// --- C2: Vòng đời tài khoản ---

public record ForgotPasswordRequest(
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")] string Email);

public record ResetPasswordRequest(
    [Required] string Email,
    [Required] string Token,
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")] string NewPassword);

public record ConfirmEmailRequest(
    [Required] string Email,
    [Required] string Token);

public record ResendConfirmationRequest(
    [Required][EmailAddress] string Email);

/// <summary>Kết quả trả về khi đăng nhập / làm mới token.</summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserProfileDto User);

/// <summary>
/// Bao bọc kết quả login: nếu tài khoản bật 2FA thì <see cref="RequiresTwoFactor"/> = true
/// kèm <see cref="TwoFactorToken"/> (token tạm); ngược lại trả thẳng <see cref="Auth"/>.
/// </summary>
public record LoginResponse(
    bool RequiresTwoFactor,
    string? TwoFactorToken,
    AuthResponse? Auth);
