using System.Text;
using System.Text.Encodings.Web;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private const long MaxAvatarBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly Dictionary<string, string> AllowedAvatarTypes = new()
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };

    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;
    private readonly IDailyReportService _dailyReport;

    public UsersController(UserManager<ApplicationUser> users, IWebHostEnvironment env, IDailyReportService dailyReport)
    {
        _users = users;
        _env = env;
        _dailyReport = dailyReport;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();
        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    [HttpPut("me/daily-report")]
    public async Task<IActionResult> SetDailyReport(SetDailyReportRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        user.DailyReportEnabled = request.Enabled;
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(IdentityError(result));

        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    /// <summary>Gửi thử báo cáo cuối ngày ngay (cho hôm nay). Trả {sent} — false nếu hôm nay đã gửi.</summary>
    [HttpPost("me/daily-report/send-now")]
    public async Task<IActionResult> SendDailyReportNow()
    {
        var sent = await _dailyReport.SendForUserAsync(User.GetUserId(), DateOnly.FromDateTime(DateTime.UtcNow));
        return Ok(new { sent });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        user.DisplayName = request.DisplayName.Trim();
        user.PhoneNumber = NullIfBlank(request.PhoneNumber);
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = NullIfBlank(request.Gender);
        user.Bio = NullIfBlank(request.Bio);
        user.TimeZone = NullIfBlank(request.TimeZone);
        user.Locale = NullIfBlank(request.Locale);

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(IdentityError(result));

        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return BadRequest(IdentityError(result));

        return NoContent();
    }

    [HttpPost("me/change-email")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var (ok, error) = await VerifyPasswordIfRequiredAsync(user, request.CurrentPassword);
        if (!ok) return BadRequest(new { error });

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();
        var existing = await _users.FindByEmailAsync(newEmail);
        if (existing is not null && existing.Id != user.Id)
            return BadRequest(new { error = "Email đã được sử dụng." });

        var token = await _users.GenerateChangeEmailTokenAsync(user, newEmail);
        var changed = await _users.ChangeEmailAsync(user, newEmail, token);
        if (!changed.Succeeded) return BadRequest(IdentityError(changed));

        // Dùng email làm username để đăng nhập.
        await _users.SetUserNameAsync(user, newEmail);

        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes + 512 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Chưa chọn ảnh." });
        if (file.Length > MaxAvatarBytes)
            return BadRequest(new { error = "Ảnh vượt quá 2 MB." });
        if (!AllowedAvatarTypes.TryGetValue(file.ContentType, out var ext))
            return BadRequest(new { error = "Chỉ chấp nhận ảnh PNG, JPEG hoặc WebP." });

        var dir = Path.Combine(WebRoot(), "uploads", "avatars");
        Directory.CreateDirectory(dir);

        // Xóa ảnh cũ (có thể khác đuôi) để không sót file rác.
        foreach (var old in Directory.EnumerateFiles(dir, user.Id + ".*"))
            System.IO.File.Delete(old);

        var fileName = $"{user.Id}{ext}";
        await using (var stream = System.IO.File.Create(Path.Combine(dir, fileName)))
            await file.CopyToAsync(stream);

        user.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _users.UpdateAsync(user);

        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    [HttpDelete("me/avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var dir = Path.Combine(WebRoot(), "uploads", "avatars");
        if (Directory.Exists(dir))
            foreach (var old in Directory.EnumerateFiles(dir, user.Id + ".*"))
                System.IO.File.Delete(old);

        user.AvatarUrl = null;
        await _users.UpdateAsync(user);

        return Ok(await UserProfileFactory.BuildAsync(_users, user));
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var (ok, error) = await VerifyPasswordIfRequiredAsync(user, request.CurrentPassword);
        if (!ok) return BadRequest(new { error });

        // Không cho xóa admin cuối cùng (giữ luôn có người quản trị).
        if (await _users.IsInRoleAsync(user, Roles.Admin))
        {
            var admins = await _users.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
                return BadRequest(new { error = "Không thể xóa tài khoản quản trị viên cuối cùng." });
        }

        // Dọn avatar trên đĩa.
        var dir = Path.Combine(WebRoot(), "uploads", "avatars");
        if (Directory.Exists(dir))
            foreach (var old in Directory.EnumerateFiles(dir, user.Id + ".*"))
                System.IO.File.Delete(old);

        var result = await _users.DeleteAsync(user); // refresh token & login cascade theo FK
        if (!result.Succeeded) return BadRequest(IdentityError(result));

        return NoContent();
    }

    // --- 2FA (TOTP) ---

    [HttpPost("me/2fa/setup")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var key = await _users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _users.ResetAuthenticatorKeyAsync(user);
            key = await _users.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? "user";
        var uri = $"otpauth://totp/{UrlEncoder.Default.Encode("MeUp")}:{UrlEncoder.Default.Encode(email)}"
                  + $"?secret={key}&issuer={UrlEncoder.Default.Encode("MeUp")}&digits=6";

        return Ok(new TwoFactorSetupDto(FormatKey(key!), uri));
    }

    [HttpPost("me/2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var code = request.Code.Replace(" ", "").Replace("-", "");
        var valid = await _users.VerifyTwoFactorTokenAsync(
            user, _users.Options.Tokens.AuthenticatorTokenProvider, code);
        if (!valid)
            return BadRequest(new { error = "Mã xác thực không đúng." });

        await _users.SetTwoFactorEnabledAsync(user, true);
        var codes = await _users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Ok(new EnableTwoFactorResultDto(codes?.ToList() ?? []));
    }

    [HttpPost("me/2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request)
    {
        var user = await _users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null) return NotFound();

        var (ok, error) = await VerifyPasswordIfRequiredAsync(user, request.CurrentPassword);
        if (!ok) return BadRequest(new { error });

        await _users.SetTwoFactorEnabledAsync(user, false);
        await _users.ResetAuthenticatorKeyAsync(user);

        return NoContent();
    }

    // --- Tiện ích ---

    private string WebRoot() => _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static object IdentityError(IdentityResult result) =>
        new { error = string.Join(" ", result.Errors.Select(e => e.Description)) };

    /// <summary>
    /// Với tài khoản có mật khẩu: bắt buộc nhập đúng mật khẩu hiện tại.
    /// Với tài khoản chỉ-Google (chưa có mật khẩu): bỏ qua. Trả (ok, thông báo lỗi).
    /// </summary>
    private async Task<(bool Ok, string Error)> VerifyPasswordIfRequiredAsync(ApplicationUser user, string? password)
    {
        if (!await _users.HasPasswordAsync(user)) return (true, "");

        if (string.IsNullOrEmpty(password) || !await _users.CheckPasswordAsync(user, password))
            return (false, "Mật khẩu hiện tại không đúng.");

        return (true, "");
    }

    /// <summary>Chia khóa thành nhóm 4 ký tự cho dễ nhập tay.</summary>
    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
            sb.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        return sb.ToString().Trim().ToLowerInvariant();
    }
}
