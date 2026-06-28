using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record JournalEntryDto(
    Guid Id,
    DateOnly Date,
    string? Title,
    string ContentHtml,
    string? Mood,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpsertJournalRequest(
    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string? Title,

    string? ContentHtml,

    [RegularExpression("excellent|good|normal|bad|terrible", ErrorMessage = "Tâm trạng không hợp lệ.")]
    string? Mood = null);

/// <summary>Một điểm xu hướng tâm trạng: ngày + mood + điểm 1–5.</summary>
public record MoodTrendPointDto(DateOnly Date, string Mood, int Score);
