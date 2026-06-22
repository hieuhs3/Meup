using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho F2 (Sức khỏe): upsert theo ngày, cô lập user, so sánh, validate.</summary>
public class F2HealthTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public F2HealthTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"f2_{Guid.NewGuid():N}@test.local";

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
    public async Task Upsert_CapNhatCungNgay_KhongTaoTrung()
    {
        var c = await NewUserClientAsync();

        await c.PutAsJsonAsync("/api/health/logs/2026-06-10",
            new { weight = 70.5, sleepHours = 7, waterMl = 2000, workoutMinutes = 30, note = "ok" });
        await c.PutAsJsonAsync("/api/health/logs/2026-06-10",
            new { weight = 71.0, sleepHours = 8, waterMl = 2500, workoutMinutes = 45, note = "tốt hơn" });

        var list = await Json(await c.GetAsync("/api/health/logs"));
        Assert.Equal(1, list.GetArrayLength());

        var log = await Json(await c.GetAsync("/api/health/logs/2026-06-10"));
        Assert.Equal(71.0m, log.GetProperty("weight").GetDecimal());
        Assert.Equal(8, log.GetProperty("sleepHours").GetDecimal());
        Assert.Equal(2500, log.GetProperty("waterMl").GetInt32());
    }

    [Fact]
    public async Task GetLog_KhongCo_TraRong()
    {
        var c = await NewUserClientAsync();
        var resp = await c.GetAsync("/api/health/logs/2099-01-01");
        await AssertEmptyOrNull(resp);
    }

    /// <summary>Không có bản ghi → 204 No Content (hoặc thân rỗng/"null").</summary>
    private static async Task AssertEmptyOrNull(HttpResponseMessage resp)
    {
        var text = await resp.Content.ReadAsStringAsync();
        Assert.True(
            resp.StatusCode == HttpStatusCode.NoContent || string.IsNullOrEmpty(text) || text == "null",
            $"Mong đợi rỗng/null nhưng nhận {(int)resp.StatusCode}: '{text}'");
    }

    [Fact]
    public async Task Upsert_GiaTriVuotNguong_400()
    {
        var c = await NewUserClientAsync();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PutAsJsonAsync("/api/health/logs/2026-06-11", new { weight = 999 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PutAsJsonAsync("/api/health/logs/2026-06-11", new { sleepHours = 25 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PutAsJsonAsync("/api/health/logs/2026-06-11", new { waterMl = 99999 })).StatusCode);
    }

    [Fact]
    public async Task Summary_SoSanhVoiLanTruoc()
    {
        var c = await NewUserClientAsync();
        await c.PutAsJsonAsync("/api/health/logs/2026-06-01", new { weight = 70 });
        await c.PutAsJsonAsync("/api/health/logs/2026-06-05", new { weight = 69 });

        var s = await Json(await c.GetAsync("/api/health/summary?date=2026-06-05"));
        Assert.Equal("2026-06-05", s.GetProperty("today").GetProperty("date").GetString());
        Assert.Equal("2026-06-01", s.GetProperty("previous").GetProperty("date").GetString());
        Assert.Equal(69m, s.GetProperty("today").GetProperty("weight").GetDecimal());
        Assert.Equal(70m, s.GetProperty("previous").GetProperty("weight").GetDecimal());
    }

    [Fact]
    public async Task Summary_ChuaCoLanTruoc_PreviousNull()
    {
        var c = await NewUserClientAsync();
        await c.PutAsJsonAsync("/api/health/logs/2026-07-01", new { weight = 65 });

        var s = await Json(await c.GetAsync("/api/health/summary?date=2026-07-01"));
        Assert.Equal(JsonValueKind.Null, s.GetProperty("previous").ValueKind);
    }

    [Fact]
    public async Task DeleteLog_RoiXoaLai_404()
    {
        var c = await NewUserClientAsync();
        await c.PutAsJsonAsync("/api/health/logs/2026-06-20", new { weight = 60 });

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync("/api/health/logs/2026-06-20")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.DeleteAsync("/api/health/logs/2026-06-20")).StatusCode);
    }

    [Fact]
    public async Task List_LocTheoKhoangNgay()
    {
        var c = await NewUserClientAsync();
        await c.PutAsJsonAsync("/api/health/logs/2026-08-01", new { weight = 70 });
        await c.PutAsJsonAsync("/api/health/logs/2026-08-10", new { weight = 71 });
        await c.PutAsJsonAsync("/api/health/logs/2026-08-20", new { weight = 72 });

        var list = await Json(await c.GetAsync("/api/health/logs?from=2026-08-05&to=2026-08-15"));
        Assert.Equal(1, list.GetArrayLength());
        Assert.Equal("2026-08-10", list[0].GetProperty("date").GetString());
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongThay()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();

        await a.PutAsJsonAsync("/api/health/logs/2026-09-09", new { weight = 80, note = "của A" });

        // B không thấy bản ghi của A.
        await AssertEmptyOrNull(await b.GetAsync("/api/health/logs/2026-09-09"));

        var bList = await Json(await b.GetAsync("/api/health/logs"));
        Assert.Equal(0, bList.GetArrayLength());

        // B xóa ngày của A → 404 (B không có bản ghi ngày đó).
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync("/api/health/logs/2026-09-09")).StatusCode);

        // A vẫn còn bản ghi.
        var aLog = await Json(await a.GetAsync("/api/health/logs/2026-09-09"));
        Assert.Equal(80m, aLog.GetProperty("weight").GetDecimal());
    }

    [Fact]
    public async Task ChuaDangNhap_Bi401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/health/logs")).StatusCode);
    }
}
