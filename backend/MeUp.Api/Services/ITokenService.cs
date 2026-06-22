using System.Security.Claims;
using MeUp.Api.Entities;

namespace MeUp.Api.Services;

public interface ITokenService
{
    /// <summary>Tạo access token (JWT) cho user kèm vai trò. Trả token và thời điểm hết hạn.</summary>
    (string token, DateTime expiresAt) CreateAccessToken(ApplicationUser user, string role);

    /// <summary>Sinh refresh token thô (ngẫu nhiên, an toàn) để gửi cho client.</summary>
    string GenerateRefreshToken();

    /// <summary>Băm SHA-256 token thô để lưu vào DB (so khớp khi refresh).</summary>
    string HashToken(string rawToken);

    /// <summary>Tạo token thử thách 2FA (JWT hạn ngắn, claim purpose=twofa). Không dùng được như access token.</summary>
    string CreateTwoFactorToken(ApplicationUser user);

    /// <summary>Xác thực token thử thách 2FA; trả Id user nếu hợp lệ, ngược lại null.</summary>
    Guid? ValidateTwoFactorToken(string token);
}
