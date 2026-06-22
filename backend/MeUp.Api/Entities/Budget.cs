namespace MeUp.Api.Entities;

/// <summary>Hạn mức chi hằng tháng cho một danh mục (cô lập theo UserId). Mỗi danh mục tối đa 1 ngân sách.</summary>
public class Budget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>Hạn mức chi mỗi tháng (VND).</summary>
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public Category? Category { get; set; }
}
