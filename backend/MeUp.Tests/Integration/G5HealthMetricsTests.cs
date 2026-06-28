using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G5 (Health metrics): BMI, Activity CRUD, xu hướng, validate, cô lập.</summary>
public class G5HealthMetricsTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G5HealthMetricsTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g5_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Health User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Bmi_TinhTuCanNangVaChieuCao()
    {
        var c = await NewUserClientAsync();
        var log = await Json(await c.PutAsJsonAsync("/api/health/logs/2026-06-20",
            new { weight = 70, heightCm = 175 }));
        Assert.Equal(22.9m, log.GetProperty("bmi").GetDecimal()); // 70/(1.75²) ≈ 22.9
    }

    [Fact]
    public async Task Bmi_ThieuChieuCao_Null()
    {
        var c = await NewUserClientAsync();
        var log = await Json(await c.PutAsJsonAsync("/api/health/logs/2026-06-20", new { weight = 70 }));
        Assert.True(log.GetProperty("bmi").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Activity_CRUD_VaTrendCalories()
    {
        var c = await NewUserClientAsync();
        var a = await Json(await c.PostAsJsonAsync("/api/health/activities",
            new { date = "2026-06-15", type = "running", durationMin = 30, calories = 300, note = (string?)null }));
        Assert.Equal("running", a.GetProperty("type").GetString());
        var id = a.GetProperty("id").GetString();

        await c.PostAsJsonAsync("/api/health/activities",
            new { date = "2026-06-15", type = "gym", durationMin = 45, calories = 200, note = (string?)null });

        var list = await Json(await c.GetAsync("/api/health/activities?from=2026-06-15&to=2026-06-15"));
        Assert.Equal(2, list.GetArrayLength());

        var trend = await Json(await c.GetAsync("/api/health/trends?from=2026-06-01&to=2026-06-30"));
        var cal = trend.GetProperty("calories").EnumerateArray().First(p => p.GetProperty("date").GetString() == "2026-06-15");
        Assert.Equal(500, cal.GetProperty("value").GetDecimal()); // 300 + 200

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/health/activities/{id}")).StatusCode);
    }

    [Fact]
    public async Task Activity_LoaiKhongHopLe_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/health/activities",
            new { date = "2026-06-15", type = "flying", durationMin = 10, calories = (int?)null, note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Trend_WeightVaBmi()
    {
        var c = await NewUserClientAsync();
        await c.PutAsJsonAsync("/api/health/logs/2026-05-10", new { weight = 72, heightCm = 175 });
        await c.PutAsJsonAsync("/api/health/logs/2026-05-12", new { weight = 71, heightCm = 175 });

        var trend = await Json(await c.GetAsync("/api/health/trends?from=2026-05-01&to=2026-05-31"));
        Assert.Equal(2, trend.GetProperty("weight").GetArrayLength());
        Assert.Equal(2, trend.GetProperty("bmi").GetArrayLength());
        // Sắp theo ngày tăng dần
        Assert.Equal("2026-05-10", trend.GetProperty("weight")[0].GetProperty("date").GetString());
    }

    [Fact]
    public async Task Activity_CoLap_VaChuaDangNhap401()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var created = await Json(await a.PostAsJsonAsync("/api/health/activities",
            new { date = "2026-06-15", type = "walking", durationMin = 20, calories = (int?)null, note = (string?)null }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/health/activities"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/health/activities/{id}")).StatusCode);

        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/health/trends")).StatusCode);
    }
}
