using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(UserManager<ApplicationUser> users) => _users = users;

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers()
    {
        var users = await _users.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();

        var result = new List<AdminUserDto>(users.Count);
        foreach (var u in users)
        {
            var role = (await _users.GetRolesAsync(u)).FirstOrDefault() ?? Roles.User;
            result.Add(new AdminUserDto(u.Id, u.Email ?? "", u.DisplayName, role, u.IsLocked, u.CreatedAt));
        }
        return Ok(result);
    }

    /// <summary>Khóa/mở khóa một tài khoản. Admin không thể tự khóa chính mình.</summary>
    [HttpPost("users/{id:guid}/lock")]
    public async Task<IActionResult> ToggleLock(Guid id)
    {
        if (id == User.GetUserId())
            return BadRequest(new { error = "Bạn không thể tự khóa tài khoản của mình." });

        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsLocked = !user.IsLocked;
        await _users.UpdateAsync(user);

        return Ok(new { id = user.Id, isLocked = user.IsLocked });
    }
}
