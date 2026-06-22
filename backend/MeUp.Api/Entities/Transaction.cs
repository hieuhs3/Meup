namespace MeUp.Api.Entities;

/// <summary>Một giao dịch thu/chi của người dùng (cô lập theo UserId).</summary>
public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>"income" | "expense".</summary>
    public string Type { get; set; } = FinanceType.Expense;

    /// <summary>Số tiền (> 0), VND.</summary>
    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>Ngày của giao dịch (không gồm giờ) — dùng để tổng theo ngày/tháng.</summary>
    public DateOnly Date { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public Category? Category { get; set; }
}
