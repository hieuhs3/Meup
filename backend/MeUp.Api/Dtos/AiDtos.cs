using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

/// <summary>Tổng kết tuần do AI tạo. Enabled=false khi chưa cấu hình API key.</summary>
public record WeeklyInsightDto(bool Enabled, string? Summary, DateOnly From, DateOnly To);

public record CategorizeRequest(
    [Required(ErrorMessage = "Ghi chú là bắt buộc.")]
    string Note,
    [RegularExpression("income|expense", ErrorMessage = "Loại không hợp lệ.")]
    string Type);

/// <summary>Gợi ý danh mục cho một giao dịch. Enabled=false khi chưa cấu hình.</summary>
public record CategorySuggestionDto(bool Enabled, Guid? CategoryId, string? CategoryName);
