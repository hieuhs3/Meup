using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

/// <summary>Hồ sơ người dùng trả ra client (không chứa thông tin nhạy cảm).</summary>
public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    DateTime CreatedAt,
    string? PhoneNumber = null,
    DateOnly? DateOfBirth = null,
    string? Gender = null,
    string? Bio = null,
    string? AvatarUrl = null,
    string? TimeZone = null,
    string? Locale = null,
    bool TwoFactorEnabled = false,
    bool HasPassword = true,
    IReadOnlyList<string>? AuthProviders = null);

/// <summary>Cập nhật hồ sơ mở rộng. Chỉ DisplayName bắt buộc; còn lại tùy chọn.</summary>
public record UpdateProfileRequest(
    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [MaxLength(100)]
    string DisplayName,

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    string? PhoneNumber,

    DateOnly? DateOfBirth,

    [RegularExpression("male|female|other", ErrorMessage = "Giới tính không hợp lệ.")]
    string? Gender,

    [MaxLength(500, ErrorMessage = "Tiểu sử tối đa 500 ký tự.")]
    string? Bio,

    [MaxLength(64)]
    string? TimeZone,

    [MaxLength(10)]
    string? Locale);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
    string CurrentPassword,

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    string NewPassword);

public record ChangeEmailRequest(
    [Required(ErrorMessage = "Email mới là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    string NewEmail,

    /// <summary>Bắt buộc với tài khoản có mật khẩu; bỏ trống với tài khoản chỉ-Google.</summary>
    string? CurrentPassword);

public record DeleteAccountRequest(
    /// <summary>Bắt buộc với tài khoản có mật khẩu; bỏ trống với tài khoản chỉ-Google.</summary>
    string? CurrentPassword);

/// <summary>Thông tin user cho trang quản trị.</summary>
public record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsLocked,
    DateTime CreatedAt);

// --- 2FA ---

/// <summary>Kết quả setup 2FA: khóa chia sẻ (hiển thị) + URI để quét QR.</summary>
public record TwoFactorSetupDto(string SharedKey, string AuthenticatorUri);

public record EnableTwoFactorRequest(
    [Required(ErrorMessage = "Mã xác thực là bắt buộc.")]
    string Code);

/// <summary>Trả về sau khi bật 2FA: mã khôi phục, chỉ hiện một lần.</summary>
public record EnableTwoFactorResultDto(IReadOnlyList<string> RecoveryCodes);

public record DisableTwoFactorRequest(
    /// <summary>Bắt buộc với tài khoản có mật khẩu.</summary>
    string? CurrentPassword);
