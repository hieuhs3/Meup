using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

/// <summary>Tham chiếu ngắn tới một note (dùng cho backlinks).</summary>
public record NoteRefDto(Guid Id, string Title);

public record NoteDto(
    Guid Id,
    string? Title,
    string Content,
    string? Category,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> OutLinks,
    IReadOnlyList<NoteRefDto> Backlinks,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpsertNoteRequest(
    [Required(ErrorMessage = "Nội dung ghi chú là bắt buộc.")]
    [MaxLength(5000, ErrorMessage = "Ghi chú tối đa 5000 ký tự.")]
    string Content,

    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
    string? Title = null,

    [MaxLength(50, ErrorMessage = "Nhóm tối đa 50 ký tự.")]
    string? Category = null,

    List<string>? Tags = null);
