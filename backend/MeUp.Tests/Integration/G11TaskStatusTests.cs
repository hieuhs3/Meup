using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G11 (Kanban / trạng thái task): đổi status, đồng bộ IsDone, lặp lại.</summary>
public class G11TaskStatusTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G11TaskStatusTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g11_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Kanban User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task TaoTask_MacDinh_StatusTodo()
    {
        var c = await NewUserClientAsync();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Việc A" }));
        Assert.Equal("todo", t.GetProperty("status").GetString());
        Assert.False(t.GetProperty("isDone").GetBoolean());
    }

    [Fact]
    public async Task DoiStatus_DongBoIsDone()
    {
        var c = await NewUserClientAsync();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Việc B" }));
        var id = t.GetProperty("id").GetString();

        var inProgress = await Json(await c.PutAsJsonAsync($"/api/work/tasks/{id}/status", new { status = "in_progress" }));
        Assert.Equal("in_progress", inProgress.GetProperty("status").GetString());
        Assert.False(inProgress.GetProperty("isDone").GetBoolean());

        var done = await Json(await c.PutAsJsonAsync($"/api/work/tasks/{id}/status", new { status = "done" }));
        Assert.Equal("done", done.GetProperty("status").GetString());
        Assert.True(done.GetProperty("isDone").GetBoolean());

        // Quay lại todo → IsDone false
        var back = await Json(await c.PutAsJsonAsync($"/api/work/tasks/{id}/status", new { status = "todo" }));
        Assert.False(back.GetProperty("isDone").GetBoolean());
    }

    [Fact]
    public async Task Toggle_DongBoStatus()
    {
        var c = await NewUserClientAsync();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Việc C" }));
        var id = t.GetProperty("id").GetString();

        var toggled = await Json(await c.PostAsync($"/api/work/tasks/{id}/toggle", null));
        Assert.True(toggled.GetProperty("isDone").GetBoolean());
        Assert.Equal("done", toggled.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StatusKhongHopLe_400()
    {
        var c = await NewUserClientAsync();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks", new { title = "Việc D" }));
        var id = t.GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PutAsJsonAsync($"/api/work/tasks/{id}/status", new { status = "blocked" })).StatusCode);
    }

    [Fact]
    public async Task DoiStatusDone_TaskLapLai_SinhLanKe()
    {
        var c = await NewUserClientAsync();
        var t = await Json(await c.PostAsJsonAsync("/api/work/tasks",
            new { title = "Tập thể dục", dueDate = "2026-06-20", recurrence = "daily" }));
        var id = t.GetProperty("id").GetString();

        await c.PutAsJsonAsync($"/api/work/tasks/{id}/status", new { status = "done" });
        // 1 task gốc (done) + 1 lần kế tiếp tự sinh = 2
        Assert.Equal(2, (await Json(await c.GetAsync("/api/work/tasks"))).GetArrayLength());
    }
}
