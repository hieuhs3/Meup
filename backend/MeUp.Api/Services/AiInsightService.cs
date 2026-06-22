using Anthropic;
using Anthropic.Models.Messages;
using MeUp.Api.Data;
using MeUp.Api.Dtos;
using MeUp.Api.Entities;
using MeUp.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeUp.Api.Services;

/// <summary>
/// Tính năng AI dùng Claude API (SDK Anthropic). Opus 4.8 cho tổng kết tuần, Haiku 4.5 cho phân loại.
/// Tắt an toàn (Enabled=false) khi chưa cấu hình Ai:ApiKey.
/// </summary>
public class AiInsightService : IAiInsightService
{
    private readonly AppDbContext _db;
    private readonly IStatsService _stats;
    private readonly ILogger<AiInsightService> _logger;
    private readonly AnthropicClient? _client;

    public AiInsightService(AppDbContext db, IStatsService stats, IOptions<AiOptions> opt, ILogger<AiInsightService> logger)
    {
        _db = db;
        _stats = stats;
        _logger = logger;
        if (opt.Value.IsConfigured)
            _client = new AnthropicClient { ApiKey = opt.Value.ApiKey };
    }

    public bool Enabled => _client is not null;

    public async Task<WeeklyInsightDto> GetWeeklyInsightAsync(Guid userId, DateOnly date, bool refresh = false)
    {
        var from = date.AddDays(-6);
        var to = date;
        if (_client is null) return new WeeklyInsightDto(false, null, from, to);

        // Đọc cache trước: nếu đã có tổng kết cho đúng khoảng tuần này thì trả luôn,
        // không gọi lại Claude (trừ khi yêu cầu tạo lại bằng refresh=true).
        var cached = await _db.WeeklyInsights
            .FirstOrDefaultAsync(w => w.UserId == userId && w.WeekFrom == from && w.WeekTo == to);
        if (cached is not null && !refresh)
            return new WeeklyInsightDto(true, cached.Summary, from, to);

        var stats = await _stats.GetStatsAsync(userId, from, to);
        try
        {
            var resp = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model.ClaudeOpus4_8,
                MaxTokens = 1500,
                Messages = [new() { Role = Role.User, Content = BuildWeeklyPrompt(stats, from, to) }],
            });
            var text = resp.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text).FirstOrDefault();

            // Lưu lại để lần sau không phải gọi Claude nữa. Chỉ cache khi có nội dung hợp lệ.
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (cached is null)
                    _db.WeeklyInsights.Add(new WeeklyInsight
                    {
                        UserId = userId, WeekFrom = from, WeekTo = to, Summary = text,
                    });
                else
                {
                    cached.Summary = text;
                    cached.CreatedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync();
            }
            return new WeeklyInsightDto(true, text, from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gọi Claude (weekly insight).");
            // Lỗi tạm thời: nếu đã có bản cache cũ thì trả lại, đỡ mất trải nghiệm.
            if (cached is not null)
                return new WeeklyInsightDto(true, cached.Summary, from, to);
            return new WeeklyInsightDto(true, "Không tạo được tổng kết lúc này. Vui lòng thử lại sau.", from, to);
        }
    }

    public async Task<CategorySuggestionDto> SuggestCategoryAsync(Guid userId, string note, string type)
    {
        if (_client is null) return new CategorySuggestionDto(false, null, null);

        var cats = await _db.Categories
            .Where(c => c.UserId == userId && c.Type == type)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        if (cats.Count == 0) return new CategorySuggestionDto(true, null, null);

        var list = string.Join("\n", cats.Select(c => $"{c.Id} = {c.Name}"));
        var prompt =
            "Bạn phân loại giao dịch tài chính. Danh sách danh mục (id = tên):\n" + list +
            $"\n\nGhi chú giao dịch: \"{note}\"\n\n" +
            "Chỉ trả về DUY NHẤT id của danh mục phù hợp nhất (không giải thích). Nếu không chắc, chọn id của danh mục \"Khác\" nếu có.";

        try
        {
            var resp = await _client.Messages.Create(new MessageCreateParams
            {
                Model = Model.ClaudeHaiku4_5,
                MaxTokens = 60,
                Messages = [new() { Role = Role.User, Content = prompt }],
            });
            var text = resp.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text).FirstOrDefault() ?? "";
            var match = cats.FirstOrDefault(c => text.Contains(c.Id.ToString()));
            return match is null
                ? new CategorySuggestionDto(true, null, null)
                : new CategorySuggestionDto(true, match.Id, match.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gọi Claude (categorize).");
            return new CategorySuggestionDto(true, null, null);
        }
    }

    private static string BuildWeeklyPrompt(StatsDto s, DateOnly from, DateOnly to)
    {
        var f = s.Finance;
        var h = s.Health;
        var w = s.Work;
        var cats = string.Join(", ", f.ByCategory.Take(6).Select(c => $"{c.Name} ({(c.Type == "income" ? "thu" : "chi")}): {c.Amount:0}đ"));
        return
            $"Bạn là trợ lý cá nhân. Dựa trên dữ liệu tuần ({from:dd/MM}–{to:dd/MM}) của người dùng, viết một đoạn " +
            "TỔNG KẾT NGẮN bằng tiếng Việt (3–5 câu) kèm 2–3 GỢI Ý hành động cụ thể. Văn phong thân thiện, súc tích.\n\n" +
            $"TÀI CHÍNH: tổng thu {f.TotalIncome:0}đ, tổng chi {f.TotalExpense:0}đ. Theo danh mục: {(string.IsNullOrEmpty(cats) ? "không có" : cats)}.\n" +
            $"SỨC KHỎE ({h.Days} ngày ghi): cân nặng TB {h.AvgWeight}kg, ngủ TB {h.AvgSleep}h, nước TB {h.AvgWater}ml, tập TB {h.AvgWorkout} phút.\n" +
            $"CÔNG VIỆC: hoàn thành {w.TasksDone} việc, tiến độ mục tiêu TB {w.GoalsAvgProgress}%, {w.HabitChecks} lượt check thói quen.\n";
    }
}
