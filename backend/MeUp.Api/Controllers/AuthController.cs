using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _auth.RegisterAsync(request);
        return result.Succeeded ? Ok(result.Response) : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (!result.Succeeded) return Unauthorized(new { error = result.Error });

        return Ok(new LoginResponse(result.RequiresTwoFactor, result.TwoFactorToken, result.Response));
    }

    [HttpPost("login/2fa")]
    public async Task<IActionResult> LoginTwoFactor(LoginTwoFactorRequest request)
    {
        var result = await _auth.LoginTwoFactorAsync(request);
        if (!result.Succeeded) return Unauthorized(new { error = result.Error });

        return Ok(new LoginResponse(false, null, result.Response));
    }

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleLoginRequest request)
    {
        var result = await _auth.GoogleLoginAsync(request);
        if (!result.Succeeded) return Unauthorized(new { error = result.Error });

        return Ok(new LoginResponse(false, null, result.Response));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken);
        return result.Succeeded ? Ok(result.Response) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        await _auth.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    // --- C2: Vòng đời tài khoản ---

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _auth.ForgotPasswordAsync(request);
        return Ok(new { message = "Nếu email tồn tại, chúng tôi đã gửi liên kết đặt lại mật khẩu." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var result = await _auth.ResetPasswordAsync(request);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
        => await _auth.ConfirmEmailAsync(request)
            ? Ok(new { message = "Email đã được xác thực." })
            : BadRequest(new { error = "Xác thực email thất bại hoặc liên kết không hợp lệ." });

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
    {
        await _auth.ResendConfirmationAsync(request);
        return Ok(new { message = "Nếu email chưa xác thực, chúng tôi đã gửi lại liên kết." });
    }
}
