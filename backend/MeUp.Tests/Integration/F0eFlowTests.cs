using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>
/// Integration test cho F0E: chạy API thật (TestServer + Postgres test DB), gọi qua HTTP.
/// Yêu cầu Postgres ở localhost:5433 (docker compose up -d).
/// </summary>
public class F0eFlowTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public F0eFlowTests(MeUpWebAppFactory factory) => _factory = factory;

    // --- Tiện ích ---

    private static string NewEmail() => $"it_{Guid.NewGuid():N}@test.local";
    private const string Pwd = "Passw0rd!";

    private HttpClient Client() => _factory.CreateClient();

    private HttpClient Authed(string token)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<(string email, string token)> RegisterAsync(string? email = null)
    {
        email ??= NewEmail();
        var resp = await Client().PostAsJsonAsync("/api/auth/register",
            new { email, password = Pwd, displayName = "IT User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (email, body.GetProperty("accessToken").GetString()!);
    }

    private async Task<JsonElement> LoginAsync(string email, string password)
    {
        var resp = await Client().PostAsJsonAsync("/api/auth/login", new { email, password });
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }

    // --- Hồ sơ ---

    [Fact]
    public async Task Login_TraVe_LoginResponse_KhongYeuCau2FA()
    {
        var (email, _) = await RegisterAsync();

        var login = await LoginAsync(email, Pwd);

        Assert.False(login.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.Equal(JsonValueKind.Null, login.GetProperty("twoFactorToken").ValueKind);
        var user = login.GetProperty("auth").GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.True(user.GetProperty("hasPassword").GetBoolean());
        Assert.False(user.GetProperty("twoFactorEnabled").GetBoolean());
    }

    [Fact]
    public async Task UpdateProfile_LuuCacTruongMoRong()
    {
        var (_, token) = await RegisterAsync();

        var resp = await Authed(token).PutAsJsonAsync("/api/users/me", new
        {
            displayName = "Tên Mới",
            phoneNumber = "0900000000",
            dateOfBirth = "1995-03-20",
            gender = "male",
            bio = "Xin chào",
            timeZone = "Asia/Ho_Chi_Minh",
            locale = "vi",
        });

        resp.EnsureSuccessStatusCode();
        var u = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tên Mới", u.GetProperty("displayName").GetString());
        Assert.Equal("0900000000", u.GetProperty("phoneNumber").GetString());
        Assert.Equal("1995-03-20", u.GetProperty("dateOfBirth").GetString());
        Assert.Equal("male", u.GetProperty("gender").GetString());
        Assert.Equal("Xin chào", u.GetProperty("bio").GetString());
    }

    [Fact]
    public async Task UpdateProfile_GioiTinhKhongHopLe_400()
    {
        var (_, token) = await RegisterAsync();

        var resp = await Authed(token).PutAsJsonAsync("/api/users/me",
            new { displayName = "X", gender = "khong-hop-le" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // --- Đổi email ---

    [Fact]
    public async Task ChangeEmail_SaiMatKhau_400()
    {
        var (_, token) = await RegisterAsync();

        var resp = await Authed(token).PostAsJsonAsync("/api/users/me/change-email",
            new { newEmail = NewEmail(), currentPassword = "SAI" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangeEmail_DungMatKhau_DangNhapDuocBangEmailMoi()
    {
        var (_, token) = await RegisterAsync();
        var newEmail = NewEmail();

        var resp = await Authed(token).PostAsJsonAsync("/api/users/me/change-email",
            new { newEmail, currentPassword = Pwd });
        resp.EnsureSuccessStatusCode();

        var login = await LoginAsync(newEmail, Pwd);
        Assert.NotEqual(JsonValueKind.Null, login.GetProperty("auth").ValueKind);
    }

    [Fact]
    public async Task ChangeEmail_TrungEmailNguoiKhac_400()
    {
        var (existing, _) = await RegisterAsync();
        var (_, token) = await RegisterAsync();

        var resp = await Authed(token).PostAsJsonAsync("/api/users/me/change-email",
            new { newEmail = existing, currentPassword = Pwd });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // --- Avatar ---

    [Fact]
    public async Task UploadAvatar_SaiDinhDang_400()
    {
        var (_, token) = await RegisterAsync();

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent("khong-phai-anh"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "x.txt");

        var resp = await Authed(token).PostAsync("/api/users/me/avatar", content);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_PngHopLe_GanAvatarUrl()
    {
        var (_, token) = await RegisterAsync();

        // PNG tối thiểu hợp lệ (signature + IHDR…), nội dung không cần là ảnh thật.
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "a.png");

        var resp = await Authed(token).PostAsync("/api/users/me/avatar", content);

        resp.EnsureSuccessStatusCode();
        var u = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("/uploads/avatars/", u.GetProperty("avatarUrl").GetString());
    }

    // --- Đăng nhập Google ---

    [Fact]
    public async Task GoogleLogin_EmailMoi_TaoTaiKhoan_TraToken()
    {
        var email = NewEmail();

        var resp = await Client().PostAsJsonAsync("/api/auth/google", new { idToken = $"valid:{email}" });

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var user = body.GetProperty("auth").GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        // Tài khoản tạo qua Google không có mật khẩu.
        Assert.False(user.GetProperty("hasPassword").GetBoolean());
    }

    [Fact]
    public async Task GoogleLogin_EmailChuaXacThuc_401()
    {
        var email = NewEmail();

        var resp = await Client().PostAsJsonAsync("/api/auth/google", new { idToken = $"unverified:{email}" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // --- 2FA ---

    [Fact]
    public async Task TwoFactor_BatRoiDangNhapHaiBuoc_BangTotp()
    {
        var (email, token) = await RegisterAsync();
        var auth = Authed(token);

        var setup = await (await auth.PostAsync("/api/users/me/2fa/setup", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var key = setup.GetProperty("sharedKey").GetString()!;

        var enable = await auth.PostAsJsonAsync("/api/users/me/2fa/enable", new { code = Totp.Compute(key) });
        enable.EnsureSuccessStatusCode();

        // Bước 1: login giờ trả thử thách 2FA, chưa có token.
        var login = await LoginAsync(email, Pwd);
        Assert.True(login.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.Equal(JsonValueKind.Null, login.GetProperty("auth").ValueKind);
        var tft = login.GetProperty("twoFactorToken").GetString()!;

        // Bước 2: nhập TOTP → cấp token.
        var resp = await Client().PostAsJsonAsync("/api/auth/login/2fa",
            new { twoFactorToken = tft, code = Totp.Compute(key) });
        resp.EnsureSuccessStatusCode();
        var done = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, done.GetProperty("auth").ValueKind);
    }

    [Fact]
    public async Task TwoFactor_MaKhoiPhuc_DungMotLan()
    {
        var (email, token) = await RegisterAsync();
        var auth = Authed(token);

        var setup = await (await auth.PostAsync("/api/users/me/2fa/setup", null))
            .Content.ReadFromJsonAsync<JsonElement>();
        var key = setup.GetProperty("sharedKey").GetString()!;

        var enableBody = await (await auth.PostAsJsonAsync("/api/users/me/2fa/enable", new { code = Totp.Compute(key) }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var recovery = enableBody.GetProperty("recoveryCodes")[0].GetString()!;

        // Dùng mã khôi phục lần đầu → thành công.
        var tft1 = (await LoginAsync(email, Pwd)).GetProperty("twoFactorToken").GetString()!;
        var ok = await Client().PostAsJsonAsync("/api/auth/login/2fa",
            new { twoFactorToken = tft1, code = recovery });
        ok.EnsureSuccessStatusCode();

        // Dùng lại chính mã đó → thất bại.
        var tft2 = (await LoginAsync(email, Pwd)).GetProperty("twoFactorToken").GetString()!;
        var reused = await Client().PostAsJsonAsync("/api/auth/login/2fa",
            new { twoFactorToken = tft2, code = recovery });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task LoginTwoFactor_TokenSai_401()
    {
        var resp = await Client().PostAsJsonAsync("/api/auth/login/2fa",
            new { twoFactorToken = "khong-hop-le", code = "123456" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // --- Xóa tài khoản ---

    [Fact]
    public async Task DeleteAccount_SaiMatKhau_400()
    {
        var (_, token) = await RegisterAsync();

        var req = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { currentPassword = "SAI" }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await Client().SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_DungMatKhau_204_RoiKhongDangNhapDuoc()
    {
        var (email, token) = await RegisterAsync();

        var req = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { currentPassword = Pwd }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await Client().SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var login = await Client().PostAsJsonAsync("/api/auth/login", new { email, password = Pwd });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
