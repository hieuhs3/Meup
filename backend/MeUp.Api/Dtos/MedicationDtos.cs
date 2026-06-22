using System.ComponentModel.DataAnnotations;

namespace MeUp.Api.Dtos;

public record MedicationDto(
    Guid Id,
    string Name,
    string? Dosage,
    string? Note,
    DateOnly Date,
    bool Taken,
    DateTime CreatedAt);

public record CreateMedicationRequest(
    [Required(ErrorMessage = "Tên thuốc là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thuốc tối đa 150 ký tự.")]
    string Name,
    [MaxLength(100)] string? Dosage,
    [MaxLength(500)] string? Note);

public record UpdateMedicationRequest(
    [Required(ErrorMessage = "Tên thuốc là bắt buộc.")]
    [MaxLength(150, ErrorMessage = "Tên thuốc tối đa 150 ký tự.")]
    string Name,
    [MaxLength(100)] string? Dosage,
    [MaxLength(500)] string? Note);
