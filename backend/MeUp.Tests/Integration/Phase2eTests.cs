using System.Net.Http.Json;
using System.Text.Json;
using System.Net;

namespace MeUp.Tests.Integration;

// --- D: AI Insights (nhánh chưa cấu hình API key — môi trường test không có Ai:ApiKey) ---
public class AiInsightTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public AiInsightTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task Status_TraVe_Disabled_KhiChuaCauHinhKey()
    {
        var c = await P2.NewUserAsync(_f, "ai");
        var s = await P2.Json(await c.GetAsync("/api/ai/status"));
        Assert.False(s.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task WeeklyInsight_Disabled_KhongGoiClaude()
    {
        var c = await P2.NewUserAsync(_f, "ai");
        var s = await P2.Json(await c.GetAsync("/api/ai/weekly-insight?date=2026-06-19"));
        Assert.False(s.GetProperty("enabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, s.GetProperty("summary").ValueKind);
    }

    [Fact]
    public async Task Categorize_Disabled()
    {
        var c = await P2.NewUserAsync(_f, "ai");
        var s = await P2.Json(await c.PostAsJsonAsync("/api/ai/categorize", new { note = "Highlands", type = "expense" }));
        Assert.False(s.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/ai/status")).StatusCode);
    }
}
