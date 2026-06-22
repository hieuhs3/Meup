using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record JournalEntryDto(
    Guid Id,
    DateOnly Date,
    string? Title,
    string ContentHtml,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpsertJournalRequest(
    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string? Title,

    string? ContentHtml);
