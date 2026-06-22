using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record NoteDto(Guid Id, string Content, DateTime CreatedAt, DateTime UpdatedAt);

public record UpsertNoteRequest(
    [Required(ErrorMessage = "Nội dung ghi chú là bắt buộc.")]
    [MaxLength(2000, ErrorMessage = "Ghi chú tối đa 2000 ký tự.")]
    string Content);
