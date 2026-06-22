using MeUp.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Data;

/// <summary>Áp dụng migration và tạo dữ liệu khởi tạo: vai trò + tài khoản admin mặc định.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in new[] { Roles.User, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }

        // Tài khoản admin mặc định (cấu hình trong Seed:Admin). Chỉ tạo nếu chưa có.
        var adminEmail = config["Seed:Admin:Email"];
        var adminPassword = config["Seed:Admin:Password"];
        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    DisplayName = "Quản trị viên",
                    EmailConfirmed = true,
                };
                var created = await userManager.CreateAsync(admin, adminPassword);
                if (created.Succeeded)
                    await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
        }
    }
}
