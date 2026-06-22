# F0 — Nền tảng & Auth

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT** (chờ Review/Deploy)
Ưu tiên: P0 (nền bắt buộc cho mọi chức năng)

> Tiến độ task: T1–T14 ✅ HOÀN TẤT & kiểm chứng.
> Backend: mọi AC đạt; 7/7 unit test pass. Frontend Angular build sạch, dev server chạy, CORS thông.
> Stack chạy: API `http://localhost:5149`, web `http://localhost:4200`. Admin: `admin@meup.local` / `Admin@12345`.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Cung cấp khung ứng dụng full-stack và hệ thống tài khoản an toàn: đăng ký, đăng nhập,
quản lý phiên, quản lý hồ sơ và phân quyền — làm nền cho mọi chức năng sau.

## 1.2 User stories
- Là khách, tôi muốn **đăng ký** bằng email + mật khẩu để có tài khoản.
- Là người dùng, tôi muốn **đăng nhập** để truy cập dữ liệu của riêng mình.
- Là người dùng, tôi muốn **giữ đăng nhập** an toàn và **đăng xuất** khi cần.
- Là người dùng, tôi muốn **xem/sửa hồ sơ** và **đổi mật khẩu**.
- Là admin, tôi muốn **xem danh sách user** và **khóa/mở khóa** tài khoản.
- Là người dùng, tôi muốn **dữ liệu của tôi tách biệt**, không ai khác đọc được.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-A1** Đăng ký: email hợp lệ + chưa tồn tại; mật khẩu ≥ 8 ký tự. Trùng email → báo lỗi rõ.
- **AC-A2** Mật khẩu **không bao giờ** lưu dạng thô — băm bằng thuật toán mạnh.
- **AC-A3** Đăng nhập đúng → trả **access token (ngắn hạn)** + **refresh token (dài hạn)**.
- **AC-A4** Access token hết hạn → dùng refresh token lấy token mới mà không cần đăng nhập lại.
- **AC-A5** Đăng xuất → refresh token bị thu hồi (không dùng lại được).
- **AC-A6** Mọi API nghiệp vụ yêu cầu access token hợp lệ; thiếu/sai → 401.
- **AC-A7** Truy cập dữ liệu không thuộc về mình → 403/404 (không lộ dữ liệu).
- **AC-A8** Endpoint admin chỉ truy cập được với vai trò `admin`; user thường → 403.
- **AC-A9** Đổi mật khẩu yêu cầu mật khẩu hiện tại đúng.
- **AC-A10** Toàn bộ thông báo lỗi/giao diện tiếng Việt.

## 1.4 Trường hợp biên
- Email sai định dạng, mật khẩu yếu, email trùng.
- Token hết hạn, token giả mạo, refresh token đã thu hồi/đã dùng.
- Đăng nhập sai nhiều lần (chuẩn bị chỗ cho rate-limit/khóa tạm — P2).
- Admin tự khóa chính mình → chặn.

---

# 2. THIẾT KẾ (Design)

## 2.1 Kiến trúc tổng thể (áp dụng cho cả app)
```
[ Angular SPA ] ──HTTPS, JWT in Authorization header──► [ ASP.NET Core Web API ]
                                                              │
                            ┌─────────────────────────────────┼───────────────┐
                            ▼                                 ▼               ▼
                     [ PostgreSQL (EF Core) ]         [ Redis: refresh    [ Serilog
                                                        token / cache ]     logging ]
```
Phân tầng backend: **Controller → Service → Repository/DbContext**. DTO tách khỏi entity.

## 2.2 Mô hình dữ liệu
```
User
  Id (Guid, PK)
  Email (unique, index)
  PasswordHash
  DisplayName
  Role            ("user" | "admin")
  IsLocked (bool)
  CreatedAt

RefreshToken
  Id (Guid, PK)
  UserId (FK → User)
  TokenHash
  ExpiresAt
  RevokedAt (nullable)
  CreatedAt
```
> Quy ước chung toàn app: mọi bảng nghiệp vụ sau này có cột `UserId` để cô lập dữ liệu.

## 2.3 API (REST)
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|-------|-------|
| POST | `/api/auth/register` | Đăng ký | công khai |
| POST | `/api/auth/login` | Đăng nhập → access + refresh | công khai |
| POST | `/api/auth/refresh` | Làm mới access token | công khai (kèm refresh token) |
| POST | `/api/auth/logout` | Thu hồi refresh token | đã đăng nhập |
| GET  | `/api/users/me` | Xem hồ sơ | đã đăng nhập |
| PUT  | `/api/users/me` | Sửa hồ sơ (DisplayName) | đã đăng nhập |
| POST | `/api/users/me/change-password` | Đổi mật khẩu | đã đăng nhập |
| GET  | `/api/admin/users` | Danh sách user | admin |
| POST | `/api/admin/users/{id}/lock` | Khóa/mở khóa | admin |

## 2.4 Luồng JWT
- Login → tạo access token (vd 15 phút) ký bằng khóa server + refresh token (vd 7 ngày), lưu **hash** refresh token vào DB/Redis.
- Refresh → kiểm tra refresh token còn hạn & chưa thu hồi → cấp access token mới (xoay refresh token).
- Logout → đánh dấu `RevokedAt`.

## 2.5 Bảo mật
- Băm mật khẩu: ASP.NET Core Identity (PBKDF2) hoặc Argon2.
- HTTPS bắt buộc; CORS chỉ cho origin của Angular app.
- Middleware xác thực JWT + policy phân quyền theo Role.
- Không trả thông tin nhạy cảm (PasswordHash) ra DTO.

## 2.6 Frontend (Angular)
- Module `auth`: trang Đăng ký, Đăng nhập, Hồ sơ, Đổi mật khẩu.
- `AuthService` giữ token (in-memory + refresh), `AuthInterceptor` gắn header + tự refresh khi 401.
- `AuthGuard` chặn route khi chưa đăng nhập; `AdminGuard` cho route admin.
- Khung layout: sidebar điều hướng + vùng nội dung (chuẩn bị chỗ cho F1–F6).

---

# 3. LẬP KẾ HOẠCH (Plan)

Thứ tự: backend nền → auth core → frontend khung → admin. Độ lớn: S/M/L.

| # | Task | File/Phạm vi | Phụ thuộc | Lớn |
|---|------|--------------|-----------|-----|
| T1 | Khởi tạo solution .NET Web API + cấu trúc thư mục | `/backend` | — | M |
| T2 | Cấu hình EF Core + PostgreSQL + chuỗi kết nối + Docker compose (db, redis) | backend, `docker-compose.yml` | T1 | M |
| T3 | Entity `User`, `RefreshToken` + DbContext + migration đầu | backend | T2 | M |
| T4 | Service băm mật khẩu + sinh/validate JWT + refresh token | backend | T3 | L |
| T5 | `AuthController`: register / login / refresh / logout | backend | T4 | M |
| T6 | `UsersController`: me (get/put) + change-password | backend | T4 | S |
| T7 | Middleware JWT + policy Role + cô lập theo UserId | backend | T5 | M |
| T8 | `AdminController`: list users + lock/unlock | backend | T7 | S |
| T9 | Unit/integration test backend (xUnit) theo AC | `/backend.Tests` | T5–T8 | M |
| T10 | Khởi tạo Angular app + routing + layout khung | `/frontend` | — | M |
| T11 | `AuthService` + `AuthInterceptor` + `AuthGuard`/`AdminGuard` | frontend | T10, T5 | M |
| T12 | Trang Đăng ký / Đăng nhập / Hồ sơ / Đổi mật khẩu | frontend | T11 | L |
| T13 | Trang admin quản lý user | frontend | T11, T8 | M |
| T14 | README chạy dự án (backend + frontend + docker) | `/README.md` | mọi task | S |

**Rủi ro / điểm quyết định:**
- Dùng **ASP.NET Core Identity** trọn gói hay tự cài user/token tối giản? → đề xuất Identity cho chuẩn & bảo mật.
- Lưu refresh token ở **PostgreSQL** hay **Redis**? → MVP dùng PostgreSQL cho đơn giản; Redis thêm khi cần scale.
- Phiên bản .NET (đề xuất .NET 8 LTS) và Angular (bản mới nhất ổn định) — cần xác nhận môi trường máy.

**Định nghĩa hoàn thành (DoD) cho F0:** đăng ký/đăng nhập/refresh/logout chạy được; dữ liệu cô lập theo user; admin khóa được user; test theo AC pass; README chạy được.
