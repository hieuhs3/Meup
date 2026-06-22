namespace MeUp.Api.Options;

/// <summary>Cấu hình AI (Claude), đọc từ section "Ai". Để trống ApiKey = tắt tính năng AI.</summary>
public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>ANTHROPIC_API_KEY. Đặt qua user-secrets / biến môi trường ở production, KHÔNG commit.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
