namespace MeUp.Api.Entities;

/// <summary>Một loại thuốc người dùng cần uống (cô lập theo UserId).</summary>
public class Medication
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Liều lượng, vd "1 viên sáng".</summary>
    public string? Dosage { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<MedicationIntake> Intakes { get; set; } = new List<MedicationIntake>();
}

/// <summary>Đánh dấu đã uống một loại thuốc trong một ngày (sự hiện diện = đã uống).</summary>
public class MedicationIntake
{
    public Guid Id { get; set; }
    public Guid MedicationId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    public Medication? Medication { get; set; }
}
