using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho F5 (Nhật ký): CRUD, cô lập user, lọc/tìm kiếm.</summary>
public class F5JournalTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public F5JournalTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"f5_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Journal User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Create_Get_Update_Delete()
    {
        var c = await NewUserClientAsync();

        var created = await Json(await c.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-18", title = "Ngày đầu", contentHtml = "<p>Xin <b>chào</b></p>" }));
        var id = created.GetProperty("id").GetString();
        Assert.Equal("Ngày đầu", created.GetProperty("title").GetString());
        Assert.Contains("<b>chào</b>", created.GetProperty("contentHtml").GetString());

        // Get
        var got = await Json(await c.GetAsync($"/api/journal/{id}"));
        Assert.Equal(id, got.GetProperty("id").GetString());

        // Update
        var updated = await Json(await c.PutAsJsonAsync($"/api/journal/{id}",
            new { date = "2026-06-18", title = "Đã sửa", contentHtml = "<p>Nội dung mới</p>" }));
        Assert.Equal("Đã sửa", updated.GetProperty("title").GetString());

        // Delete
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/journal/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/journal/{id}")).StatusCode);
    }

    [Fact]
    public async Task TitleQuaDai_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-18", title = new string('x', 201), contentHtml = "<p>x</p>" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task LocVaTimKiem()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-05-01", title = "Du lịch Đà Lạt", contentHtml = "<p>thông và sương</p>" });
        await c.PostAsJsonAsync("/api/journal", new { date = "2026-05-20", title = "Họp dự án", contentHtml = "<p>deadline</p>" });

        // tìm theo tiêu đề
        var byTitle = await Json(await c.GetAsync("/api/journal?q=Đà"));
        Assert.Equal(1, byTitle.GetArrayLength());

        // tìm theo nội dung
        var byContent = await Json(await c.GetAsync("/api/journal?q=deadline"));
        Assert.Equal(1, byContent.GetArrayLength());

        // lọc khoảng ngày
        var byRange = await Json(await c.GetAsync("/api/journal?from=2026-05-10&to=2026-05-31"));
        Assert.Equal(1, byRange.GetArrayLength());
        Assert.Equal("Họp dự án", byRange[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task CoLap_NguoiKhacKhongThayVaKhongTruyCap()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();

        var created = await Json(await a.PostAsJsonAsync("/api/journal",
            new { date = "2026-06-18", title = "Bí mật của A", contentHtml = "<p>riêng tư</p>" }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/journal"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/journal/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/journal/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/journal/{id}")).StatusCode);
    }

    [Fact]
    public async Task ChuaDangNhap_Bi401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/journal")).StatusCode);
    }
}
