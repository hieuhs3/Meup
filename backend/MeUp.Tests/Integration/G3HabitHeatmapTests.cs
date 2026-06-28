using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G3 (Habit nâng cấp): best streak, completion rate, heatmap, frequency/target.</summary>
public class G3HabitHeatmapTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G3HabitHeatmapTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g3_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Habit User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task BestStreak_CompletionRate_RecentChecks()
    {
        var c = await NewUserClientAsync();
        var h = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "Đọc sách" }));
        var id = h.GetProperty("id").GetString();

        // Chuỗi 3 ngày liên tiếp + 1 ngày lẻ (cách quãng).
        foreach (var d in new[] { "2026-06-01", "2026-06-02", "2026-06-03", "2026-06-05" })
            await c.PostAsync($"/api/work/habits/{id}/check?date={d}", null);

        var list = await Json(await c.GetAsync("/api/work/habits?date=2026-06-05"));
        var habit = list[0];

        Assert.Equal(1, habit.GetProperty("streak").GetInt32());        // 06-04 trống → streak hiện tại = 1
        Assert.Equal(3, habit.GetProperty("bestStreak").GetInt32());    // 06-01..06-03
        Assert.Equal(13, habit.GetProperty("completionRate").GetInt32()); // 4/30 ≈ 13%
        Assert.Equal(4, habit.GetProperty("recentChecks").GetArrayLength());
    }

    [Fact]
    public async Task Frequency_Weekly_VoiTarget()
    {
        var c = await NewUserClientAsync();
        var created = await Json(await c.PostAsJsonAsync("/api/work/habits",
            new { name = "Tập gym", frequency = "weekly", targetPerWeek = 3 }));
        Assert.Equal("weekly", created.GetProperty("frequency").GetString());
        Assert.Equal(3, created.GetProperty("targetPerWeek").GetInt32());
    }

    [Fact]
    public async Task Frequency_MacDinh_Daily()
    {
        var c = await NewUserClientAsync();
        var created = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "Uống nước" }));
        Assert.Equal("daily", created.GetProperty("frequency").GetString());
        Assert.True(created.GetProperty("targetPerWeek").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_DoiTanSuat()
    {
        var c = await NewUserClientAsync();
        var h = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "Thiền" }));
        var id = h.GetProperty("id").GetString();

        var updated = await Json(await c.PutAsJsonAsync($"/api/work/habits/{id}",
            new { name = "Thiền", frequency = "weekly", targetPerWeek = 5 }));
        Assert.Equal("weekly", updated.GetProperty("frequency").GetString());
        Assert.Equal(5, updated.GetProperty("targetPerWeek").GetInt32());
    }
}
