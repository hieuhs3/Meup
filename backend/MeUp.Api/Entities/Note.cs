namespace MeUp.Api.Entities;

/// <summary>Ghi chú nhanh, tự do (cô lập theo UserId).</summary>
public class Note
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
