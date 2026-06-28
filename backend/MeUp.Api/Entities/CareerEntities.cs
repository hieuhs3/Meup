namespace MeUp.Api.Entities;

/// <summary>Kỹ năng của người dùng (cô lập theo UserId).</summary>
public class Skill
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Nhóm kỹ năng (tùy chọn), vd "Backend", "Cloud".</summary>
    public string? Category { get; set; }

    /// <summary>Mức thành thạo 1–5.</summary>
    public int Level { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Chứng chỉ của người dùng.</summary>
public class Certification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Đơn vị cấp (vd AWS, Microsoft).</summary>
    public string? Issuer { get; set; }

    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Dự án trong hồ sơ sự nghiệp (khác Goal/Task — đây là dự án đã/đang làm).</summary>
public class CareerProject
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Vai trò (vd Backend Dev, Tech Lead).</summary>
    public string? Role { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartedAt { get; set; }
    public DateOnly? EndedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
