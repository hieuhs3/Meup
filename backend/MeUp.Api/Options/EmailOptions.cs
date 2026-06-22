namespace MeUp.Api.Options;

/// <summary>Cấu hình email, đọc từ section "Email". Để trống Host = dùng chế độ dev (ghi log/file).</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;

    public string FromEmail { get; set; } = "no-reply@meup.local";
    public string FromName { get; set; } = "MeUp";

    /// <summary>URL gốc của web app, để dựng link trong email.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:4200";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
