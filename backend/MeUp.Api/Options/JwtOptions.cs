namespace MeUp.Api.Options;

/// <summary>Cấu hình JWT, đọc từ section "Jwt" trong appsettings.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Khóa bí mật ký token. Phải đủ dài (>= 32 ký tự). KHÔNG commit khóa thật.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
