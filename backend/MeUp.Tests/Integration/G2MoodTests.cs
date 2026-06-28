using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G2 (Mood tracking trong Nhật ký): lưu/trả mood, validate, xu hướng.</summary>
public class G2MoodTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G2MoodTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g2_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Mood User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Mood_LuuVaTra()
    {
        var c = await NewUserClientAsync();
        var created = await Json(await c.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-20", title = "Ngày vui", contentHtml = "<p>hi</p>", mood = "excellent" }));
        Assert.Equal("excellent", created.GetProperty("mood").GetString());

        var list = await Json(await c.GetAsync("/api/journal"));
        Assert.Equal("excellent", list[0].GetProperty("mood").GetString());
    }

    [Fact]
    public async Task Mood_KhongHopLe_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-20", contentHtml = "x", mood = "happy" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Mood_BoTrong_VanLuuDuoc()
    {
        var c = await NewUserClientAsync();
        var created = await Json(await c.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-21", contentHtml = "không mood" }));
        Assert.Null(created.GetProperty("mood").GetString());
    }

    [Fact]
    public async Task MoodTrend_SapXepTheoNgay_CoScore_LoaiBaiKhongMood()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-06-12", contentHtml = "a", mood = "good" });        // score 4
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-06-10", contentHtml = "b", mood = "terrible" });    // score 1
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-06-11", contentHtml = "c" });                       // không mood → loại

        var trend = await Json(await c.GetAsync("/api/journal/mood-trend"));
        Assert.Equal(2, trend.GetArrayLength());
        // Sắp theo ngày tăng dần
        Assert.Equal("2026-06-10", trend[0].GetProperty("date").GetString());
        Assert.Equal(1, trend[0].GetProperty("score").GetInt32());
        Assert.Equal("2026-06-12", trend[1].GetProperty("date").GetString());
        Assert.Equal(4, trend[1].GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task MoodTrend_ChuaDangNhap_401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/journal/mood-trend")).StatusCode);
    }
}
