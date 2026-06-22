namespace MeUp.Api.Entities;

/// <summary>Giá trị hợp lệ của loại giao dịch / danh mục.</summary>
public static class FinanceType
{
    public const string Income = "income";
    public const string Expense = "expense";

    public static bool IsValid(string? value) => value is Income or Expense;
}

/// <summary>Danh mục thu/chi của một người dùng (cô lập theo UserId).</summary>
public class Category
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>"income" | "expense".</summary>
    public string Type { get; set; } = FinanceType.Expense;

    /// <summary>Màu hiển thị, vd "#4361ee".</summary>
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
