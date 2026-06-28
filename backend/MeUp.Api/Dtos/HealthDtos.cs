using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record HealthLogDto(
    DateOnly Date,
    decimal? Weight,
    decimal? HeightCm,
    decimal? Bmi,
    decimal? SleepHours,
    int? WaterMl,
    int? WorkoutMinutes,
    string? Note,
    DateTime UpdatedAt);

public record UpsertHealthLogRequest(
    [Range(0, 500, ErrorMessage = "Cân nặng phải trong khoảng 0–500 kg.")]
    decimal? Weight,

    [Range(0, 300, ErrorMessage = "Chiều cao phải trong khoảng 0–300 cm.")]
    decimal? HeightCm,

    [Range(0, 24, ErrorMessage = "Giờ ngủ phải trong khoảng 0–24.")]
    decimal? SleepHours,

    [Range(0, 20000, ErrorMessage = "Lượng nước phải trong khoảng 0–20000 ml.")]
    int? WaterMl,

    [Range(0, 1440, ErrorMessage = "Thời gian tập phải trong khoảng 0–1440 phút.")]
    int? WorkoutMinutes,

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? Note);

/// <summary>Bản ghi của một ngày + bản ghi gần nhất trước đó (để so sánh).</summary>
public record HealthSummaryDto(DateOnly Date, HealthLogDto? Today, HealthLogDto? Previous);

// --- Hoạt động & Xu hướng (G5) ---

public record ActivityDto(
    Guid Id, DateOnly Date, string Type, int DurationMin, int? Calories, string? Note, DateTime CreatedAt);

public record SaveActivityRequest(
    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    [Required(ErrorMessage = "Loại là bắt buộc.")]
    [RegularExpression("running|walking|gym|swimming|cycling|other", ErrorMessage = "Loại hoạt động không hợp lệ.")]
    string Type,

    [Range(1, 1440, ErrorMessage = "Thời lượng từ 1 đến 1440 phút.")]
    int DurationMin,

    [Range(0, 100000, ErrorMessage = "Calo không hợp lệ.")]
    int? Calories,

    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? Note);

public record TrendPointDto(DateOnly Date, decimal? Value);

/// <summary>Xu hướng cân nặng + BMI + tổng calo theo ngày trong khoảng.</summary>
public record HealthTrendDto(
    IReadOnlyList<TrendPointDto> Weight,
    IReadOnlyList<TrendPointDto> Bmi,
    IReadOnlyList<TrendPointDto> Calories);
