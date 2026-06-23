using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho F3 (Công việc/Mục tiêu/Thói quen): CRUD, quá hạn, tiến độ, streak, cô lập.</summary>
public class F3WorkTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public F3WorkTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"f3_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Work User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    // --- Task ---

    [Fact]
    public async Task Task_QuaHan_VaToggle()
    {
        var c = await NewUserClientAsync();

        var overdue = await Json(await c.PostAsJsonAsync("/api/work/tasks",
            new { title = "Việc trễ", dueDate = "2020-01-01" }));
        Assert.True(overdue.GetProperty("isOverdue").GetBoolean());

        var id = overdue.GetProperty("id").GetString();
        var toggled = await Json(await c.PostAsync($"/api/work/tasks/{id}/toggle", null));
        Assert.True(toggled.GetProperty("isDone").GetBoolean());
        Assert.False(toggled.GetProperty("isOverdue").GetBoolean()); // đã xong thì không còn quá hạn
    }

    [Fact]
    public async Task Task_LocTheoTrangThai()
    {
        var c = await NewUserClientAsync();
        var t1 = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "A" }));
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "B" });
        await c.PostAsync($"/api/work/tasks/{t1.GetProperty("id").GetString()}/toggle", null);

        Assert.Equal(2, (await Json(await c.GetAsync("/api/work/tasks?status=all"))).GetArrayLength());
        Assert.Equal(1, (await Json(await c.GetAsync("/api/work/tasks?status=active"))).GetArrayLength());
        Assert.Equal(1, (await Json(await c.GetAsync("/api/work/tasks?status=done"))).GetArrayLength());
    }

    [Fact]
    public async Task Task_KhongTieuDe_400()
    {
        var c = await NewUserClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync("/api/work/tasks", new { title = "" })).StatusCode);
    }

    [Fact]
    public async Task Task_Update_KhongTonTai_404()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PutAsJsonAsync($"/api/work/tasks/{Guid.NewGuid()}",
            new { title = "x", isDone = false });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // --- Goal ---

    [Fact]
    public async Task Goal_TienDoTuDong_TheoTaskCon()
    {
        var c = await NewUserClientAsync();
        var g = await Json(await c.PostAsJsonAsync("/api/work/goals", new { name = "Đọc sách" }));
        var gid = g.GetProperty("id").GetString();
        Assert.Equal(0, g.GetProperty("progress").GetInt32());

        var t1 = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Chương 1", goalId = gid }));
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "Chương 2", goalId = gid });
        await c.PostAsync($"/api/work/tasks/{t1.GetProperty("id").GetString()}/toggle", null);

        var goal = (await Json(await c.GetAsync("/api/work/goals")))[0];
        Assert.Equal(2, goal.GetProperty("taskCount").GetInt32());
        Assert.Equal(1, goal.GetProperty("doneCount").GetInt32());
        Assert.Equal(50, goal.GetProperty("progress").GetInt32()); // 1/2 xong = 50%
    }

    [Fact]
    public async Task Task_SubTask_KeThuaGoal_VaXoaGoalCascade()
    {
        var c = await NewUserClientAsync();
        var g = await Json(await c.PostAsJsonAsync("/api/work/goals", new { name = "Dự án" }));
        var gid = g.GetProperty("id").GetString();

        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Task cấp 1", goalId = gid }));
        var tid = t.GetProperty("id").GetString();
        Assert.Equal(gid, t.GetProperty("goalId").GetString());

        var sub = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Sub-task", parentTaskId = tid }));
        Assert.Equal(tid, sub.GetProperty("parentTaskId").GetString());
        Assert.Equal(gid, sub.GetProperty("goalId").GetString()); // kế thừa goal từ cha
        Assert.Equal(2, (await Json(await c.GetAsync("/api/work/tasks"))).GetArrayLength());

        // Xóa mục tiêu → cascade xóa cả task cấp 1 lẫn sub-task
        await c.DeleteAsync($"/api/work/goals/{gid}");
        Assert.Equal(0, (await Json(await c.GetAsync("/api/work/tasks"))).GetArrayLength());
    }

    // --- Habit ---

    [Fact]
    public async Task Habit_CheckLienTiep_StreakTang()
    {
        var c = await NewUserClientAsync();
        var h = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "Tập thể dục" }));
        var id = h.GetProperty("id").GetString();

        await c.PostAsync($"/api/work/habits/{id}/check?date=2026-03-10", null);
        await c.PostAsync($"/api/work/habits/{id}/check?date=2026-03-11", null);
        await c.PostAsync($"/api/work/habits/{id}/check?date=2026-03-11", null); // idempotent
        var last = await Json(await c.PostAsync($"/api/work/habits/{id}/check?date=2026-03-12", null));

        Assert.True(last.GetProperty("checked").GetBoolean());
        Assert.Equal(3, last.GetProperty("streak").GetInt32());

        // Bỏ check ngày cuối → streak tại ngày đó về 0.
        var unchecked_ = await Json(await c.DeleteAsync($"/api/work/habits/{id}/check?date=2026-03-12"));
        Assert.False(unchecked_.GetProperty("checked").GetBoolean());
        Assert.Equal(0, unchecked_.GetProperty("streak").GetInt32());
    }

    [Fact]
    public async Task Habit_KhoangTrong_StreakDut()
    {
        var c = await NewUserClientAsync();
        var h = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "Thiền" }));
        var id = h.GetProperty("id").GetString();

        await c.PostAsync($"/api/work/habits/{id}/check?date=2026-04-01", null);
        // bỏ trống 04-02
        await c.PostAsync($"/api/work/habits/{id}/check?date=2026-04-03", null);

        var list = await Json(await c.GetAsync("/api/work/habits?date=2026-04-03"));
        Assert.Equal(1, list[0].GetProperty("streak").GetInt32());
    }

    // --- Summary + cô lập ---

    [Fact]
    public async Task Summary_DemDungSoLieu()
    {
        var c = await NewUserClientAsync();
        var g = await Json(await c.PostAsJsonAsync("/api/work/goals", new { name = "G" }));
        var gid = g.GetProperty("id").GetString();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "T1", goalId = gid }));
        await c.PostAsync($"/api/work/tasks/{t.GetProperty("id").GetString()}/toggle", null);
        await c.PostAsJsonAsync("/api/work/tasks", new { title = "T2", dueDate = "2020-01-01", goalId = gid });
        var h = await Json(await c.PostAsJsonAsync("/api/work/habits", new { name = "H" }));
        await c.PostAsync($"/api/work/habits/{h.GetProperty("id").GetString()}/check?date=2026-06-18", null);

        var s = await Json(await c.GetAsync("/api/work/summary?date=2026-06-18"));
        Assert.Equal(2, s.GetProperty("tasksTotal").GetInt32());
        Assert.Equal(1, s.GetProperty("tasksDone").GetInt32());
        Assert.Equal(1, s.GetProperty("tasksOverdue").GetInt32());
        Assert.Equal(50, s.GetProperty("goalsAvgProgress").GetInt32());
        Assert.Equal(1, s.GetProperty("habitsCheckedToday").GetInt32());
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongTruyCapTask()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();

        var t = await Json(await a.PostAsJsonAsync("/api/work/tasks", new { title = "của A" }));
        var id = t.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/work/tasks"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/work/tasks/{id}/toggle", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/work/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task ChuaDangNhap_Bi401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/work/tasks")).StatusCode);
    }
}
