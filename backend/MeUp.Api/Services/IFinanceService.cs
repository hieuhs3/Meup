using MeUp.Api.Dtos;

namespace MeUp.Api.Services;

/// <summary>Kết quả thao tác có thể lỗi nghiệp vụ (vd danh mục không hợp lệ).</summary>
public record FinanceResult<T>(T? Value, string? Error = null, bool NotFound = false)
{
    public bool Succeeded => Error is null && !NotFound;
    public static FinanceResult<T> Ok(T value) => new(value);
    public static FinanceResult<T> Fail(string error) => new(default, error);
    public static FinanceResult<T> Missing() => new(default, null, true);
}

public interface IFinanceService
{
    // Danh mục
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid userId, string? type);
    Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request);
    Task<CategoryDto?> UpdateCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(Guid userId, Guid id);

    // Giao dịch
    Task<TransactionListDto> GetTransactionsAsync(
        Guid userId, DateOnly? from, DateOnly? to, string? type, Guid? categoryId, string? q, int page, int pageSize);
    Task<TransactionDto?> GetTransactionAsync(Guid userId, Guid id);
    Task<FinanceResult<TransactionDto>> CreateTransactionAsync(Guid userId, CreateTransactionRequest request);
    Task<FinanceResult<TransactionDto>> UpdateTransactionAsync(Guid userId, Guid id, UpdateTransactionRequest request);
    Task<bool> DeleteTransactionAsync(Guid userId, Guid id);

    // Tổng hợp
    Task<SummaryDto> GetSummaryAsync(Guid userId, DateOnly date);

    // Ngân sách (A1) — month: ngày bất kỳ trong tháng tham chiếu để tính "đã chi"
    Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(Guid userId, DateOnly month);
    Task<FinanceResult<BudgetDto>> CreateBudgetAsync(Guid userId, CreateBudgetRequest request, DateOnly month);
    Task<BudgetDto?> UpdateBudgetAsync(Guid userId, Guid id, UpdateBudgetRequest request, DateOnly month);
    Task<bool> DeleteBudgetAsync(Guid userId, Guid id);
}
