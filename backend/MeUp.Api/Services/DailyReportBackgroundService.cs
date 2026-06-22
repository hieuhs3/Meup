namespace MeUp.Api.Services;

/// <summary>Định kỳ (mỗi 30 phút) kiểm tra và gửi email báo cáo cuối ngày cho user đã bật, lúc 21:00 giờ địa phương.</summary>
public class DailyReportBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private readonly IServiceProvider _sp;
    private readonly ILogger<DailyReportBackgroundService> _logger;

    public DailyReportBackgroundService(IServiceProvider sp, ILogger<DailyReportBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var reports = scope.ServiceProvider.GetRequiredService<IDailyReportService>();
                await reports.RunDueAsync(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi báo cáo cuối ngày tự động.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
