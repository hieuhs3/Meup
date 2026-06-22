using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Tiện ích chung cho test Phase 2.</summary>
internal static class P2
{
    public const string Pwd = "Passw0rd!";

    public static async Task<HttpClient> NewUserAsync(MeUpWebAppFactory factory, string prefix)
    {
        var client = factory.CreateClient();
        var email = $"{prefix}_{Guid.NewGuid():N}@test.local";
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = Pwd, displayName = "P2 User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    public static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<string> CreateExpenseCategoryAsync(HttpClient c, string name)
    {
        var cat = await Json(await c.PostAsJsonAsync("/api/finance/categories",
            new { name, type = "expense", color = "#123456" }));
        return cat.GetProperty("id").GetString()!;
    }
}

// --- A1: Ngân sách ---
public class BudgetTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public BudgetTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task TaoNganSach_TinhDaChi_TheoThang()
    {
        var c = await P2.NewUserAsync(_f, "bud");
        var catId = await P2.CreateExpenseCategoryAsync(c, "Ăn uống");

        var b = await P2.Json(await c.PostAsJsonAsync("/api/finance/budgets?month=2026-06-15",
            new { categoryId = catId, amount = 2_000_000 }));
        Assert.Equal(0m, b.GetProperty("spent").GetDecimal());

        // chi 500k trong tháng 6
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 500_000, categoryId = catId, date = "2026-06-20" });
        // chi 100k tháng khác (không tính)
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 100_000, categoryId = catId, date = "2026-07-01" });

        var list = await P2.Json(await c.GetAsync("/api/finance/budgets?month=2026-06-15"));
        var item = list[0];
        Assert.Equal(500_000m, item.GetProperty("spent").GetDecimal());
        Assert.Equal(1_500_000m, item.GetProperty("remaining").GetDecimal());
        Assert.Equal(25, item.GetProperty("percent").GetInt32());
    }

    [Fact]
    public async Task NganSachChoDanhMucThu_400()
    {
        var c = await P2.NewUserAsync(_f, "bud");
        var inc = await P2.Json(await c.PostAsJsonAsync("/api/finance/categories",
            new { name = "Lương", type = "income", color = (string?)null }));
        var resp = await c.PostAsJsonAsync("/api/finance/budgets",
            new { categoryId = inc.GetProperty("id").GetString(), amount = 1000 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task NganSachTrungDanhMuc_400()
    {
        var c = await P2.NewUserAsync(_f, "bud");
        var catId = await P2.CreateExpenseCategoryAsync(c, "Đi lại");
        (await c.PostAsJsonAsync("/api/finance/budgets", new { categoryId = catId, amount = 1000 })).EnsureSuccessStatusCode();
        var dup = await c.PostAsJsonAsync("/api/finance/budgets", new { categoryId = catId, amount = 2000 });
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongThay()
    {
        var a = await P2.NewUserAsync(_f, "bud");
        var b = await P2.NewUserAsync(_f, "bud");
        var catId = await P2.CreateExpenseCategoryAsync(a, "Mua sắm");
        var created = await P2.Json(await a.PostAsJsonAsync("/api/finance/budgets", new { categoryId = catId, amount = 1000 }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(0, (await P2.Json(await b.GetAsync("/api/finance/budgets"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/finance/budgets/{id}")).StatusCode);
    }
}

// --- F4: Lịch trình & Sự kiện ---
public class EventTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public EventTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task CRUD_VaLocKhoangNgay()
    {
        var c = await P2.NewUserAsync(_f, "evt");

        var created = await P2.Json(await c.PostAsJsonAsync("/api/events",
            new { date = "2026-06-20", startTime = "09:00:00", endTime = "10:00:00", title = "Họp", location = "Phòng A", note = "mang laptop" }));
        var id = created.GetProperty("id").GetString();
        Assert.Equal("Họp", created.GetProperty("title").GetString());

        await c.PostAsJsonAsync("/api/events", new { date = "2026-07-01", title = "Khám sức khỏe" });

        var inJune = await P2.Json(await c.GetAsync("/api/events?from=2026-06-01&to=2026-06-30"));
        Assert.Equal(1, inJune.GetArrayLength());

        var updated = await P2.Json(await c.PutAsJsonAsync($"/api/events/{id}",
            new { date = "2026-06-20", title = "Họp dời giờ", startTime = "14:00:00", endTime = (string?)null, location = (string?)null, note = (string?)null }));
        Assert.Equal("Họp dời giờ", updated.GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task KhongTieuDe_400()
    {
        var c = await P2.NewUserAsync(_f, "evt");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync("/api/events", new { date = "2026-06-20", title = "" })).StatusCode);
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongTruyCap()
    {
        var a = await P2.NewUserAsync(_f, "evt");
        var b = await P2.NewUserAsync(_f, "evt");
        var created = await P2.Json(await a.PostAsJsonAsync("/api/events", new { date = "2026-06-20", title = "Riêng tư" }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(0, (await P2.Json(await b.GetAsync("/api/events"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/events")).StatusCode);
    }
}

// --- F7: Thống kê ---
public class StatsTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public StatsTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task TongHop_F1_F2_F3_TrongKhoang()
    {
        var c = await P2.NewUserAsync(_f, "stat");

        // Tài chính
        await c.PostAsJsonAsync("/api/finance/transactions", new { type = "income", amount = 3_000_000, date = "2026-08-05" });
        await c.PostAsJsonAsync("/api/finance/transactions", new { type = "expense", amount = 800_000, date = "2026-08-06" });
        // Sức khỏe
        await c.PutAsJsonAsync("/api/health/logs/2026-08-05", new { weight = 70, sleepHours = 8 });
        await c.PutAsJsonAsync("/api/health/logs/2026-08-07", new { weight = 72, sleepHours = 6 });
        // Công việc
        var t = await P2.Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Xong việc" }));
        await c.PostAsync($"/api/work/tasks/{t.GetProperty("id").GetString()}/toggle", null);

        // Khoảng rộng để bao cả dữ liệu tháng 8 lẫn task hoàn thành ở thời điểm hiện tại.
        var s = await P2.Json(await c.GetAsync("/api/stats?from=2026-01-01&to=2026-12-31"));

        Assert.Equal(3_000_000m, s.GetProperty("finance").GetProperty("totalIncome").GetDecimal());
        Assert.Equal(800_000m, s.GetProperty("finance").GetProperty("totalExpense").GetDecimal());
        Assert.Equal(71m, s.GetProperty("health").GetProperty("avgWeight").GetDecimal()); // (70+72)/2
        Assert.True(s.GetProperty("work").GetProperty("tasksDone").GetInt32() >= 1);
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/stats")).StatusCode);
    }
}
