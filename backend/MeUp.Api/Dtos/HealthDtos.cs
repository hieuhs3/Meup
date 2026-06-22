using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record HealthLogDto(
    DateOnly Date,
    decimal? Weight,
    decimal? SleepHours,
    int? WaterMl,
    int? WorkoutMinutes,
    string? Note,
    DateTime UpdatedAt);

public record UpsertHealthLogRequest(
    [Range(0, 500, ErrorMessage = "Cân nặng phải trong khoảng 0–500 kg.")]
    decimal? Weight,

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
