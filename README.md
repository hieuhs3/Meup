# MeUp

Web app đa người dùng để quản lý **tất tần tật về bản thân theo từng ngày**: tài chính, sức khỏe, công việc & mục tiêu.

- **Full-stack**: backend ASP.NET Core Web API (.NET 9) + frontend Angular 20 + PostgreSQL.
- **Bảo mật**: đăng ký/đăng nhập bằng JWT (access + refresh token), phân quyền user/admin, dữ liệu cô lập theo người dùng.

## Trạng thái

Chức năng **F0 — Nền tảng & Auth** đã hoàn tất (backend + frontend + test).
Bản mở rộng **F0E** cũng đã hoàn tất: hồ sơ cá nhân đầy đủ (ảnh đại diện, SĐT, ngày sinh,
giới tính, tiểu sử, múi giờ, ngôn ngữ), **đổi email**, **xóa tài khoản**, **đăng nhập Google (OAuth2)**
và **xác thực 2 lớp 2FA (TOTP)** — xem `docs/features/f0e-profile-auth.md`.
Chức năng **F1 — Tài chính** đã hoàn tất: ghi thu/chi theo ngày, danh mục (tự seed mặc định),
số dư + tổng thu/chi theo ngày/tháng, lọc/tìm/sửa/xóa, phân trang — xem `docs/features/f1-finance.md`.
Chức năng **F2 — Sức khỏe** đã hoàn tất: nhật ký ngày (cân nặng, giờ ngủ, nước, buổi tập, ghi chú),
upsert theo ngày, so sánh với lần ghi trước, lịch sử gần đây — xem `docs/features/f2-health.md`.
Chức năng **F3 — Công việc/Mục tiêu/Thói quen** đã hoàn tất: task (hạn, quá hạn, hoàn thành),
mục tiêu (tiến độ %), thói quen (check theo ngày + streak) — xem `docs/features/f3-work.md`.
Chức năng **F6 — Tổng quan "Hôm nay"** đã hoàn tất: chọn ngày bất kỳ + lùi/tới, gom Tài chính/Sức khỏe/Công việc
theo ngày, gợi ý khi rỗng — xem `docs/features/f6-today.md`. ✅ **Mốc MVP (F0+F1+F2+F3+F6) đã khép.**
Chức năng **F5 — Nhật ký** đã hoàn tất: viết nhật ký theo ngày với **bộ soạn thảo rich-text nhúng**
(đậm/nghiêng/tiêu đề/danh sách/trích dẫn/link), CRUD + tìm kiếm — xem `docs/features/f5-journal.md`.
**Phase 2:** ✅ A1 Ngân sách · ✅ F4 Lịch trình · ✅ F7 Thống kê (biểu đồ) · ✅ A2 Thuốc · ✅ A3 Task lặp lại ·
✅ B2 Tìm kiếm toàn cục · ✅ A5 Ghi chú nhanh · ✅ C3 Cài đặt (dark mode + xuất dữ liệu) · ✅ C4 PWA ·
✅ C1 Nhắc nhở/thông báo · ✅ C2 Reset mật khẩu + verify email + khóa đăng nhập.
**Phase 3 — Đợt 3A (đã xong, 120 test backend):**
✅ **G1** Mục tiêu đa cấp (đời→năm→quý→tháng→tuần) + trạng thái + tiến độ rollup + Goal Dashboard ·
✅ **G2** Mood tracking + biểu đồ xu hướng (Nhật ký) ·
✅ **G4** Tài sản & Net Worth + Saving Rate + Cash Flow (Tài chính) ·
✅ **G3** Habit nâng cấp: best streak + completion % + **heatmap** 12 tuần.
Xem `docs/08-phase3-plan.md`.
**Phase 3 — Đợt 3B (đã xong, 141 test backend):**
✅ **G5** Sức khỏe: BMI + hoạt động (chạy/gym/bơi…) + biểu đồ xu hướng ·
✅ **G6** Kiến thức: ghi chú có tiêu đề/thẻ + **backlinks** `[[..]]` (kiểu Obsidian) ·
✅ **G7** Sự nghiệp: Skills/Certifications/Projects ·
✅ **G8** Tài liệu: upload + phân loại + lưu local (`IFileStorage`, đổi MinIO/S3 sau).
**Phase 3 — Đợt 3C:** ✅ **G11** Task Kanban (todo→đang làm→soát→xong) — công tắc Cây/Kanban.
**Chờ khóa/dịch vụ ngoài (làm khi deploy):** G9 AI Assistant + RAG (cần `ANTHROPIC_API_KEY` + embedding provider + PgVector),
G10 Web Push/Telegram (cần VAPID / bot token).
**Còn lại:** i18n vi/en (tùy chọn).

> Tổng Phase 3 đã xong: **G1–G8 + G11** — backend **146 test pass**, frontend build sạch.
Email: mặc định ghi ra `backend/MeUp.Api/sent-emails/` (dev); điền `Email:Host` trong appsettings để bật SMTP thật.
Kế hoạch tổng thể & lộ trình Phase 2: xem `docs/03-feature-plan.md`.

## Cấu trúc

```
MeUp/
├── backend/             # ASP.NET Core Web API + xUnit tests
│   ├── MeUp.Api/
│   └── MeUp.Tests/
├── frontend/            # Angular 20 (standalone + signals)
├── docs/                # Quy trình, yêu cầu, kiến trúc, kế hoạch chức năng
├── .claude/agents/      # Định nghĩa các vai trò (agents) của quy trình
└── docker-compose.yml   # PostgreSQL + Redis cho môi trường dev
```

## Yêu cầu môi trường

- .NET SDK 9, Node.js 20+, Docker.

## Cách chạy (dev)

**1. Khởi động hạ tầng (PostgreSQL + Redis):**
```bash
docker compose up -d
```
> Postgres chạy ở cổng host **5433** (tránh đụng Postgres cài sẵn trên máy ở 5432).

**2. Backend API** (tự áp migration + seed vai trò & admin khi khởi động):
```bash
cd backend/MeUp.Api
dotnet run --launch-profile http
# API: http://localhost:5149
```

**3. Frontend Angular:**
```bash
cd frontend
npm install        # lần đầu
npx ng serve       # http://localhost:4200
```

Mở trình duyệt tại **http://localhost:4200**.

## Tài khoản admin mặc định

| Email | Mật khẩu |
|-------|----------|
| `admin@meup.local` | `Admin@12345` |

> Cấu hình trong `backend/MeUp.Api/appsettings.json` (mục `Seed:Admin`). Đổi trước khi triển khai thật.

## Chạy test

**Backend** (xUnit — unit + integration qua WebApplicationFactory trên DB test `meup_test`):
```bash
docker compose up -d   # cần Postgres (cổng 5433) cho integration test
cd backend
dotnet test            # 83 test
```

**Frontend** (Karma + Jasmine, chạy headless — cần Chrome/Chromium):
```bash
cd frontend
npm run test:ci        # 29 test (service + ThemeService + RichEditor + component: Login/Today/Search), ChromeHeadless
# npm test            # chế độ watch (mở trình duyệt)
```
> Nếu Karma không tự tìm thấy trình duyệt, đặt biến `CHROME_BIN` trỏ tới chrome.exe.

## Đăng nhập Google (tùy chọn)

Mặc định nút "Đăng nhập với Google" bị ẩn. Để bật:

1. Tạo **OAuth 2.0 Client ID** (loại Web) trong Google Cloud Console, thêm origin `http://localhost:4200`.
2. Backend: đặt `Authentication:Google:ClientId` trong `appsettings.json` (hoặc biến môi trường / user-secrets).
3. Frontend: điền `GOOGLE_CLIENT_ID` trong `frontend/src/app/core/api.config.ts`.

> Avatar upload được lưu ở `backend/MeUp.Api/wwwroot/uploads/avatars` và phục vụ tĩnh qua API.

## Lưu ý bảo mật (dev → production)

- Đổi `Jwt:Key` và mật khẩu DB/admin; KHÔNG commit khóa thật (dùng biến môi trường / user-secrets).
- Bật HTTPS thật khi triển khai.
- Đặt `Authentication:Google:ClientId` qua biến môi trường ở production (không commit).
