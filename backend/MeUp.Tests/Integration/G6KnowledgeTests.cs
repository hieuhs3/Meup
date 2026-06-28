using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G6 (Knowledge): title/category/tags, backlinks [[..]], lọc, cô lập.</summary>
public class G6KnowledgeTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G6KnowledgeTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g6_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Note User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    private static JsonElement Find(JsonElement arr, string id) =>
        arr.EnumerateArray().First(n => n.GetProperty("id").GetString() == id);

    [Fact]
    public async Task Note_TaoVoiTieuDeNhomThe()
    {
        var c = await NewUserClientAsync();
        var n = await Json(await c.PostAsJsonAsync("/api/notes",
            new { content = "Học về EF Core", title = "EF Core", category = ".NET", tags = new[] { "dotnet", "orm" } }));
        Assert.Equal("EF Core", n.GetProperty("title").GetString());
        Assert.Equal(".NET", n.GetProperty("category").GetString());
        Assert.Equal(2, n.GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public async Task Backlinks_VaOutLinks()
    {
        var c = await NewUserClientAsync();
        var a = await Json(await c.PostAsJsonAsync("/api/notes",
            new { content = "Nội dung về Docker", title = "Docker" }));
        var aid = a.GetProperty("id").GetString();
        var b = await Json(await c.PostAsJsonAsync("/api/notes",
            new { content = "Xem thêm [[Docker]] để hiểu container", title = "Container" }));
        var bid = b.GetProperty("id").GetString();

        var list = await Json(await c.GetAsync("/api/notes"));
        var noteB = Find(list, bid!);
        Assert.Contains("Docker", noteB.GetProperty("outLinks").EnumerateArray().Select(x => x.GetString()));

        var noteA = Find(list, aid!);
        var backlinkIds = noteA.GetProperty("backlinks").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()).ToList();
        Assert.Contains(bid, backlinkIds);
    }

    [Fact]
    public async Task LocTheoThe()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/notes", new { content = "a", title = "A", tags = new[] { "docker" } });
        await c.PostAsJsonAsync("/api/notes", new { content = "b", title = "B", tags = new[] { "java" } });

        Assert.Equal(1, (await Json(await c.GetAsync("/api/notes?tag=docker"))).GetArrayLength());
        Assert.Equal(2, (await Json(await c.GetAsync("/api/notes"))).GetArrayLength());
    }

    [Fact]
    public async Task TimTheoTuKhoa()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/notes", new { content = "Kubernetes orchestration", title = "K8s" });
        await c.PostAsJsonAsync("/api/notes", new { content = "Ghi chú khác", title = "Khác" });

        var hits = await Json(await c.GetAsync("/api/notes?q=orchestration"));
        Assert.Equal(1, hits.GetArrayLength());
    }

    [Fact]
    public async Task GhiChuNhanh_ChiNoiDung_VanHoatDong()
    {
        var c = await NewUserClientAsync();
        var n = await Json(await c.PostAsJsonAsync("/api/notes", new { content = "ghi chú nhanh" }));
        Assert.True(n.GetProperty("title").ValueKind == JsonValueKind.Null);
        Assert.Equal(0, n.GetProperty("tags").GetArrayLength());
    }

    [Fact]
    public async Task CoLap_VaChuaDangNhap401()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var n = await Json(await a.PostAsJsonAsync("/api/notes", new { content = "của A", title = "A" }));
        var id = n.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/notes"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/notes/{id}")).StatusCode);

        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/notes")).StatusCode);
    }
}
