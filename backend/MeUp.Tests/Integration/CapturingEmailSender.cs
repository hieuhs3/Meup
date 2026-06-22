using System.Collections.Concurrent;
using MeUp.Api.Services;

namespace MeUp.Tests.Integration;

/// <summary>Thay IEmailSender trong test: ghi lại email cuối cùng theo người nhận để kiểm tra token.</summary>
public class CapturingEmailSender : IEmailSender
{
    public static readonly ConcurrentDictionary<string, (string Subject, string Body)> Sent = new();

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        Sent[toEmail.ToLowerInvariant()] = (subject, htmlBody);
        return Task.CompletedTask;
    }
}
