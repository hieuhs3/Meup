using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using MeUp.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeUp.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IGoogleTokenValidator _google;
    private readonly IEmailSender _email;
    private readonly JwtOptions _jwt;
    private readonly EmailOptions _emailOpt;

    public AuthService(
        UserManager<ApplicationUser> users,
        AppDbContext db,
        ITokenService tokens,
        IGoogleTokenValidator google,
        IEmailSender email,
        IOptions<JwtOptions> jwt,
        IOptions<EmailOptions> emailOpt)
    {
        _users = users;
        _db = db;
        _tokens = tokens;
        _google = google;
        _email = email;
        _jwt = jwt.Value;
        _emailOpt = emailOpt.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.FindByEmailAsync(email) is not null)
            return AuthResult.Fail("Email đã được sử dụng.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return AuthResult.Fail(string.Join(" ", created.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, Roles.User);

        await SendConfirmationEmailAsync(user); // xác thực email (không chặn đăng nhập)

        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(email);

        // Thông báo chung để không lộ email nào tồn tại.
        if (user is null)
            return AuthResult.Fail("Email hoặc mật khẩu không đúng.");

        // Khóa tạm do đăng nhập sai nhiều lần.
        if (await _users.IsLockedOutAsync(user))
            return AuthResult.Fail("Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau.");

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user); // tăng đếm; tự khóa khi vượt ngưỡng
            return AuthResult.Fail("Email hoặc mật khẩu không đúng.");
        }

        if (user.IsLocked)
            return AuthResult.Fail("Tài khoản đã bị khóa.");

        await _users.ResetAccessFailedCountAsync(user);

        // Bật 2FA → chưa cấp token; trả thử thách để client nhập mã ở bước 2.
        if (user.TwoFactorEnabled)
            return AuthResult.TwoFactor(_tokens.CreateTwoFactorToken(user));

        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> LoginTwoFactorAsync(LoginTwoFactorRequest request)
    {
        var userId = _tokens.ValidateTwoFactorToken(request.TwoFactorToken);
        if (userId is null)
            return AuthResult.Fail("Phiên xác thực 2 lớp đã hết hạn. Vui lòng đăng nhập lại.");

        var user = await _users.FindByIdAsync(userId.Value.ToString());
        if (user is null) return AuthResult.Fail("Tài khoản không tồn tại.");
        if (user.IsLocked) return AuthResult.Fail("Tài khoản đã bị khóa.");

        // Mã TOTP: chỉ chữ số (bỏ khoảng trắng/gạch). Mã khôi phục giữ nguyên dấu gạch.
        var totpCode = request.Code.Replace(" ", "").Replace("-", "");
        var recoveryCode = request.Code.Trim();

        var validTotp = await _users.VerifyTwoFactorTokenAsync(
            user, _users.Options.Tokens.AuthenticatorTokenProvider, totpCode);

        if (!validTotp)
        {
            var redeem = await _users.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode);
            if (!redeem.Succeeded)
                return AuthResult.Fail("Mã xác thực không đúng.");
        }

        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> GoogleLoginAsync(GoogleLoginRequest request)
    {
        var info = await _google.ValidateAsync(request.IdToken);
        if (info is null || !info.EmailVerified)
            return AuthResult.Fail("Đăng nhập Google thất bại hoặc email chưa được xác thực.");

        var email = info.Email.Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = string.IsNullOrWhiteSpace(info.Name) ? email : info.Name!,
                AvatarUrl = info.Picture,
            };
            var created = await _users.CreateAsync(user); // không mật khẩu — chỉ đăng nhập Google
            if (!created.Succeeded)
                return AuthResult.Fail(string.Join(" ", created.Errors.Select(e => e.Description)));

            await _users.AddToRoleAsync(user, Roles.User);
        }

        if (user.IsLocked)
            return AuthResult.Fail("Tài khoản đã bị khóa.");

        // Liên kết external login (nếu chưa có) để lần sau nhận diện.
        var logins = await _users.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == info.Subject))
            await _users.AddLoginAsync(user, new UserLoginInfo("Google", info.Subject, "Google"));

        return AuthResult.Ok(await IssueTokensAsync(user));
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var hash = _tokens.HashToken(refreshToken);
        var now = DateTime.UtcNow;

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || !stored.IsActive(now))
            return AuthResult.Fail("Refresh token không hợp lệ hoặc đã hết hạn.");

        if (stored.User.IsLocked)
            return AuthResult.Fail("Tài khoản đã bị khóa.");

        // Xoay vòng: thu hồi token cũ rồi cấp token mới.
        stored.RevokedAt = now;
        var response = await IssueTokensAsync(stored.User);
        return AuthResult.Ok(response);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = _tokens.HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>Tạo access token + refresh token mới và lưu hash refresh token.</summary>
    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user)
    {
        var role = (await _users.GetRolesAsync(user)).FirstOrDefault() ?? Roles.User;
        var (accessToken, accessExpiresAt) = _tokens.CreateAccessToken(user, role);

        var rawRefresh = _tokens.GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokens.HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });
        await _db.SaveChangesAsync();

        var profile = await UserProfileFactory.BuildAsync(_users, user);
        return new AuthResponse(accessToken, rawRefresh, accessExpiresAt, profile);
    }

    // --- C2: Vòng đời tài khoản ---

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null) return; // không lộ email nào tồn tại

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var link = $"{_emailOpt.WebBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
        await _email.SendAsync(user.Email!, "Đặt lại mật khẩu MeUp",
            $"<p>Xin chào {user.DisplayName},</p>" +
            $"<p>Bấm vào liên kết sau để đặt lại mật khẩu (hết hạn sau một thời gian ngắn):</p>" +
            $"<p><a href=\"{link}\">Đặt lại mật khẩu</a></p>" +
            "<p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>");
    }

    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null) return AuthResult.Fail("Yêu cầu không hợp lệ.");

        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return AuthResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _users.ResetAccessFailedCountAsync(user);
        return AuthResult.Ok(null!);
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null) return false;
        var result = await _users.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded;
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || user.EmailConfirmed) return;
        await SendConfirmationEmailAsync(user);
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{_emailOpt.WebBaseUrl}/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
        await _email.SendAsync(user.Email!, "Xác thực email MeUp",
            $"<p>Xin chào {user.DisplayName},</p>" +
            $"<p>Bấm vào liên kết sau để xác thực email của bạn:</p>" +
            $"<p><a href=\"{link}\">Xác thực email</a></p>");
    }
}
