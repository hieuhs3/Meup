namespace MeUp.Api.Services;

public interface IDailyReportService
{
    /// <summary>Tổng hợp & gửi email báo cáo ngày cho 1 user (chống trùng theo ngày). Trả true nếu vừa gửi.</summary>
    Task<bool> SendForUserAsync(Guid userId, DateOnly date);

    /// <summary>Quét các user đã bật, gửi báo cáo nếu giờ địa phương của họ là 21:00 (gọi bởi background service).</summary>
    Task RunDueAsync(DateTime utcNow);
}
