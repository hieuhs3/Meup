# MeUp — Kiến trúc Backend

> .NET 9 · ASP.NET Core Web API · EF Core + PostgreSQL

---

## 1. Cấu trúc thư mục

```
backend/
├── MeUp.Api/
│   ├── Controllers/          # REST API controllers (17 files)
│   ├── Data/
│   │   ├── AppDbContext.cs   # EF Core DbContext
│   │   ├── DbSeeder.cs       # Seed roles + admin khi khởi động
│   │   └── Migrations/       # EF Core migrations
│   ├── Dtos/                 # Request/Response DTOs
│   ├── Entities/             # Domain entities (POCO classes)
│   ├── Options/              # Config binding classes
│   ├── Services/             # Business logic (interface + implementation)
│   ├── Program.cs            # Điểm vào: DI, middleware pipeline
│   ├── appsettings.json      # Config mặc định
│   └── Dockerfile            # Multi-stage build
└── MeUp.Tests/               # xUnit test project
```

---

## 2. Middleware Pipeline

Thứ tự trong `Program.cs`:

```
Request
  │
  ▼ UseForwardedHeaders()    # X-Forwarded-* từ Cloudflare/nginx → scheme/IP gốc
  ▼ UseStaticFiles()         # /uploads/avatars/* (ảnh đại diện)
  ▼ UseCors("AngularApp")    # Cho phép origin Angular / Capacitor
  ▼ UseAuthentication()      # Đọc JWT Bearer token
  ▼ UseAuthorization()       # Kiểm tra [Authorize] attribute
  ▼ MapControllers()         # Định tuyến tới Controller
```

---

## 3. Dependency Injection (DI) — Services

Tất cả service đăng ký dạng `Scoped` (theo request), trừ `IFileStorage` là `Singleton`.

### Auth & User

| Interface | Implementation | Ghi chú |
|-----------|----------------|---------|
| `ITokenService` | `TokenService` | Tạo/validate JWT + refresh token |
| `IAuthService` | `AuthService` | Register, login, Google login, 2FA, refresh |
| `IGoogleTokenValidator` | `GoogleTokenValidator` | Gọi Google tokeninfo API qua HttpClient |
| `IEmailSender` | `SmtpEmailSender` hoặc `LogEmailSender` | Tự chọn theo config |

### Nghiệp vụ

| Interface | Implementation | Domain |
|-----------|----------------|--------|
| `IFinanceService` | `FinanceService` | Tài chính (transactions, categories, assets, budget) |
| `IHealthService` | `HealthService` | Sức khỏe (health logs, activities, BMI) |
| `IWorkService` | `WorkService` | Công việc, mục tiêu, thói quen, Kanban |
| `IJournalService` | `JournalService` | Nhật ký (rich-text, mood) |
| `IEventService` | `EventService` | Lịch trình / calendar events |
| `IStatsService` | `StatsService` | Thống kê tổng hợp |
| `IMedicationService` | `MedicationService` | Thuốc & nhắc nhở uống thuốc |
| `ISearchService` | `SearchService` | Tìm kiếm toàn cục |
| `INoteService` | `NoteService` | Ghi chú nhanh (có tag, backlinks) |
| `ICareerService` | `CareerService` | Sự nghiệp (skills, certs, projects) |
| `IDocumentService` | `DocumentService` | Tài liệu (upload, phân loại) |
| `INotificationService` | `NotificationService` | Thông báo trong app |
| `IReminderService` | `ReminderService` | Nhắc nhở task/thuốc |
| `IDailyReportService` | `DailyReportService` | Báo cáo tổng kết ngày qua email |
| `IAiInsightService` | `AiInsightService` | Gợi ý AI (Claude API) |
| `IExportService` | `ExportService` | Xuất dữ liệu JSON |

### Storage & Background

| Interface | Implementation | Ghi chú |
|-----------|----------------|---------|
| `IFileStorage` | `LocalFileStorage` | Lưu file local (có thể đổi sang MinIO/S3) |
| — | `ReminderBackgroundService` | IHostedService: kiểm tra task/thuốc quá hạn |
| — | `DailyReportBackgroundService` | IHostedService: gửi báo cáo email 21:00 |

---

## 4. Controllers

### Danh sách endpoints (17 controllers)

| Controller | Base Route | Chức năng |
|------------|-----------|-----------|
| `AuthController` | `/api/auth` | Đăng ký, đăng nhập, Google, 2FA, refresh, logout |
| `UsersController` | `/api/users` | Profile, avatar, đổi email, xóa TK, admin |
| `FinanceController` | `/api/finance` | Transactions, categories, budgets, assets, net worth |
| `HealthController` | `/api/health` | Health logs, activities, BMI |
| `WorkController` | `/api/work` | Tasks, goals, habits, habit-checks, Kanban |
| `JournalController` | `/api/journal` | Journal entries (rich-text + mood) |
| `EventsController` | `/api/events` | Calendar events |
| `StatsController` | `/api/stats` | Thống kê tổng hợp |
| `MedicationsController` | `/api/medications` | Thuốc, lịch uống |
| `SearchController` | `/api/search` | Tìm kiếm toàn cục |
| `NotesController` | `/api/notes` | Ghi chú nhanh (Knowledge) |
| `CareerController` | `/api/career` | Skills, certifications, projects |
| `DocumentsController` | `/api/documents` | Upload/download tài liệu |
| `NotificationsController` | `/api/notifications` | Thông báo in-app |
| `AiController` | `/api/ai` | AI status, weekly insight |
| `ExportController` | `/api/export` | Xuất toàn bộ dữ liệu JSON |
| `AdminController` | `/api/admin` | Quản lý người dùng (admin only) |

### Phân quyền

```
[AllowAnonymous]  →  /api/auth/login, register, google, refresh, forgot-password, reset-password, confirm-email
[Authorize]       →  Tất cả các route còn lại (yêu cầu JWT hợp lệ)
[Authorize(Roles = "Admin")]  →  /api/admin/*
```

### Pattern thống nhất

```csharp
// Lấy UserId từ JWT claim
var userId = User.GetUserId();  // ClaimsPrincipalExtensions

// Trả 404 thay vì 403 khi không tìm thấy (không lộ tồn tại)
var item = await _service.GetAsync(id, userId);
if (item is null) return NotFound();
```

---

## 5. Data Layer (EF Core + PostgreSQL)

### AppDbContext

Kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`, thêm 22 DbSet nghiệp vụ:

```
RefreshTokens       Categories          Transactions
HealthLogs          Activities          Tasks
Goals               Habits              HabitChecks
JournalEntries      Budgets             CalendarEvents
Medications         MedicationIntakes   Notes
Assets              Skills              Certifications
CareerProjects      Documents           Notifications
WeeklyInsights
```

### Migration & Seeding

- Migrations tự động áp khi khởi động (gọi `DbSeeder.SeedAsync`).
- Seed: tạo Role "Admin" + "User", tạo tài khoản admin từ config.

---

## 6. Entities (Domain Model)

### ApplicationUser (Identity)

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    // Extended profile (F0E)
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }        // male|female|other
    public string? Bio { get; set; }           // max 500 chars
    public string? AvatarUrl { get; set; }     // relative path
    public string? TimeZone { get; set; }      // IANA, vd "Asia/Ho_Chi_Minh"
    public string? Locale { get; set; }        // vd "vi"
    public bool DailyReportEnabled { get; set; }
    public string? EncryptedAiApiKey { get; set; }  // BYO key, đã mã hóa
}
```

### Các enum string (static class thay vì enum để EF lưu string)

| Class | Giá trị |
|-------|---------|
| `Recurrence` | none, daily, weekly, monthly |
| `TaskPriority` | low, medium, high, critical |
| `WorkTaskStatus` | todo, in_progress, review, done, cancelled |
| `GoalLevel` | life, year, quarter, month, week |
| `GoalStatus` | draft, active, completed, cancelled, archived |
| `HabitFrequency` | daily, weekly |
| `ActivityType` | running, walking, gym, swimming, cycling, other |
| `AssetType` | cash, bank, stock, crypto, gold, other |
| `DocumentCategory` | identity, finance, health, education, work, other |

---

## 7. Background Services

### ReminderBackgroundService

- Chạy mỗi giờ (1 giờ 1 lần).
- Kiểm tra tasks/medications sắp đến hạn.
- Tạo `Notification` trong DB nếu chưa có (dùng `DedupKey` để tránh trùng).

### DailyReportBackgroundService

- Chạy mỗi phút, kiểm tra user có `DailyReportEnabled = true`.
- Gửi email tổng kết cuối ngày lúc 21:00 theo múi giờ của từng user.
- Dùng `DailyReportService` để tạo nội dung báo cáo.

---

## 8. AI Service (Claude API)

```csharp
// AiInsightService.cs
// Mỗi user có thể dùng API key riêng (BYO) hoặc key server
// Key user được mã hóa bằng Data Protection, lưu trong EncryptedAiApiKey
// Weekly insight: tổng hợp dữ liệu 7 ngày → gửi Claude → cache vào WeeklyInsights
```

**Chú ý**: Chức năng AI chỉ hoạt động khi có `ANTHROPIC_API_KEY` (server-wide) hoặc user tự cấu hình key riêng.

---

## 9. File Storage (G8 — Documents)

Interface `IFileStorage` cho phép swap implementation:

```csharp
public interface IFileStorage {
    Task<string> SaveAsync(Stream stream, string fileName, string contentType);
    Task<(Stream stream, string contentType)> ReadAsync(string storageKey);
    Task DeleteAsync(string storageKey);
}
```

- **Dev/Production hiện tại**: `LocalFileStorage` → lưu tại `backend/MeUp.Api/storage/`.
- **Future**: Thay bằng `MinioFileStorage` hoặc `S3FileStorage` mà không đổi controller.

---

## 10. Options (Config Binding)

| Class | Section | Key fields |
|-------|---------|-----------|
| `JwtOptions` | `Jwt` | Issuer, Audience, Key, AccessTokenMinutes, RefreshTokenDays |
| `GoogleOptions` | `Authentication:Google` | ClientId |
| `EmailOptions` | `Email` | Host, Port, User, Password, UseSsl, FromEmail, WebBaseUrl |
| `AiOptions` | `Ai` | ApiKey |
