using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

// --- A5: Ghi chú nhanh ---
public class NoteTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public NoteTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task CRUD()
    {
        var c = await P2.NewUserAsync(_f, "note");

        var n = await P2.Json(await c.PostAsJsonAsync("/api/notes", new { content = "Mua sữa" }));
        var id = n.GetProperty("id").GetString();
        Assert.Equal("Mua sữa", n.GetProperty("content").GetString());

        var updated = await P2.Json(await c.PutAsJsonAsync($"/api/notes/{id}", new { content = "Mua sữa và bánh mì" }));
        Assert.Equal("Mua sữa và bánh mì", updated.GetProperty("content").GetString());

        Assert.Equal(1, (await P2.Json(await c.GetAsync("/api/notes"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/notes/{id}")).StatusCode);
        Assert.Equal(0, (await P2.Json(await c.GetAsync("/api/notes"))).GetArrayLength());
    }

    [Fact]
    public async Task NoiDungRong_400()
    {
        var c = await P2.NewUserAsync(_f, "note");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/notes", new { content = "" })).StatusCode);
    }

    [Fact]
    public async Task CoLap()
    {
        var a = await P2.NewUserAsync(_f, "note");
        var b = await P2.NewUserAsync(_f, "note");
        await a.PostAsJsonAsync("/api/notes", new { content = "của A" });
        Assert.Equal(0, (await P2.Json(await b.GetAsync("/api/notes"))).GetArrayLength());
    }
}

// --- C3: Xuất dữ liệu ---
public class ExportTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _f;
    public ExportTests(MeUpWebAppFactory f) => _f = f;

    [Fact]
    public async Task XuatGomDuLieuCuaUser()
    {
        var c = await P2.NewUserAsync(_f, "exp");
        await c.PostAsJsonAsync("/api/finance/transactions", new { type = "income", amount = 100000, date = "2026-06-18", note = "test thu" });
        await c.PostAsJsonAsync("/api/notes", new { content = "ghi chú xuất" });

        var dump = await P2.Json(await c.GetAsync("/api/export"));
        Assert.True(dump.TryGetProperty("transactions", out var txs) && txs.GetArrayLength() >= 1);
        Assert.True(dump.TryGetProperty("notes", out var notes) && notes.GetArrayLength() >= 1);
        Assert.True(dump.TryGetProperty("exportedAt", out _));
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _f.CreateClient().GetAsync("/api/export")).StatusCode);
    }
}
