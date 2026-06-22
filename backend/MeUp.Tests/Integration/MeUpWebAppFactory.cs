using MeUp.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeUp.Tests.Integration;

/// <summary>
/// Chạy toàn bộ API trong bộ nhớ (TestServer) trên database test riêng <c>meup_test</c>
/// (Postgres ở cổng 5433 do docker-compose cung cấp). Thay IGoogleTokenValidator bằng fake.
/// </summary>
public class MeUpWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Port=5433;Database=meup_test;Username=meup;Password=meup_dev_password",
                ["Authentication:Google:ClientId"] = "test-client-id",
                // Ép tắt AI trong test để không phụ thuộc key cấu hình ở máy local
                // (user-secrets/appsettings). Các test nhánh "AI tắt" nhờ vậy luôn xác định.
                ["Ai:ApiKey"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, CapturingEmailSender>();
        });
    }
}
