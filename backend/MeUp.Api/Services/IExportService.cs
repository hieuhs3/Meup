namespace MeUp.Api.Services;

public interface IExportService
{
    /// <summary>Gom toàn bộ dữ liệu của một người dùng để xuất ra (JSON).</summary>
    Task<object> ExportAsync(Guid userId);
}
