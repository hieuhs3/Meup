using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G8 (Document): upload/list/download/delete, validate type/size, cô lập.</summary>
public class G8DocumentTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G8DocumentTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g8_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Doc User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    private static MultipartFormDataContent FileForm(string fileName, string category, byte[] bytes, string contentType = "application/pdf")
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(category), "category");
        return form;
    }

    [Fact]
    public async Task Upload_List_Download_Delete()
    {
        var c = await NewUserClientAsync();
        var bytes = Encoding.UTF8.GetBytes("noi dung CV gia lap");
        var created = await Json(await c.PostAsync("/api/documents", FileForm("cv.pdf", "cv", bytes)));
        var id = created.GetProperty("id").GetString();
        Assert.Equal("cv", created.GetProperty("category").GetString());
        Assert.Equal("cv.pdf", created.GetProperty("fileName").GetString());

        Assert.Equal(1, (await Json(await c.GetAsync("/api/documents"))).GetArrayLength());
        Assert.Equal(1, (await Json(await c.GetAsync("/api/documents?category=cv"))).GetArrayLength());
        Assert.Equal(0, (await Json(await c.GetAsync("/api/documents?category=invoice"))).GetArrayLength());

        var dl = await c.GetAsync($"/api/documents/{id}/download");
        dl.EnsureSuccessStatusCode();
        Assert.Equal(bytes, await dl.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/documents/{id}")).StatusCode);
        Assert.Equal(0, (await Json(await c.GetAsync("/api/documents"))).GetArrayLength());
    }

    [Fact]
    public async Task Upload_DinhDangKhongHoTro_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsync("/api/documents",
            FileForm("hack.exe", "other", Encoding.UTF8.GetBytes("x"), "application/octet-stream"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Download_CuaNguoiKhac_404()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var created = await Json(await a.PostAsync("/api/documents",
            FileForm("a.pdf", "personal", Encoding.UTF8.GetBytes("cua A"))));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/documents/{id}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/documents/{id}")).StatusCode);
        Assert.Equal(0, (await Json(await b.GetAsync("/api/documents"))).GetArrayLength());
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/documents")).StatusCode);
    }
}
