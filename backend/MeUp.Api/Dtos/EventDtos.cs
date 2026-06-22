using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record EventDto(
    Guid Id,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string Title,
    string? Location,
    string? Note);

public record UpsertEventRequest(
    [Required(ErrorMessage = "Ngày là bắt buộc.")]
    DateOnly Date,

    TimeOnly? StartTime,
    TimeOnly? EndTime,

    [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string Title,

    [MaxLength(200)] string? Location,
    [MaxLength(1000)] string? Note);
