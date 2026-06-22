using MeUp.Api.Services;

namespace MeUp.Tests.Integration;

/// <summary>
/// Thay cho việc gọi Google thật trong test. Quy ước idToken:
///   "valid:{email}"       → token hợp lệ, email đã xác thực
///   "unverified:{email}"  → token hợp lệ nhưng email chưa xác thực
///   còn lại               → null (token sai)
/// </summary>
public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        var parts = idToken.Split(':', 2);
        if (parts.Length == 2 && parts[0] is "valid" or "unverified")
        {
            var info = new GoogleUserInfo(
                Subject: "g-" + parts[1],
                Email: parts[1],
                EmailVerified: parts[0] == "valid",
                Name: "Google User",
                Picture: null);
            return Task.FromResult<GoogleUserInfo?>(info);
        }
        return Task.FromResult<GoogleUserInfo?>(null);
    }
}
