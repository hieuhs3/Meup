using Microsoft.AspNetCore.Identity;

namespace MeUp.Api.Entities;

/// <summary>Vai trò người dùng (user / admin) với khóa kiểu Guid.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}

/// <summary>Tên các vai trò chuẩn trong hệ thống.</summary>
public static class Roles
{
    public const string User = "user";
    public const string Admin = "admin";
}
