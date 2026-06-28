using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho G4 (Tài sản & Net Worth): CRUD, tổng theo loại, saving rate, cash flow, validate.</summary>
public class G4AssetTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public G4AssetTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"g4_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Asset User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task Asset_CRUD()
    {
        var c = await NewUserClientAsync();
        var created = await Json(await c.PostAsJsonAsync("/api/finance/assets",
            new { name = "Sổ tiết kiệm", type = "bank", value = 5000000, note = "VCB" }));
        var id = created.GetProperty("id").GetString();
        Assert.Equal("bank", created.GetProperty("type").GetString());
        Assert.Equal(5000000, created.GetProperty("value").GetDecimal());

        var list = await Json(await c.GetAsync("/api/finance/assets"));
        Assert.Equal(1, list.GetArrayLength());

        var updated = await Json(await c.PutAsJsonAsync($"/api/finance/assets/{id}",
            new { name = "Sổ tiết kiệm", type = "bank", value = 6000000, note = (string?)null }));
        Assert.Equal(6000000, updated.GetProperty("value").GetDecimal());

        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/finance/assets/{id}")).StatusCode);
        Assert.Equal(0, (await Json(await c.GetAsync("/api/finance/assets"))).GetArrayLength());
    }

    [Fact]
    public async Task NetWorth_TongTaiSan_GopTheoLoai()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/finance/assets", new { name = "Ví", type = "cash", value = 100, note = (string?)null });
        await c.PostAsJsonAsync("/api/finance/assets", new { name = "Nhẫn", type = "gold", value = 50, note = (string?)null });
        await c.PostAsJsonAsync("/api/finance/assets", new { name = "Két", type = "cash", value = 25, note = (string?)null });

        var nw = await Json(await c.GetAsync("/api/finance/networth"));
        Assert.Equal(175, nw.GetProperty("netWorth").GetDecimal());

        var byType = nw.GetProperty("byType").EnumerateArray().ToList();
        var cash = byType.First(g => g.GetProperty("type").GetString() == "cash");
        Assert.Equal(125, cash.GetProperty("total").GetDecimal());
        Assert.Equal(6, nw.GetProperty("cashFlow").GetArrayLength());
    }

    [Fact]
    public async Task NetWorth_SavingRate_TheoThangThamChieu()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "income", amount = 1000000, date = "2026-05-05", note = (string?)null });
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 400000, date = "2026-05-20", note = (string?)null });

        var nw = await Json(await c.GetAsync("/api/finance/networth?month=2026-05-10"));
        Assert.Equal(1000000, nw.GetProperty("monthIncome").GetDecimal());
        Assert.Equal(400000, nw.GetProperty("monthExpense").GetDecimal());
        Assert.Equal(60, nw.GetProperty("savingRate").GetInt32()); // (1tr-400k)/1tr = 60%

        var flow = nw.GetProperty("cashFlow").EnumerateArray().ToList();
        Assert.Equal("2026-05", flow[^1].GetProperty("month").GetString()); // tháng tham chiếu là cuối
        Assert.Equal(600000, flow[^1].GetProperty("net").GetDecimal());
    }

    [Fact]
    public async Task Asset_LoaiKhongHopLe_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/finance/assets",
            new { name = "X", type = "nft", value = 1, note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Asset_CoLap_VaChuaDangNhap401()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();
        var created = await Json(await a.PostAsJsonAsync("/api/finance/assets",
            new { name = "của A", type = "cash", value = 9, note = (string?)null }));
        var id = created.GetProperty("id").GetString();

        Assert.Equal(0, (await Json(await b.GetAsync("/api/finance/assets"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/finance/assets/{id}")).StatusCode);

        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/finance/networth")).StatusCode);
    }
}
