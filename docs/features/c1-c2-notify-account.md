# C1 — Nhắc nhở & Thông báo · C2 — Vòng đời tài khoản

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1. Phụ thuộc: F0 (auth) + F2/F3/F4 (nguồn dữ liệu để nhắc).

> **Test: 91/91 backend pass** (+8 cho C1/C2) · **29 frontend pass**.
> Kiểm chứng API thật: quên→đặt lại→đăng nhập bằng mật khẩu mới (token qua dev email ghi file);
> xác thực email bằng token; **khóa tạm sau 5 lần sai**; nhắc digest tạo thông báo (unread) + **chống trùng**.
> Email mặc định **LogEmailSender** (ghi `backend/MeUp.Api/sent-emails/*.txt`); điền `Email:Host` để bật SMTP thật.

---

# 1. YÊU CẦU

## C2 — Vòng đời tài khoản (cần email)
- **AC-C2.1** Quên mật khẩu: gửi email kèm link đặt lại; đặt lại bằng token. Không lộ email nào tồn tại (luôn trả 200).
- **AC-C2.2** Xác thực email: khi đăng ký gửi email xác thực; xác nhận bằng token; cho gửi lại.
- **AC-C2.3** Khóa đăng nhập: sai mật khẩu nhiều lần (5) → **khóa tạm 15 phút** (Identity lockout).
- **AC-C2.4** Mọi thông báo tiếng Việt; token URL-encode trong link.

## C1 — Nhắc nhở & Thông báo
- **AC-C1.1** Thông báo **in-app**: danh sách + số chưa đọc; đánh dấu đã đọc / đọc tất cả / xóa.
- **AC-C1.2** **Nhắc tự động** (digest theo ngày, 1 lần/ngày/user): việc **quá hạn**, **sự kiện hôm nay**, **thuốc chưa uống**. Tạo notification in-app + gửi email (nếu có cấu hình).
- **AC-C1.3** Chống trùng: mỗi (user, khóa nhắc theo ngày) chỉ tạo một lần.
- **AC-C1.4** Dữ liệu cô lập theo UserId.

---

# 2. THIẾT KẾ

## 2.1 Email — 2 chế độ
- `IEmailSender.SendAsync(to, subject, htmlBody)`.
- `SmtpEmailSender` (System.Net.Mail, không thêm thư viện) khi cấu hình `Email:Host`.
- `LogEmailSender` (mặc định dev): ghi email ra **log** + file `backend/MeUp.Api/sent-emails/*.txt` để lấy link reset/verify khi chưa có SMTP.
- `EmailOptions` (section `Email`): `Host, Port, User, Password, UseSsl, FromEmail, FromName, WebBaseUrl`.
- Link xây từ `WebBaseUrl` (mặc định `http://localhost:4200`).

## 2.2 C2 — Auth mở rộng (`api/auth`)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/forgot-password` | Gửi email link đặt lại (luôn 200) |
| POST | `/reset-password` | Đặt lại mật khẩu bằng token |
| POST | `/confirm-email` | Xác nhận email bằng token |
| POST | `/resend-confirmation` | Gửi lại email xác thực |
- `register` thêm: sinh token xác thực + gửi email (không chặn đăng nhập).
- `login` dùng **lockout**: kiểm tra `IsLockedOut` → `AccessFailed` khi sai → `ResetAccessFailedCount` khi đúng. Cấu hình: 5 lần, khóa 15 phút.

## 2.3 C1 — Mô hình & dịch vụ
```
Notification
  Id, UserId, Type, Title, Message, Link?, IsRead, CreatedAt
  DedupKey?  (unique theo (UserId, DedupKey) để chống trùng nhắc)
```
- `INotificationService`: list / unreadCount / markRead / markAllRead / delete / create.
- `IReminderService.GenerateForUserAsync(userId, date)`: gom việc quá hạn + sự kiện ngày + thuốc chưa uống → 1 digest (DedupKey `daily:{date}`); bỏ qua nếu đã có; gửi email kèm.
- `ReminderBackgroundService` (IHostedService): định kỳ (mỗi 30 phút) quét mọi user gọi generate cho hôm nay.
- Endpoint `POST /api/notifications/run-reminders`: chạy nhắc thủ công cho user hiện tại (cho nút "kiểm tra ngay" + test).

## 2.4 API thông báo (`api/notifications`)
| Method | Endpoint |
|--------|----------|
| GET | `/` (danh sách) |
| GET | `/unread-count` |
| POST | `/{id}/read` · `/read-all` · `/run-reminders` |
| DELETE | `/{id}` |

## 2.5 Frontend
- C2: trang **Quên mật khẩu** (`/forgot-password`) + **Đặt lại** (`/reset-password?email=&token=`); link từ trang Đăng nhập.
- C1: **chuông 🔔** ở shell (số chưa đọc) + trang **Thông báo** (`/app/notifications`): danh sách, đánh dấu đọc/đọc hết/xóa, nút "Nhắc ngay".

---

# 3. KẾ HOẠCH
| # | Task | Phạm vi |
|---|------|---------|
| 1 | Email infra (IEmailSender, Smtp/Log, options, đăng ký) | backend |
| 2 | C2: forgot/reset/confirm/resend + lockout + gửi email khi register | backend |
| 3 | C1: Notification entity + service/controller; Reminder service + hosted service; migration | backend |
| 4 | Test integration (reset qua email-capture, lockout, notifications, reminder) | test |
| 5 | FE: forgot/reset pages + chuông + trang thông báo | frontend |

**Quyết định:** mặc định **LogEmailSender** để chạy không cần SMTP; điền `Email:Host` để bật SMTP thật.
Dùng Identity lockout sẵn có thay vì tự viết rate-limit. Nhắc dạng **digest/ngày** cho đơn giản & hữu ích.

**DoD:** quên/đặt lại mật khẩu chạy (token qua email/log); khóa sau 5 lần sai; thông báo in-app + nhắc digest hoạt động & chống trùng; test xanh; build sạch.
