using System.Security.Claims;

namespace MeUp.Api.Services;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Lấy Id người dùng hiện tại từ claim. Ném lỗi nếu không có (chỉ gọi sau [Authorize]).</summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var guid)
            ? guid
            : throw new InvalidOperationException("Không tìm thấy Id người dùng trong token.");
    }
}
