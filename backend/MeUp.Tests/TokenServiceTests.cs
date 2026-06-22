using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MeUp.Api.Entities;
using MeUp.Api.Options;
using MeUp.Api.Services;
using Microsoft.Extensions.Options;

namespace MeUp.Tests;

public class TokenServiceTests
{
    private static TokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Issuer = "MeUp.Api",
        Audience = "MeUp.Client",
        Key = "unit_test_secret_key_at_least_32_characters_long_000",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    }));

    [Fact]
    public void HashToken_LaTatDinh_CungInputCungHash()
    {
        var svc = CreateService();
        Assert.Equal(svc.HashToken("abc"), svc.HashToken("abc"));
    }

    [Fact]
    public void HashToken_KhacInput_KhacHash()
    {
        var svc = CreateService();
        Assert.NotEqual(svc.HashToken("abc"), svc.HashToken("xyz"));
    }

    [Fact]
    public void GenerateRefreshToken_KhongRong_VaDuyNhat()
    {
        var svc = CreateService();
        var a = svc.GenerateRefreshToken();
        var b = svc.GenerateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(a));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void CreateAccessToken_ChuaDungClaim_VaHetHanTrongTuongLai()
    {
        var svc = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A" };

        var (token, expiresAt) = svc.CreateAccessToken(user, Roles.User);

        Assert.True(expiresAt > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(Roles.User, jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void TwoFactorToken_KhứHồi_TraVeDungUserId()
    {
        var svc = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A" };

        var token = svc.CreateTwoFactorToken(user);

        Assert.Equal(user.Id, svc.ValidateTwoFactorToken(token));
    }

    [Fact]
    public void ValidateTwoFactorToken_TokenBay_TraVeNull()
    {
        var svc = CreateService();
        Assert.Null(svc.ValidateTwoFactorToken("khong-phai-jwt-hop-le"));
    }

    [Fact]
    public void ValidateTwoFactorToken_TuChoiAccessToken_VìKhongDungPurpose()
    {
        var svc = CreateService();
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A" };

        // Access token không có claim purpose=twofa → không được chấp nhận làm token 2FA.
        var (accessToken, _) = svc.CreateAccessToken(user, Roles.User);

        Assert.Null(svc.ValidateTwoFactorToken(accessToken));
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_ChuaThuHoi_ChuaHetHan_ThiActive()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = new RefreshToken { ExpiresAt = now.AddDays(1) };
        Assert.True(token.IsActive(now));
    }

    [Fact]
    public void IsActive_HetHan_ThiKhongActive()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = new RefreshToken { ExpiresAt = now.AddDays(-1) };
        Assert.False(token.IsActive(now));
    }

    [Fact]
    public void IsActive_DaThuHoi_ThiKhongActive()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = new RefreshToken { ExpiresAt = now.AddDays(1), RevokedAt = now.AddHours(-1) };
        Assert.False(token.IsActive(now));
    }
}
