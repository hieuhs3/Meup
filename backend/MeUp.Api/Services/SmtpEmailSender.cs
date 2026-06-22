using System.Net;
using System.Net.Mail;
using MeUp.Api.Options;
using Microsoft.Extensions.Options;

namespace MeUp.Api.Services;

/// <summary>Gửi email thật qua SMTP (System.Net.Mail). Dùng khi cấu hình Email:Host.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;

    public SmtpEmailSender(IOptions<EmailOptions> opt) => _opt = opt.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_opt.FromEmail, _opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_opt.Host!, _opt.Port)
        {
            EnableSsl = _opt.UseSsl,
            Credentials = string.IsNullOrEmpty(_opt.User)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_opt.User, _opt.Password),
        };
        await client.SendMailAsync(message, ct);
    }
}
