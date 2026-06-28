namespace MeUp.Api.Entities;

/// <summary>Tài sản của người dùng (cô lập theo UserId). Tổng giá trị dùng tính Net Worth.</summary>
public class Asset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Loại tài sản: cash|bank|stock|crypto|gold|other.</summary>
    public string Type { get; set; } = AssetType.Cash;

    /// <summary>Giá trị hiện tại.</summary>
    public decimal Value { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

/// <summary>Loại tài sản hợp lệ.</summary>
public static class AssetType
{
    public const string Cash = "cash";
    public const string Bank = "bank";
    public const string Stock = "stock";
    public const string Crypto = "crypto";
    public const string Gold = "gold";
    public const string Other = "other";

    public static bool IsValid(string? v) => v is Cash or Bank or Stock or Crypto or Gold or Other;
    public static string Normalize(string? v) => IsValid(v) ? v! : Cash;
}
