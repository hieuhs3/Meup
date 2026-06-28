using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

// --- Skill ---

public record SkillDto(Guid Id, string Name, string? Category, int Level, DateTime CreatedAt);

public record SaveSkillRequest(
    [Required(ErrorMessage = "Tên kỹ năng là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Tên kỹ năng tối đa 100 ký tự.")]
    string Name,

    [MaxLength(50, ErrorMessage = "Nhóm tối đa 50 ký tự.")]
    string? Category,

    [Range(1, 5, ErrorMessage = "Mức thành thạo từ 1 đến 5.")]
    int Level);

// --- Certification ---

public record CertificationDto(
    Guid Id, string Name, string? Issuer, DateOnly? IssuedAt, DateOnly? ExpiresAt, DateTime CreatedAt);

public record SaveCertificationRequest(
    [Required(ErrorMessage = "Tên chứng chỉ là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên chứng chỉ tối đa 150 ký tự.")]
    string Name,

    [MaxLength(100, ErrorMessage = "Đơn vị cấp tối đa 100 ký tự.")]
    string? Issuer,

    DateOnly? IssuedAt,
    DateOnly? ExpiresAt);

// --- Career Project ---

public record CareerProjectDto(
    Guid Id, string Name, string? Role, string? Description, DateOnly? StartedAt, DateOnly? EndedAt, DateTime CreatedAt);

public record SaveCareerProjectRequest(
    [Required(ErrorMessage = "Tên dự án là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên dự án tối đa 150 ký tự.")]
    string Name,

    [MaxLength(100, ErrorMessage = "Vai trò tối đa 100 ký tự.")]
    string? Role,

    [MaxLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    string? Description,

    DateOnly? StartedAt,
    DateOnly? EndedAt);
