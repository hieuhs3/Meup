using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

// --- A2: Thuốc ---
public class MedicationTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public MedicationTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task CRUD_VaDanhDauUong()
    {
        var c = await P2.NewUserAsync(_f, "med");

        var med = await P2.Json(await c.PostAsJsonAsync("/api/medications",
            new { name = "Vitamin C", dosage = "1 viên sáng", note = "sau ăn" }));
        var id = med.GetProperty("id").GetString();
        Assert.False(med.GetProperty("taken").GetBoolean());

        // đánh dấu uống hôm 2026-06-18
        var taken = await P2.Json(await c.PostAsync($"/api/medications/{id}/take?date=2026-06-18", null));
        Assert.True(taken.GetProperty("taken").GetBoolean());

        // list theo ngày đó → taken = true
        var list = await P2.Json(await c.GetAsync("/api/medications?date=2026-06-18"));
        Assert.True(list[0].GetProperty("taken").GetBoolean());
        // ngày khác → taken = false
        var other = await P2.Json(await c.GetAsync("/api/medications?date=2026-06-19"));
        Assert.False(other[0].GetProperty("taken").GetBoolean());

        // bỏ đánh dấu
        var untaken = await P2.Json(await c.DeleteAsync($"/api/medications/{id}/take?date=2026-06-18"));
        Assert.False(untaken.GetProperty("taken").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/medications/{id}")).StatusCode);
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongThay()
    {
        var a = await P2.NewUserAsync(_f, "med");
        var b = await P2.NewUserAsync(_f, "med");
        await a.PostAsJsonAsync("/api/medications", new { name = "Thuốc của A", dosage = (string?)null, note = (string?)null });
        Assert.Equal(0, (await P2.Json(await b.GetAsync("/api/medications"))).GetArrayLength());
    }
}

// --- A3: Task lặp lại ---
public class RecurringTaskTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public RecurringTaskTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task HoanThanhTaskLap_TuSinhLanKe()
    {
        var c = await P2.NewUserAsync(_f, "rec");

        var t = await P2.Json(await c.PostAsJsonAsync("/api/work/tasks",
            new { title = "Uống nước", dueDate = "2026-06-18", recurrence = "daily" }));
        Assert.Equal("daily", t.GetProperty("recurrence").GetString());
        var id = t.GetProperty("id").GetString();

        // ban đầu 1 task
        Assert.Equal(1, (await P2.Json(await c.GetAsync("/api/work/tasks?status=all"))).GetArrayLength());

        // hoàn thành → sinh lần kế (due +1 ngày)
        await c.PostAsync($"/api/work/tasks/{id}/toggle", null);

        var all = await P2.Json(await c.GetAsync("/api/work/tasks?status=all"));
        Assert.Equal(2, all.GetArrayLength());
        var active = await P2.Json(await c.GetAsync("/api/work/tasks?status=active"));
        Assert.Equal(1, active.GetArrayLength());
        Assert.Equal("2026-06-19", active[0].GetProperty("dueDate").GetString());
        Assert.Equal("daily", active[0].GetProperty("recurrence").GetString());
    }

    [Fact]
    public async Task TaskKhongLap_KhongSinhThem()
    {
        var c = await P2.NewUserAsync(_f, "rec");
        var t = await P2.Json(await c.PostAsJsonAsync("/api/work/tasks",
            new { title = "Việc một lần", dueDate = "2026-06-18", recurrence = "none" }));
        await c.PostAsync($"/api/work/tasks/{t.GetProperty("id").GetString()}/toggle", null);
        Assert.Equal(1, (await P2.Json(await c.GetAsync("/api/work/tasks?status=all"))).GetArrayLength());
    }
}

// --- B2: Tìm kiếm toàn cục ---
public class SearchTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public SearchTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task TimXuyenNhieuLoai()
    {
        var c = await P2.NewUserAsync(_f, "search");
        await c.PostAsJsonAsync("/api/finance/transactions", new { type = "expense", amount = 50000, date = "2026-06-18", note = "cà phê sáng" });
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-06-18", title = "Ngày cà phê", contentHtml = "<p>thư giãn</p>" });
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "Mua cà phê", recurrence = "none" });
        await c.PostAsJsonAsync("/api/events", new { date = "2026-06-18", title = "Hẹn cà phê" });

        var res = await P2.Json(await c.GetAsync("/api/search?q=cà"));
        Assert.Equal(4, res.GetProperty("total").GetInt32());
        var types = res.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("type").GetString()).ToHashSet();
        Assert.Contains("transaction", types);
        Assert.Contains("journal", types);
        Assert.Contains("task", types);
        Assert.Contains("event", types);
    }

    [Fact]
    public async Task RongThiKhongCoKetQua()
    {
        var c = await P2.NewUserAsync(_f, "search");
        var res = await P2.Json(await c.GetAsync("/api/search?q="));
        Assert.Equal(0, res.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task CoLap_KhongTimThayCuaNguoiKhac()
    {
        var a = await P2.NewUserAsync(_f, "search");
        var b = await P2.NewUserAsync(_f, "search");
        await a.PostAsJsonAsync("/api/work/tasks", new { title = "bí mật xyzzy", recurrence = "none" });
        var res = await P2.Json(await b.GetAsync("/api/search?q=xyzzy"));
        Assert.Equal(0, res.GetProperty("total").GetInt32());
    }
}
