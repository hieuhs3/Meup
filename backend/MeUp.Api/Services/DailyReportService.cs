using System.Globalization;
using MeUp.Api.Data;
using MeUp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MeUp.Api.Services;

/// <summary>
/// Tổng hợp dữ liệu trong ngày (tài chính, sức khỏe, công việc) và gửi email báo cáo cuối ngày.
/// Tái dùng IEmailSender (SMTP/log) + INotificationService (chống trùng + lưu in-app) của MeUp.
/// </summary>
public class DailyReportService : IDailyReportService
{
    public const int ReportHour = 21; // 21:00 theo múi giờ của user

    private static readonly CultureInfo Vn = CultureInfo.GetCultureInfo("vi-VN");
    /// <summary>Định dạng tiền VND có dấu "." phân tách nghìn (đồng bộ với frontend), vd 1.000.000đ.</summary>
    private static string Money(decimal v) => v.ToString("#,##0", Vn) + "đ";

    private readonly AppDbContext _db;
    private readonly IStatsService _stats;
    private readonly INotificationService _notifications;
    private readonly IEmailSender _email;
    private readonly ILogger<DailyReportService> _logger;

    private static readonly TimeZoneInfo DefaultTz = ResolveDefaultTz();

    public DailyReportService(
        AppDbContext db,
        IStatsService stats,
        INotificationService notifications,
        IEmailSender email,
        ILogger<DailyReportService> logger)
    {
        _db = db;
        _stats = stats;
        _notifications = notifications;
        _email = email;
        _logger = logger;
    }

    public async Task<bool> SendForUserAsync(Guid userId, DateOnly date)
    {
        var stats = await _stats.GetStatsAsync(userId, date, date);
        var overdue = await _db.Tasks.CountAsync(t =>
            t.UserId == userId && !t.IsDone && t.DueDate != null && t.DueDate < date);
        var eventsToday = await _db.CalendarEvents.CountAsync(e => e.UserId == userId && e.Date == date);

        // Chống trùng: tạo bản ghi in-app với dedupKey theo ngày. null = đã gửi hôm nay → bỏ qua.
        var summary =
            $"Thu {Money(stats.Finance.TotalIncome)} · Chi {Money(stats.Finance.TotalExpense)} · " +
            $"{stats.Work.TasksDone}/{stats.Work.TasksTotal} việc xong.";
        var note = await _notifications.CreateAsync(
            userId, "report", "Báo cáo cuối ngày", summary, "/app/stats",
            dedupKey: $"dailyreport:{date:yyyy-MM-dd}");
        if (note is null) return false;

        var email = await _db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(email)) return false;

        var html = BuildHtml(date, stats, overdue, eventsToday);
        await _email.SendAsync(email, $"MeUp — Báo cáo ngày {date:dd/MM/yyyy}", html);
        return true;
    }

    public async Task RunDueAsync(DateTime utcNow)
    {
        var users = await _db.Users
            .Where(u => u.DailyReportEnabled)
            .Select(u => new { u.Id, u.TimeZone })
            .ToListAsync();

        foreach (var u in users)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, ResolveTz(u.TimeZone));
            if (local.Hour != ReportHour) continue;
            try { await SendForUserAsync(u.Id, DateOnly.FromDateTime(local)); }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi gửi báo cáo cuối ngày cho {UserId}.", u.Id); }
        }
    }

    private static string BuildHtml(DateOnly date, StatsDto s, int overdue, int eventsToday)
    {
        var f = s.Finance;
        var h = s.Health;
        var w = s.Work;
        var net = f.TotalIncome - f.TotalExpense;
        string row(string label, string value) =>
            $"<tr><td style=\"padding:6px 0;color:#76808f\">{label}</td>" +
            $"<td style=\"padding:6px 0;text-align:right;font-weight:600\">{value}</td></tr>";

        return $@"
<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;color:#1f2733"">
  <h2 style=""color:#4361ee;margin:0 0 4px"">Báo cáo ngày {date:dd/MM/yyyy}</h2>
  <p style=""color:#76808f;margin:0 0 16px"">Tổng kết một ngày của bạn trên MeUp.</p>

  <h3 style=""margin:18px 0 6px"">💰 Tài chính</h3>
  <table style=""width:100%;border-collapse:collapse"">
    {row("Tổng thu", Money(f.TotalIncome))}
    {row("Tổng chi", Money(f.TotalExpense))}
    {row("Chênh lệch", Money(net))}
  </table>

  <h3 style=""margin:18px 0 6px"">✓ Công việc</h3>
  <table style=""width:100%;border-collapse:collapse"">
    {row("Việc hoàn thành", $"{w.TasksDone}/{w.TasksTotal}")}
    {row("Việc quá hạn", overdue.ToString())}
    {row("Lượt check thói quen", w.HabitChecks.ToString())}
  </table>

  <h3 style=""margin:18px 0 6px"">♥ Sức khỏe</h3>
  <table style=""width:100%;border-collapse:collapse"">
    {row("Có ghi nhật ký", h.Days > 0 ? "Có" : "Chưa")}
    {row("Cân nặng", h.AvgWeight is null ? "—" : $"{h.AvgWeight}kg")}
    {row("Giờ ngủ", h.AvgSleep is null ? "—" : $"{h.AvgSleep}h")}
  </table>

  <h3 style=""margin:18px 0 6px"">📅 Lịch</h3>
  <p style=""margin:0"">{(eventsToday > 0 ? $"{eventsToday} sự kiện trong ngày." : "Không có sự kiện.")}</p>

  <p style=""margin:22px 0 0;color:#76808f;font-size:13px"">
    Bạn nhận email này vì đã bật ""Báo cáo cuối ngày"" trong Cài đặt MeUp. Tắt bất cứ lúc nào ở mục Cài đặt.
  </p>
</div>";
    }

    private static TimeZoneInfo ResolveTz(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* id không hợp lệ → dùng mặc định */ }
        }
        return DefaultTz;
    }

    private static TimeZoneInfo ResolveDefaultTz()
    {
        foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* thử id kế tiếp */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
    }
}
