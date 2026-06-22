using MeUp.Api.Dtos;
using MeUp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeUp.Api.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _finance;

    public FinanceController(IFinanceService finance) => _finance = finance;

    private Guid UserId => User.GetUserId();

    // --- Danh mục ---

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? type)
        => Ok(await _finance.GetCategoriesAsync(UserId, type));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
        => Ok(await _finance.CreateCategoryAsync(UserId, request));

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryRequest request)
    {
        var dto = await _finance.UpdateCategoryAsync(UserId, id, request);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
        => await _finance.DeleteCategoryAsync(UserId, id) ? NoContent() : NotFound();

    // --- Giao dịch ---

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? type,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _finance.GetTransactionsAsync(UserId, from, to, type, categoryId, q, page, pageSize));

    [HttpGet("transactions/{id:guid}")]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        var dto = await _finance.GetTransactionAsync(UserId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction(CreateTransactionRequest request)
    {
        var result = await _finance.CreateTransactionAsync(UserId, request);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("transactions/{id:guid}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, UpdateTransactionRequest request)
    {
        var result = await _finance.UpdateTransactionAsync(UserId, id, request);
        if (result.NotFound) return NotFound();
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("transactions/{id:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
        => await _finance.DeleteTransactionAsync(UserId, id) ? NoContent() : NotFound();

    // --- Tổng hợp ---

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? date)
        => Ok(await _finance.GetSummaryAsync(UserId, date ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    // --- Ngân sách (A1) ---

    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudgets([FromQuery] DateOnly? month)
        => Ok(await _finance.GetBudgetsAsync(UserId, month ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    [HttpPost("budgets")]
    public async Task<IActionResult> CreateBudget(CreateBudgetRequest request, [FromQuery] DateOnly? month)
    {
        var result = await _finance.CreateBudgetAsync(UserId, request, month ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("budgets/{id:guid}")]
    public async Task<IActionResult> UpdateBudget(Guid id, UpdateBudgetRequest request, [FromQuery] DateOnly? month)
    {
        var dto = await _finance.UpdateBudgetAsync(UserId, id, request, month ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("budgets/{id:guid}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
        => await _finance.DeleteBudgetAsync(UserId, id) ? NoContent() : NotFound();
}
