using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

public class FinanceService : IFinanceService
{
    private readonly AppDbContext _db;

    public FinanceService(AppDbContext db) => _db = db;

    /// <summary>Bộ danh mục mặc định tạo lần đầu cho người dùng.</summary>
    private static readonly (string Name, string Type, string Color)[] Defaults =
    [
        ("Ăn uống", FinanceType.Expense, "#ef476f"),
        ("Đi lại", FinanceType.Expense, "#f78c6b"),
        ("Mua sắm", FinanceType.Expense, "#ffd166"),
        ("Hóa đơn", FinanceType.Expense, "#06d6a0"),
        ("Giải trí", FinanceType.Expense, "#118ab2"),
        ("Sức khỏe", FinanceType.Expense, "#8338ec"),
        ("Khác", FinanceType.Expense, "#8d99ae"),
        ("Lương", FinanceType.Income, "#1f9d55"),
        ("Thưởng", FinanceType.Income, "#4361ee"),
        ("Đầu tư", FinanceType.Income, "#3a0ca3"),
        ("Khác", FinanceType.Income, "#8d99ae"),
    ];

    // --- Danh mục ---

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid userId, string? type)
    {
        if (!await _db.Categories.AnyAsync(c => c.UserId == userId))
            await SeedDefaultsAsync(userId);

        var query = _db.Categories.Where(c => c.UserId == userId);
        if (FinanceType.IsValid(type))
            query = query.Where(c => c.Type == type);

        return await query
            .OrderBy(c => c.Type).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color))
            .ToListAsync();
    }

    private async Task SeedDefaultsAsync(Guid userId)
    {
        foreach (var (name, type, color) in Defaults)
            _db.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Type = type,
                Color = color,
            });
        await _db.SaveChangesAsync();
    }

    public async Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Color = request.Color,
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return new CategoryDto(category.Id, category.Name, category.Type, category.Color);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(Guid userId, Guid id, UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null) return null;

        category.Name = request.Name.Trim();
        category.Color = request.Color;
        await _db.SaveChangesAsync();
        return new CategoryDto(category.Id, category.Name, category.Type, category.Color);
    }

    public async Task<bool> DeleteCategoryAsync(Guid userId, Guid id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category is null) return false;

        // Giao dịch liên quan được gỡ liên kết qua FK SET NULL.
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    // --- Giao dịch ---

    public async Task<TransactionListDto> GetTransactionsAsync(
        Guid userId, DateOnly? from, DateOnly? to, string? type, Guid? categoryId, string? q, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Transactions.Where(t => t.UserId == userId);
        if (from is not null) query = query.Where(t => t.Date >= from);
        if (to is not null) query = query.Where(t => t.Date <= to);
        if (FinanceType.IsValid(type)) query = query.Where(t => t.Type == type);
        if (categoryId is not null) query = query.Where(t => t.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => t.Note != null && EF.Functions.ILike(t.Note, $"%{q}%"));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id, t.Type, t.Amount, t.CategoryId,
                t.Category == null ? null : t.Category.Name,
                t.Category == null ? null : t.Category.Color,
                t.Date, t.Note, t.CreatedAt))
            .ToListAsync();

        return new TransactionListDto(items, total, page, pageSize);
    }

    public async Task<TransactionDto?> GetTransactionAsync(Guid userId, Guid id)
    {
        return await _db.Transactions
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new TransactionDto(
                t.Id, t.Type, t.Amount, t.CategoryId,
                t.Category == null ? null : t.Category.Name,
                t.Category == null ? null : t.Category.Color,
                t.Date, t.Note, t.CreatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<FinanceResult<TransactionDto>> CreateTransactionAsync(Guid userId, CreateTransactionRequest request)
    {
        var error = await ValidateCategoryAsync(userId, request.CategoryId, request.Type);
        if (error is not null) return FinanceResult<TransactionDto>.Fail(error);

        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        return FinanceResult<TransactionDto>.Ok((await GetTransactionAsync(userId, tx.Id))!);
    }

    public async Task<FinanceResult<TransactionDto>> UpdateTransactionAsync(Guid userId, Guid id, UpdateTransactionRequest request)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tx is null) return FinanceResult<TransactionDto>.Missing();

        var error = await ValidateCategoryAsync(userId, request.CategoryId, request.Type);
        if (error is not null) return FinanceResult<TransactionDto>.Fail(error);

        tx.Type = request.Type;
        tx.Amount = request.Amount;
        tx.CategoryId = request.CategoryId;
        tx.Date = request.Date;
        tx.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await _db.SaveChangesAsync();

        return FinanceResult<TransactionDto>.Ok((await GetTransactionAsync(userId, tx.Id))!);
    }

    public async Task<bool> DeleteTransactionAsync(Guid userId, Guid id)
    {
        var tx = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (tx is null) return false;

        _db.Transactions.Remove(tx);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Kiểm tra danh mục (nếu có) thuộc về user và cùng loại với giao dịch. Trả thông báo lỗi hoặc null.</summary>
    private async Task<string?> ValidateCategoryAsync(Guid userId, Guid? categoryId, string type)
    {
        if (categoryId is null) return null;

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);
        if (category is null) return "Danh mục không hợp lệ.";
        if (category.Type != type) return "Danh mục không cùng loại với giao dịch.";
        return null;
    }

    // --- Tổng hợp ---

    public async Task<SummaryDto> GetSummaryAsync(Guid userId, DateOnly date)
    {
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var mine = _db.Transactions.Where(t => t.UserId == userId);

        async Task<decimal> Sum(IQueryable<Transaction> q, string type) =>
            await q.Where(t => t.Type == type).SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var totalIncome = await Sum(mine, FinanceType.Income);
        var totalExpense = await Sum(mine, FinanceType.Expense);

        var day = mine.Where(t => t.Date == date);
        var month = mine.Where(t => t.Date >= monthStart && t.Date <= monthEnd);

        return new SummaryDto(
            Date: date,
            Balance: totalIncome - totalExpense,
            DayIncome: await Sum(day, FinanceType.Income),
            DayExpense: await Sum(day, FinanceType.Expense),
            MonthIncome: await Sum(month, FinanceType.Income),
            MonthExpense: await Sum(month, FinanceType.Expense));
    }

    // --- Ngân sách (A1) ---

    public async Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(Guid userId, DateOnly month)
    {
        var budgets = await _db.Budgets
            .Where(b => b.UserId == userId)
            .Include(b => b.Category)
            .ToListAsync();
        if (budgets.Count == 0) return [];

        var (start, end) = MonthRange(month);
        var catIds = budgets.Select(b => b.CategoryId).ToList();
        var spent = (await _db.Transactions
                .Where(t => t.UserId == userId && t.Type == FinanceType.Expense
                            && t.Date >= start && t.Date <= end
                            && t.CategoryId != null && catIds.Contains(t.CategoryId.Value))
                .GroupBy(t => t.CategoryId!.Value)
                .Select(g => new { CatId = g.Key, Sum = g.Sum(x => x.Amount) })
                .ToListAsync())
            .ToDictionary(x => x.CatId, x => x.Sum);

        return budgets
            .Select(b => ToBudgetDto(b, spent.GetValueOrDefault(b.CategoryId, 0m)))
            .OrderBy(b => b.CategoryName)
            .ToList();
    }

    public async Task<FinanceResult<BudgetDto>> CreateBudgetAsync(Guid userId, CreateBudgetRequest request, DateOnly month)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId);
        if (category is null) return FinanceResult<BudgetDto>.Fail("Danh mục không hợp lệ.");
        if (category.Type != FinanceType.Expense) return FinanceResult<BudgetDto>.Fail("Chỉ đặt ngân sách cho danh mục chi.");
        if (await _db.Budgets.AnyAsync(b => b.UserId == userId && b.CategoryId == request.CategoryId))
            return FinanceResult<BudgetDto>.Fail("Danh mục này đã có ngân sách.");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
        };
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        budget.Category = category;
        return FinanceResult<BudgetDto>.Ok(ToBudgetDto(budget, await SpentAsync(userId, request.CategoryId, month)));
    }

    public async Task<BudgetDto?> UpdateBudgetAsync(Guid userId, Guid id, UpdateBudgetRequest request, DateOnly month)
    {
        var budget = await _db.Budgets.Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget is null) return null;

        budget.Amount = request.Amount;
        await _db.SaveChangesAsync();
        return ToBudgetDto(budget, await SpentAsync(userId, budget.CategoryId, month));
    }

    public async Task<bool> DeleteBudgetAsync(Guid userId, Guid id)
    {
        var budget = await _db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget is null) return false;
        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<decimal> SpentAsync(Guid userId, Guid categoryId, DateOnly month)
    {
        var (start, end) = MonthRange(month);
        return await _db.Transactions
            .Where(t => t.UserId == userId && t.Type == FinanceType.Expense
                        && t.CategoryId == categoryId && t.Date >= start && t.Date <= end)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
    }

    private static (DateOnly start, DateOnly end) MonthRange(DateOnly month)
    {
        var start = new DateOnly(month.Year, month.Month, 1);
        return (start, start.AddMonths(1).AddDays(-1));
    }

    private static BudgetDto ToBudgetDto(Budget b, decimal spent)
    {
        var percent = b.Amount > 0 ? (int)Math.Round(spent / b.Amount * 100) : 0;
        return new BudgetDto(b.Id, b.CategoryId, b.Category?.Name ?? "", b.Category?.Color,
            b.Amount, spent, b.Amount - spent, percent);
    }
}
