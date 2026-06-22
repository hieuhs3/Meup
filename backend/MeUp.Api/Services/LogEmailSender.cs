using Microsoft.Extensions.Hosting;

namespace MeUp.Api.Services;

/// <summary>
/// Chế độ dev: không gửi email thật, mà ghi ra log + file trong thư mục sent-emails/
/// để lấy link đặt lại mật khẩu / xác thực khi chưa cấu hình SMTP.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    private readonly string _dir;

    public LogEmailSender(ILogger<LogEmailSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _dir = Path.Combine(env.ContentRootPath, "sent-emails");
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL (dev) → {To} | {Subject}\n{Body}", toEmail, subject, htmlBody);
        try
        {
            Directory.CreateDirectory(_dir);
            var file = Path.Combine(_dir, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(file, $"To: {toEmail}\nSubject: {subject}\n\n{htmlBody}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không ghi được file email dev.");
        }
    }
}
