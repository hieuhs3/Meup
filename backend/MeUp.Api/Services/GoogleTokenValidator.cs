using System.Text.Json;
using MeUp.Api.Options;
using Microsoft.Extensions.Options;

namespace MeUp.Api.Services;

/// <summary>
/// Xác thực Google ID token qua endpoint tokeninfo của Google (không cần thêm thư viện).
/// Google ký & kiểm tra token; ta chỉ cần đối chiếu audience (ClientId) và hạn dùng.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly HttpClient _http;
    private readonly GoogleOptions _options;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(HttpClient http, IOptions<GoogleOptions> options, ILogger<GoogleTokenValidator> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            _logger.LogWarning("Đăng nhập Google chưa được cấu hình (thiếu ClientId).");
            return null;
        }

        using var resp = await _http.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}", ct);
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;

        var aud = Str(root, "aud");
        if (aud != _options.ClientId) return null; // token cấp cho ứng dụng khác

        // Còn hạn? exp là epoch seconds dạng chuỗi.
        if (long.TryParse(Str(root, "exp"), out var exp)
            && DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow)
            return null;

        var email = Str(root, "email");
        if (string.IsNullOrEmpty(email)) return null;

        var emailVerified = string.Equals(Str(root, "email_verified"), "true", StringComparison.OrdinalIgnoreCase);

        return new GoogleUserInfo(
            Subject: Str(root, "sub") ?? "",
            Email: email,
            EmailVerified: emailVerified,
            Name: Str(root, "name"),
            Picture: Str(root, "picture"));
    }

    private static string? Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) ? v.GetString() : null;
}
