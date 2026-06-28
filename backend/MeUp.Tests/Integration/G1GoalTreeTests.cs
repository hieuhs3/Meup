using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G1 (Mục tiêu đa cấp): rollup tiến độ, validate cấp/vòng, cây, lọc, tương thích, cô lập.</summary>
public class G1GoalTreeTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G1GoalTreeTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g1_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Goal User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    private static JsonElement Find(JsonElement arr, string id) =>
        arr.EnumerateArray().First(g => g.GetProperty("id").GetString() == id);

    private async Task<string> CreateGoalAsync(HttpClient c, object body)
    {
        var resp = await c.PostAsJsonAsync("/api/work/goals", body);
        resp.EnsureSuccessStatusCode();
        return (await Json(resp)).GetProperty("id").GetString()!;
    }

    private async Task AddTasksAsync(HttpClient c, string goalId, int total, int done)
    {
        for (var i = 0; i < total; i++)
        {
            var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = $"T{i}", goalId }));
            if (i < done) await c.PostAsync($"/api/work/tasks/{t.GetProperty("id").GetString()}/toggle", null);
        }
    }

    [Fact]
    public async Task Goal_MacDinh_LevelYear_StatusActive()
    {
        var c = await NewUserClientAsync();
        var g = await Json(await c.PostAsJsonAsync("/api/work/goals", new { name = "Mục tiêu trơn" }));
        Assert.Equal("year", g.GetProperty("level").GetString());
        Assert.Equal("active", g.GetProperty("status").GetString());
        Assert.Null(g.GetProperty("parentGoalId").GetString());
    }

    [Fact]
    public async Task Rollup_TrungBinh_GoalConVaTaskCon()
    {
        var c = await NewUserClientAsync();
        var p = await CreateGoalAsync(c, new { name = "Năm", level = "year" });
        var c1 = await CreateGoalAsync(c, new { name = "Quý 1", level = "quarter", parentGoalId = p });
        var c2 = await CreateGoalAsync(c, new { name = "Quý 2", level = "quarter", parentGoalId = p });
        await AddTasksAsync(c, c1, total: 2, done: 1); // 50%
        await AddTasksAsync(c, c2, total: 2, done: 2); // 100%

        var goals = await Json(await c.GetAsync("/api/work/goals"));
        Assert.Equal(50, Find(goals, c1).GetProperty("progress").GetInt32());
        Assert.Equal(100, Find(goals, c2).GetProperty("progress").GetInt32());
        Assert.Equal(75, Find(goals, p).GetProperty("progress").GetInt32()); // avg(50,100)
        Assert.Equal(2, Find(goals, p).GetProperty("childCount").GetInt32());
    }

    [Fact]
    public async Task Rollup_Completed_Tinh100()
    {
        var c = await NewUserClientAsync();
        var p = await CreateGoalAsync(c, new { name = "Năm", level = "year" });
        // Mục tiêu con hoàn thành, không task → vẫn coi là 100% trong rollup.
        await CreateGoalAsync(c, new { name = "Quý xong", level = "quarter", status = "completed", parentGoalId = p });

        var goals = await Json(await c.GetAsync("/api/work/goals"));
        Assert.Equal(100, Find(goals, p).GetProperty("progress").GetInt32());
    }

    [Fact]
    public async Task Rollup_CancelledArchived_LoaiKhoiMauSo()
    {
        var c = await NewUserClientAsync();
        var p = await CreateGoalAsync(c, new { name = "Năm", level = "year" });
        var active = await CreateGoalAsync(c, new { name = "Quý chạy", level = "quarter", parentGoalId = p });
        var cancelled = await CreateGoalAsync(c, new { name = "Quý hủy", level = "quarter", status = "cancelled", parentGoalId = p });
        await AddTasksAsync(c, active, total: 2, done: 1);    // 50%
        await AddTasksAsync(c, cancelled, total: 2, done: 2); // 100% nhưng bị loại

        var goals = await Json(await c.GetAsync("/api/work/goals"));
        Assert.Equal(50, Find(goals, p).GetProperty("progress").GetInt32()); // chỉ tính nhánh "active"
    }

    [Fact]
    public async Task ChaSaiCap_400()
    {
        var c = await NewUserClientAsync();
        var quarter = await CreateGoalAsync(c, new { name = "Quý", level = "quarter" });
        // Con cấp year (cao hơn) dưới cha cấp quarter (thấp hơn) → không hợp lệ.
        var resp = await c.PostAsJsonAsync("/api/work/goals", new { name = "Năm con", level = "year", parentGoalId = quarter });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChaKhongTonTai_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/work/goals",
            new { name = "Con mồ côi", level = "month", parentGoalId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task TuLamChaCuaChinhNo_400()
    {
        var c = await NewUserClientAsync();
        var g = await CreateGoalAsync(c, new { name = "G", level = "year" });
        var resp = await c.PutAsJsonAsync($"/api/work/goals/{g}",
            new { name = "G", level = "year", parentGoalId = g });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GoalTree_TraCayLongNhau()
    {
        var c = await NewUserClientAsync();
        var p = await CreateGoalAsync(c, new { name = "Đời", level = "life" });
        var child = await CreateGoalAsync(c, new { name = "Năm", level = "year", parentGoalId = p });

        var tree = await Json(await c.GetAsync("/api/work/goals/tree"));
        Assert.Equal(1, tree.GetArrayLength()); // chỉ 1 gốc
        var root = tree[0];
        Assert.Equal(p, root.GetProperty("id").GetString());
        Assert.Equal(1, root.GetProperty("children").GetArrayLength());
        Assert.Equal(child, root.GetProperty("children")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task LocTheoLevelVaStatus()
    {
        var c = await NewUserClientAsync();
        await CreateGoalAsync(c, new { name = "Năm", level = "year" });
        await CreateGoalAsync(c, new { name = "Tháng", level = "month" });

        Assert.Equal(1, (await Json(await c.GetAsync("/api/work/goals?level=year"))).GetArrayLength());
        Assert.Equal(2, (await Json(await c.GetAsync("/api/work/goals?status=active"))).GetArrayLength());
        Assert.Equal(0, (await Json(await c.GetAsync("/api/work/goals?status=archived"))).GetArrayLength());
    }

    [Fact]
    public async Task XoaCha_CascadeXoaCayCon()
    {
        var c = await NewUserClientAsync();
        var p = await CreateGoalAsync(c, new { name = "Năm", level = "year" });
        await CreateGoalAsync(c, new { name = "Quý", level = "quarter", parentGoalId = p });

        await c.DeleteAsync($"/api/work/goals/{p}");
        Assert.Equal(0, (await Json(await c.GetAsync("/api/work/goals"))).GetArrayLength());
    }

    [Fact]
    public async Task CoLap_KhongDungGoalNguoiKhacLamCha()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var goalOfA = await CreateGoalAsync(a, new { name = "Của A", level = "year" });

        var resp = await b.PostAsJsonAsync("/api/work/goals",
            new { name = "Con của B", level = "month", parentGoalId = goalOfA });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // B không thấy goal của A → cha không tồn tại
    }
}
