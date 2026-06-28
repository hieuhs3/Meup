using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G7 (Career): Skill/Certification/Project CRUD, validate, cô lập.</summary>
public class G7CareerTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G7CareerTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g7_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Career User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Skill_CRUD_VaClampLevel()
    {
        var c = await NewUserClientAsync();
        var s = await Json(await c.PostAsJsonAsync("/api/career/skills",
            new { name = ".NET", category = "Backend", level = 4 }));
        var id = s.GetProperty("id").GetString();
        Assert.Equal(4, s.GetProperty("level").GetInt32());

        var updated = await Json(await c.PutAsJsonAsync($"/api/career/skills/{id}",
            new { name = ".NET Core", category = "Backend", level = 5 }));
        Assert.Equal(".NET Core", updated.GetProperty("name").GetString());
        Assert.Equal(5, updated.GetProperty("level").GetInt32());

        Assert.Equal(1, (await Json(await c.GetAsync("/api/career/skills"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/career/skills/{id}")).StatusCode);
        Assert.Equal(0, (await Json(await c.GetAsync("/api/career/skills"))).GetArrayLength());
    }

    [Fact]
    public async Task Skill_LevelNgoaiKhoang_400()
    {
        var c = await NewUserClientAsync();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PostAsJsonAsync("/api/career/skills", new { name = "X", level = 9 })).StatusCode);
    }

    [Fact]
    public async Task Certification_CRUD()
    {
        var c = await NewUserClientAsync();
        var cert = await Json(await c.PostAsJsonAsync("/api/career/certifications",
            new { name = "AWS SAA", issuer = "AWS", issuedAt = "2025-01-01", expiresAt = "2028-01-01" }));
        Assert.Equal("AWS", cert.GetProperty("issuer").GetString());
        var id = cert.GetProperty("id").GetString();
        Assert.Equal(1, (await Json(await c.GetAsync("/api/career/certifications"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/career/certifications/{id}")).StatusCode);
    }

    [Fact]
    public async Task Project_CRUD()
    {
        var c = await NewUserClientAsync();
        var p = await Json(await c.PostAsJsonAsync("/api/career/projects",
            new { name = "MeUp", role = "Full-stack", description = "Personal OS", startedAt = "2026-06-01", endedAt = (string?)null }));
        Assert.Equal("Full-stack", p.GetProperty("role").GetString());
        var id = p.GetProperty("id").GetString();
        Assert.Equal(1, (await Json(await c.GetAsync("/api/career/projects"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/career/projects/{id}")).StatusCode);
    }

    [Fact]
    public async Task CoLap_VaChuaDangNhap401()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var s = await Json(await a.PostAsJsonAsync("/api/career/skills", new { name = "của A", level = 2 }));
        var id = s.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/career/skills"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/career/skills/{id}")).StatusCode);

        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/career/skills")).StatusCode);
    }
}
