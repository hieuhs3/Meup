using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MeUp.Tests.Integration;

/// <summary>Integration test cho F1 (Tài chính): CRUD, cô lập theo user, lọc, tổng hợp.</summary>
public class F1FinanceTests : IClassFixture<MeUpWebAppFactory>
{
    private readonly MeUpWebAppFactory _factory;

    public F1FinanceTests(MeUpWebAppFactory factory) => _factory = factory;

    private const string Pwd = "Passw0rd!";
    private static string NewEmail() => $"f1_{Guid.NewGuid():N}@test.local";

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new { email = NewEmail(), password = Pwd, displayName = "Fin User" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage resp) =>
        await resp.Content.ReadFromJsonAsync<JsonElement>();

    // --- Danh mục ---

    [Fact]
    public async Task GetCategories_LanDau_TaoBoMacDinh()
    {
        var c = await NewUserClientAsync();

        var cats = await Json(await c.GetAsync("/api/finance/categories"));

        Assert.True(cats.GetArrayLength() > 0);
        var types = cats.EnumerateArray().Select(x => x.GetProperty("type").GetString()).Distinct().ToList();
        Assert.Contains("income", types);
        Assert.Contains("expense", types);
    }

    [Fact]
    public async Task DeleteCategory_GoLienKet_KhongXoaGiaoDich()
    {
        var c = await NewUserClientAsync();

        var cat = await Json(await c.PostAsJsonAsync("/api/finance/categories",
            new { name = "Test", type = "expense", color = "#123456" }));
        var catId = cat.GetProperty("id").GetString();

        var tx = await Json(await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 50000, categoryId = catId, date = "2026-03-10", note = "x" }));
        var txId = tx.GetProperty("id").GetString();

        var del = await c.DeleteAsync($"/api/finance/categories/{catId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Giao dịch vẫn còn nhưng categoryId = null.
        var got = await Json(await c.GetAsync($"/api/finance/transactions/{txId}"));
        Assert.Equal(JsonValueKind.Null, got.GetProperty("categoryId").ValueKind);
    }

    // --- Giao dịch + tổng hợp ---

    [Fact]
    public async Task CreateTransactions_Summary_TinhDungSoDu()
    {
        var c = await NewUserClientAsync();

        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "income", amount = 1_000_000, date = "2026-03-15", note = "Lương" });
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 200_000, date = "2026-03-15", note = "Chợ" });

        var s = await Json(await c.GetAsync("/api/finance/summary?date=2026-03-15"));

        Assert.Equal(800_000m, s.GetProperty("balance").GetDecimal());
        Assert.Equal(1_000_000m, s.GetProperty("dayIncome").GetDecimal());
        Assert.Equal(200_000m, s.GetProperty("dayExpense").GetDecimal());
        Assert.Equal(1_000_000m, s.GetProperty("monthIncome").GetDecimal());
        Assert.Equal(200_000m, s.GetProperty("monthExpense").GetDecimal());
    }

    [Fact]
    public async Task CreateTransaction_SoTienAm_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = -5, date = "2026-03-15", note = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_DanhMucKhongTonTai_400()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 1000, categoryId = Guid.NewGuid(), date = "2026-03-15" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_DanhMucKhacLoai_400()
    {
        var c = await NewUserClientAsync();
        var cat = await Json(await c.PostAsJsonAsync("/api/finance/categories",
            new { name = "Chi tiêu", type = "expense", color = (string?)null }));
        var catId = cat.GetProperty("id").GetString();

        // Dùng danh mục loại "expense" cho giao dịch "income" → lỗi.
        var resp = await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "income", amount = 1000, categoryId = catId, date = "2026-03-15" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Filter_TheoLoaiVaTuKhoa()
    {
        var c = await NewUserClientAsync();
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "income", amount = 500, date = "2026-04-01", note = "cà phê sáng" });
        await c.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 700, date = "2026-04-02", note = "taxi" });

        var byType = await Json(await c.GetAsync("/api/finance/transactions?type=income"));
        Assert.Equal(1, byType.GetProperty("total").GetInt32());
        Assert.Equal("income", byType.GetProperty("items")[0].GetProperty("type").GetString());

        var byQ = await Json(await c.GetAsync("/api/finance/transactions?q=cà"));
        Assert.Equal(1, byQ.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task UpdateTransaction_KhongTonTai_404()
    {
        var c = await NewUserClientAsync();
        var resp = await c.PutAsJsonAsync($"/api/finance/transactions/{Guid.NewGuid()}",
            new { type = "expense", amount = 1000, date = "2026-03-15" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Pagination_TraDungTongVaSoTrang()
    {
        var c = await NewUserClientAsync();
        for (var i = 0; i < 25; i++)
            await c.PostAsJsonAsync("/api/finance/transactions",
                new { type = "expense", amount = 1000 + i, date = "2026-05-10", note = $"n{i}" });

        var p1 = await Json(await c.GetAsync("/api/finance/transactions?page=1&pageSize=20"));
        Assert.Equal(25, p1.GetProperty("total").GetInt32());
        Assert.Equal(20, p1.GetProperty("items").GetArrayLength());

        var p2 = await Json(await c.GetAsync("/api/finance/transactions?page=2&pageSize=20"));
        Assert.Equal(5, p2.GetProperty("items").GetArrayLength());
    }

    // --- Cô lập theo người dùng ---

    [Fact]
    public async Task CoLap_NguoiKhacKhongThayVaKhongTruyCapDuoc()
    {
        var a = await NewUserClientAsync();
        var b = await NewUserClientAsync();

        var tx = await Json(await a.PostAsJsonAsync("/api/finance/transactions",
            new { type = "expense", amount = 9999, date = "2026-03-20", note = "bí mật" }));
        var txId = tx.GetProperty("id").GetString();

        // B không thấy giao dịch của A.
        var bList = await Json(await b.GetAsync("/api/finance/transactions"));
        Assert.Equal(0, bList.GetProperty("total").GetInt32());

        // B truy cập trực tiếp id của A → 404.
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/finance/transactions/{txId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.DeleteAsync($"/api/finance/transactions/{txId}")).StatusCode);

        // A vẫn truy cập được của mình.
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/finance/transactions/{txId}")).StatusCode);
    }

    [Fact]
    public async Task ChuaDangNhap_Bi401()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/finance/transactions")).StatusCode);
    }
}
