using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MeUp.Tests.Integration;

// --- C2: Vòng đời tài khoản ---
public class AccountLifecycleTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public AccountLifecycleTests(MeUpWebAppFactory f) => _f = f;

    private static string TokenFrom(string email)
    {
        var body = CapturingEmailSender.Sent[email.ToLowerInvariant()].Body;
        var m = Regex.Match(body, @"token=([^""&]+)");
        return Uri.UnescapeDataString(m.Groups[1].Value);
    }

    [Fact]
    public async Task QuenVaDatLaiMatKhau()
    {
        var client = _f.CreateClient();
        var email = $"reset_{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Passw0rd!", displayName = "U" })).EnsureSuccessStatusCode();

        // quên mật khẩu → luôn 200
        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        // lấy token từ email đã "gửi", đặt lại
        var token = TokenFrom(email);
        var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { email, token, newPassword = "NewPass99!" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        // đăng nhập bằng mật khẩu mới
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NewPass99!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task QuenMatKhau_EmailKhongTonTai_Van200()
    {
        var resp = await _f.CreateClient().PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "khongton tai@test.local".Replace(" ", "") });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task XacThucEmail_BangToken()
    {
        var client = _f.CreateClient();
        var email = $"verify_{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Passw0rd!", displayName = "U" })).EnsureSuccessStatusCode();

        var token = TokenFrom(email); // email xác thực gửi khi đăng ký
        var confirm = await client.PostAsJsonAsync("/api/auth/confirm-email", new { email, token });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
    }

    [Fact]
    public async Task KhoaDangNhap_SauNhieuLanSai()
    {
        var client = _f.CreateClient();
        var email = $"lock_{Guid.NewGuid():N}@test.local";
        (await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Passw0rd!", displayName = "U" })).EnsureSuccessStatusCode();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SAI" });

        // Sau 5 lần sai → khóa tạm; đăng nhập đúng cũng bị chặn.
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("khóa", body.GetProperty("error").GetString());
    }
}

// --- C1: Thông báo + nhắc ---
public class NotificationTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public NotificationTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task NhacSinhThongBao_VaChongTrung()
    {
        var c = await P2.NewUserAsync(_f, "notif");
        // tạo việc quá hạn để có cái để nhắc
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "Việc trễ", dueDate = "2020-01-01", recurrence = "none" });

        var run1 = await P2.Json(await c.PostAsync("/api/notifications/run-reminders", null));
        Assert.True(run1.GetProperty("created").GetBoolean());

        var list = await P2.Json(await c.GetAsync("/api/notifications"));
        Assert.Equal(1, list.GetArrayLength());

        var count = await P2.Json(await c.GetAsync("/api/notifications/unread-count"));
        Assert.Equal(1, count.GetProperty("count").GetInt32());

        // chạy lại cùng ngày → chống trùng (không tạo thêm)
        var run2 = await P2.Json(await c.PostAsync("/api/notifications/run-reminders", null));
        Assert.False(run2.GetProperty("created").GetBoolean());
    }

    [Fact]
    public async Task DanhDauDaDoc()
    {
        var c = await P2.NewUserAsync(_f, "notif");
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "Trễ", dueDate = "2020-01-01", recurrence = "none" });
        var run = await P2.Json(await c.PostAsync("/api/notifications/run-reminders", null));
        var id = run.GetProperty("notification").GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        var count = await P2.Json(await c.GetAsync("/api/notifications/unread-count"));
        Assert.Equal(0, count.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task KhongCoGiDeNhac_ThiKhongTao()
    {
        var c = await P2.NewUserAsync(_f, "notif");
        var run = await P2.Json(await c.PostAsync("/api/notifications/run-reminders", null));
        Assert.False(run.GetProperty("created").GetBoolean());
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/notifications")).StatusCode);
    }
}
