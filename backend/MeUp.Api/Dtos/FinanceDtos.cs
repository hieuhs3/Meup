using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

// --- Danh mục ---

public record CategoryDto(Guid Id, string Name, string Type, string? Color);

public record CreateCategoryRequest(
    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Tên danh mục tối đa 50 ký tự.")]
    string Name,

    [Required(ErrorMessage = "Loại là bắt buộc.")]
    [RegularExpression("income|expense", ErrorMessage = "Loại không hợp lệ.")]
    string Type,

    [MaxLength(7)]
    string? Color);

public record UpdateCategoryRequest(
    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Tên danh mục tối đa 50 ký tự.")]
    string Name,

    [MaxLength(7)]
    string? Color);

// --- Giao dịch ---

public record TransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    DateOnly Date,
    string? Note,
    DateTime CreatedAt);

public record CreateTransactionRequest(
    [Required(ErrorMessage = "Loại là bắt buộc.")]
    [RegularExpression("income|expense", ErrorMessage = "Loại không hợp lệ.")]
    string Type,

    [Range(0.01, 1_000_000_000_000, ErrorMessage = "Số tiền phải lớn hơn 0.")]
    decimal Amount,

    Guid? CategoryId,

    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? Note);

public record UpdateTransactionRequest(
    [Required(ErrorMessage = "Loại là bắt buộc.")]
    [RegularExpression("income|expense", ErrorMessage = "Loại không hợp lệ.")]
    string Type,

    [Range(0.01, 1_000_000_000_000, ErrorMessage = "Số tiền phải lớn hơn 0.")]
    decimal Amount,

    Guid? CategoryId,

    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? Note);

/// <summary>Kết quả danh sách giao dịch có phân trang.</summary>
public record TransactionListDto(IReadOnlyList<TransactionDto> Items, int Total, int Page, int PageSize);

/// <summary>Số dư + tổng thu/chi theo ngày và theo tháng của <see cref="Date"/>.</summary>
public record SummaryDto(
    DateOnly Date,
    decimal Balance,
    decimal DayIncome,
    decimal DayExpense,
    decimal MonthIncome,
    decimal MonthExpense);

// --- Ngân sách (A1) ---

/// <summary>Ngân sách 1 danh mục + mức đã chi trong tháng tham chiếu.</summary>
public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string? CategoryColor,
    decimal Amount,
    decimal Spent,
    decimal Remaining,
    int Percent);

public record CreateBudgetRequest(
    [Required] Guid CategoryId,
    [Range(0.01, 1_000_000_000_000, ErrorMessage = "Hạn mức phải lớn hơn 0.")] decimal Amount);

public record UpdateBudgetRequest(
    [Range(0.01, 1_000_000_000_000, ErrorMessage = "Hạn mức phải lớn hơn 0.")] decimal Amount);
