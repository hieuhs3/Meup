using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

/// <summary>Tổng kết tuần do AI tạo. Enabled=false khi chưa cấu hình API key.</summary>
public record WeeklyInsightDto(bool Enabled, string? Summary, DateOnly From, DateOnly To);

/// <summary>
/// Trạng thái AI cho người dùng hiện tại. <paramref name="Enabled"/> = AI dùng được;
/// <paramref name="HasUserKey"/> = user đã tự đặt key; <paramref name="UsingServerKey"/> = đang dùng key chung của server.
/// </summary>
public record AiStatusDto(bool Enabled, bool HasUserKey, bool UsingServerKey);

/// <summary>Yêu cầu lưu API key Claude của riêng người dùng.</summary>
public record SetAiKeyRequest(
    [Required(ErrorMessage = "API key là bắt buộc.")]
    string ApiKey);

public record CategorizeRequest(
    [Required(ErrorMessage = "Ghi chú là bắt buộc.")]
    string Note,
    [RegularExpression("income|expense", ErrorMessage = "Loại không hợp lệ.")]
    string Type);

/// <summary>Gợi ý danh mục cho một giao dịch. Enabled=false khi chưa cấu hình.</summary>
public record CategorySuggestionDto(bool Enabled, Guid? CategoryId, string? CategoryName);
