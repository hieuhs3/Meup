using System.Text;
using MeUp.Api.Data;
using MeUp.Api.Entities;
using MeUp.Api.Options;
using MeUp.Api.Services;
using GoogleOptions = MeUp.Api.Options.GoogleOptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "AngularApp";

// --- Cấu hình ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Thiếu cấu hình Jwt.");

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Identity ---
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        // Khóa tạm sau nhiều lần đăng nhập sai (C2).
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- Auth JWT ---
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// --- CORS cho Angular ---
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

// --- Đăng nhập Google (OAuth2 / OpenID Connect) ---
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(GoogleOptions.SectionName));

// --- Email: SMTP nếu có cấu hình, ngược lại ghi log/file (dev) ---
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailOpt = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
if (emailOpt.IsConfigured)
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, LogEmailSender>();

// --- DI nghiệp vụ ---
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IWorkService, WorkService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddHostedService<ReminderBackgroundService>();

// --- AI (Claude API) ---
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddScoped<IAiInsightService, AiInsightService>();
// Data Protection: dùng để mã hóa API key riêng của từng user. Lưu khóa ra thư mục "keys"
// (mount volume ở production) để giá trị đã mã hóa vẫn giải mã được sau khi restart/redeploy.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("MeUp");
builder.Services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();

// Chạy sau reverse proxy / Cloudflare Tunnel: tin header X-Forwarded-* để
// biết scheme/ip gốc (https) thay vì http nội bộ trong container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- Migrate + seed dữ liệu khởi tạo ---
await DbSeeder.SeedAsync(app.Services, builder.Configuration);

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Sau Cloudflare Tunnel, TLS do Cloudflare lo; container chỉ chạy HTTP.
    app.UseHttpsRedirection();
}
app.UseStaticFiles(); // phục vụ avatar đã upload ở wwwroot/uploads
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập lớp Program.
public partial class Program { }
