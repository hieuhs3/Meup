using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace MeUp.Api.Services;

/// <summary>Dựng <see cref="UserProfileDto"/> đầy đủ (role, mật khẩu, provider) từ một user.</summary>
public static class UserProfileFactory
{
    public static async Task<UserProfileDto> BuildAsync(UserManager<ApplicationUser> users, ApplicationUser user)
    {
        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? Roles.User;
        var hasPassword = await users.HasPasswordAsync(user);
        var providers = (await users.GetLoginsAsync(user))
            .Select(l => l.LoginProvider)
            .ToList();

        return new UserProfileDto(
            user.Id,
            user.Email ?? "",
            user.DisplayName,
            role,
            user.CreatedAt,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Gender,
            user.Bio,
            user.AvatarUrl,
            user.TimeZone,
            user.Locale,
            user.TwoFactorEnabled,
            hasPassword,
            providers);
    }
}
