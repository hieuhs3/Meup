# MeUp — Tổng quan hệ thống

> Phiên bản: Phase 3 (G1–G8 + G11) · 2026-08-05

---

## 1. Mô tả dự án

**MeUp** là ứng dụng quản lý cá nhân (personal life management) đa người dùng, full-stack. Hệ thống cho phép người dùng theo dõi tất cả các khía cạnh quan trọng trong cuộc sống: tài chính, sức khỏe, công việc, mục tiêu, thói quen, nhật ký và nhiều hơn nữa.

### Đặc điểm chính

- **Đa người dùng**: Dữ liệu hoàn toàn cô lập theo `UserId` — người dùng không thể đọc dữ liệu của nhau.
- **Full-stack**: Backend ASP.NET Core Web API (.NET 9) + Frontend Angular 20 + PostgreSQL.
- **Bảo mật cao**: JWT (access + refresh token), OAuth2 Google, 2FA TOTP, khóa tài khoản.
- **Mobile-ready**: Hỗ trợ Android/iOS qua Capacitor + PWA.
- **AI-powered**: Tích hợp Claude API (Anthropic) cho AI Insight & Weekly Report.

---

## 2. Tech Stack

### Backend

| Thành phần | Công nghệ | Phiên bản |
|------------|-----------|-----------|
| Framework | ASP.NET Core Web API | .NET 9 |
| ORM | Entity Framework Core | 9.0.17 |
| Database | PostgreSQL (Npgsql) | 16 |
| Identity | ASP.NET Core Identity | 9.0.17 |
| Auth | JWT Bearer + Google OAuth2 | 9.0.17 |
| AI | Anthropic Claude SDK | 12.29.1 |
| Caching | Redis | 7 |
| Data Protection | ASP.NET Core Data Protection | built-in |
| OpenAPI | Microsoft.AspNetCore.OpenApi | 9.0.7 |

### Frontend

| Thành phần | Công nghệ | Phiên bản |
|------------|-----------|-----------|
| Framework | Angular (Standalone Components + Signals) | 20.3.x |
| Language | TypeScript | ~5.9.2 |
| Styling | SCSS (Vanilla) | — |
| Mobile | Capacitor | 8.4.x |
| HTTP | Angular HttpClient + RxJS | 7.8.x |
| Testing | Karma + Jasmine | 6.4.x / 5.9.x |

### Infrastructure

| Thành phần | Công nghệ |
|------------|-----------|
| Container | Docker + Docker Compose |
| Reverse Proxy | Nginx (trong container web) |
| Tunnel/HTTPS | Cloudflare Tunnel |
| Hosting | Google Cloud Always Free (e2-micro) |

---

## 3. Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────┐
│                    Người dùng (Browser/App)              │
└────────────────────────┬────────────────────────────────┘
                         │ HTTPS
                         ▼
┌─────────────────────────────────────────────────────────┐
│              Cloudflare Tunnel (HTTPS, free)             │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌────────────────── Docker Compose ───────────────────────┐
│                                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │  web (nginx)                                     │    │
│  │   / → Angular SPA (static)                       │    │
│  │   /api → proxy → api:8080                        │    │
│  │   /uploads → proxy → api:8080                    │    │
│  └─────────────────┬───────────────────────────────┘    │
│                     │                                    │
│  ┌──────────────────▼──────────────────────────────┐    │
│  │  api (.NET 9 Web API) :8080                      │    │
│  │   Controllers → Services → EF Core → PostgreSQL  │    │
│  │   Background: ReminderService, DailyReportService │    │
│  └──────┬──────────────────────┬───────────────────┘    │
│         │                      │                         │
│  ┌──────▼──────┐    ┌──────────▼──────────────────┐    │
│  │  db (Postgres│    │  redis :6379                 │    │
│  │  :5432)     │    │  (caching/session)            │    │
│  └─────────────┘    └────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

**Dev environment**: Backend chạy trực tiếp `dotnet run`, Frontend `ng serve`, DB + Redis qua Docker Compose.

---

## 4. Luồng dữ liệu

### Luồng xác thực (Authentication Flow)

```
Client                    API Server              PostgreSQL
  │                          │                        │
  ├── POST /api/auth/login ──►│                        │
  │                          ├── Verify password ─────►│
  │                          │◄── User data ──────────┤
  │                          ├── Issue JWT (15 min)    │
  │                          ├── Issue RefreshToken ───►│ (lưu hash)
  │◄── access+refresh token ─┤                        │
  │                          │                        │
  ├── GET /api/finance (Bearer JWT) ──►│              │
  │                          ├── Validate JWT          │
  │                          ├── Extract UserId        │
  │                          ├── Query (WHERE UserId=) ►│
  │◄── data (cô lập user) ───┤                        │
```

### Luồng refresh token

```
Client                    API Server
  │                          │
  ├── POST /api/auth/refresh ►│
  │   {refreshToken, userId}  │
  │                          ├── Hash token → tìm trong DB
  │                          ├── Kiểm tra hết hạn
  │                          ├── Rotate: xóa cũ, tạo mới
  │◄── new access+refresh ───┤
```

---

## 5. Các tính năng (Feature Map)

### MVP Core (Phase 1)

| Mã | Tính năng | Trạng thái |
|----|-----------|-----------|
| F0 | Auth & Nền tảng (đăng ký, đăng nhập, JWT) | ✅ Hoàn tất |
| F0E | Hồ sơ mở rộng, đổi email, xóa TK, Google OAuth2, 2FA TOTP | ✅ Hoàn tất |
| F1 | Tài chính (thu/chi, danh mục, số dư, lọc/phân trang) | ✅ Hoàn tất |
| F2 | Sức khỏe (nhật ký ngày, cân nặng, ngủ, nước, tập) | ✅ Hoàn tất |
| F3 | Công việc & Mục tiêu & Thói quen | ✅ Hoàn tất |
| F6 | Tổng quan "Hôm nay" (chọn ngày, gom F1+F2+F3) | ✅ Hoàn tất |
| F5 | Nhật ký (rich-text editor, CRUD, tìm kiếm) | ✅ Hoàn tất |

### Phase 2

| Mã | Tính năng | Trạng thái |
|----|-----------|-----------|
| A1 | Ngân sách theo danh mục | ✅ Hoàn tất |
| F4 | Lịch trình (calendar events) | ✅ Hoàn tất |
| F7 | Thống kê (biểu đồ) | ✅ Hoàn tất |
| A2 | Thuốc & nhắc uống thuốc | ✅ Hoàn tất |
| A3 | Task lặp lại (daily/weekly/monthly) | ✅ Hoàn tất |
| B2 | Tìm kiếm toàn cục | ✅ Hoàn tất |
| A5 | Ghi chú nhanh | ✅ Hoàn tất |
| C3 | Cài đặt (dark mode + xuất dữ liệu) | ✅ Hoàn tất |
| C4 | PWA | ✅ Hoàn tất |
| C1 | Nhắc nhở/thông báo (background service) | ✅ Hoàn tất |
| C2 | Reset mật khẩu, verify email, khóa đăng nhập | ✅ Hoàn tất |

### Phase 3

| Mã | Tính năng | Trạng thái |
|----|-----------|-----------|
| G1 | Mục tiêu đa cấp (đời→năm→quý→tháng→tuần) + rollup + Dashboard | ✅ Hoàn tất |
| G2 | Mood tracking + biểu đồ xu hướng (Nhật ký) | ✅ Hoàn tất |
| G3 | Habit nâng cấp: best streak + completion% + heatmap 12 tuần | ✅ Hoàn tất |
| G4 | Tài sản & Net Worth + Saving Rate + Cash Flow | ✅ Hoàn tất |
| G5 | Sức khỏe: BMI + hoạt động + biểu đồ xu hướng | ✅ Hoàn tất |
| G6 | Kiến thức: ghi chú có tag + backlinks `[[..]]` kiểu Obsidian | ✅ Hoàn tất |
| G7 | Sự nghiệp: Skills / Certifications / Projects | ✅ Hoàn tất |
| G8 | Tài liệu: upload + phân loại + lưu local | ✅ Hoàn tất |
| G11 | Task Kanban (todo→đang làm→soát→xong) | ✅ Hoàn tất |
| G9 | AI Assistant + RAG | ⏳ Chờ API key + PgVector |
| G10 | Web Push / Telegram | ⏳ Chờ VAPID / bot token |

### Mobile

| Mã | Tính năng | Trạng thái |
|----|-----------|-----------|
| M0 | Setup Capacitor (Android) | ✅ Hoàn tất |
| M1 | Mobile UX (responsive, safe area, gestures) | ✅ Hoàn tất |
| M2 | Mobile screens | ✅ Hoàn tất |
| M3 | Native plugins (camera, haptics, status bar) | ✅ Hoàn tất |
| M4 | Offline support (sync service) | ✅ Hoàn tất |
| M5 | Build APK release | ✅ Hoàn tất |

---

## 6. Nguyên tắc thiết kế

1. **Data Isolation**: Mọi bảng nghiệp vụ có `UserId` (Guid). Mọi query đều filter `WHERE UserId = currentUserId`. Truy cập sai → 404 (không lộ tồn tại).
2. **Stateless API**: JWT ngắn hạn (15 phút). Refresh token xoay vòng (7 ngày). Không session server-side.
3. **Interface-first DI**: Mọi service đăng ký qua interface (`IFinanceService`, v.v.) → dễ mock, dễ test.
4. **Cascade Delete**: Xóa user → xóa toàn bộ dữ liệu (cascade DB). Đảm bảo không rò rỉ dữ liệu.
5. **Background Services**: `ReminderBackgroundService` + `DailyReportBackgroundService` chạy hosted service, không block request.
6. **Tiếng Việt**: Toàn bộ thông báo lỗi và giao diện bằng tiếng Việt.
