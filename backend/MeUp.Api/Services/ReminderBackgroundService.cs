namespace MeUp.Api.Services;

/// <summary>Định kỳ sinh nhắc digest cho tất cả người dùng (mỗi 30 phút).</summary>
public class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private readonly IServiceProvider _sp;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceProvider sp, ILogger<ReminderBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ app khởi động ổn định.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IReminderService>();
                await reminders.GenerateForAllAsync(DateOnly.FromDateTime(DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sinh nhắc tự động.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
