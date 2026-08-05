# MeUp — API Reference

> Base URL (dev): `http://localhost:5149/api`  
> Base URL (prod): `https://app.yourdomain.com/api`  
> Auth: `Authorization: Bearer <access_token>`

---

## 1. Auth (`/api/auth`)

### POST `/api/auth/register`

Đăng ký tài khoản mới.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "Passw0rd!",
  "displayName": "Nguyễn Văn A"
}
```

**Response 200:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "roles": ["User"]
}
```

---

### POST `/api/auth/login`

Đăng nhập (hỗ trợ 2FA).

**Request:** `{ "email", "password" }`

**Response (không có 2FA):**
```json
{
  "requiresTwoFactor": false,
  "twoFactorToken": null,
  "auth": { "accessToken", "refreshToken", "userId", "email", "displayName", "roles" }
}
```

**Response (có 2FA):**
```json
{
  "requiresTwoFactor": true,
  "twoFactorToken": "eyJ... (JWT ngắn hạn 5 phút)",
  "auth": null
}
```

---

### POST `/api/auth/login/2fa`

Bước 2 đăng nhập khi 2FA bật.

**Request:** `{ "twoFactorToken": "...", "code": "123456" }` hoặc `{ ..., "recoveryCode": "abc-def" }`

**Response 200:** AuthResponse đầy đủ.

---

### POST `/api/auth/google`

Đăng nhập/đăng ký bằng Google ID token.

**Request:** `{ "idToken": "Google ID token từ Google Identity Services" }`

**Response 200:** AuthResponse đầy đủ (tạo mới hoặc liên kết tài khoản).

---

### POST `/api/auth/refresh`

Lấy access token mới bằng refresh token.

**Request:** `{ "refreshToken": "...", "userId": "guid" }`

**Response 200:** AuthResponse đầy đủ (refresh token xoay vòng).

---

### POST `/api/auth/logout`

Thu hồi refresh token.

**Request:** `{ "refreshToken": "..." }`  **Response 200.**

---

### POST `/api/auth/forgot-password`

Gửi email đặt lại mật khẩu.

**Request:** `{ "email": "user@example.com" }`

---

### POST `/api/auth/reset-password`

Đặt lại mật khẩu bằng token email.

**Request:** `{ "email", "token", "newPassword" }`

---

### GET `/api/auth/confirm-email`

Xác thực email từ link trong mail.

**Query:** `?userId=...&token=...`

---

## 2. Users (`/api/users`)

### GET `/api/users/me`

Lấy thông tin hồ sơ hiện tại.

**Response:**
```json
{
  "id": "guid",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "phoneNumber": "0901234567",
  "dateOfBirth": "1990-01-15",
  "gender": "male",
  "bio": "Tiểu sử ngắn",
  "avatarUrl": "/uploads/avatars/guid.png",
  "timeZone": "Asia/Ho_Chi_Minh",
  "locale": "vi",
  "twoFactorEnabled": false,
  "hasPassword": true,
  "authProviders": ["local"],
  "dailyReportEnabled": false,
  "hasOwnAiKey": false,
  "roles": ["User"]
}
```

---

### PUT `/api/users/me`

Cập nhật hồ sơ (tất cả các trường đều tùy chọn).

**Request:** `{ displayName?, phoneNumber?, dateOfBirth?, gender?, bio?, timeZone?, locale?, dailyReportEnabled? }`

---

### POST `/api/users/me/avatar`

Upload ảnh đại diện (multipart/form-data).

**Form field:** `file` (PNG/JPEG/WEBP, ≤ 2MB)

**Response:** `{ "avatarUrl": "/uploads/avatars/guid.png" }`

---

### DELETE `/api/users/me/avatar`

Xóa ảnh đại diện.

---

### POST `/api/users/me/change-email`

Đổi email đăng nhập.

**Request:** `{ "currentPassword": "...", "newEmail": "new@example.com" }`

---

### DELETE `/api/users/me`

Xóa tài khoản (xóa cứng + cascade).

**Request:** `{ "password": "..." }` (hoặc `{ "confirmText": "XÓA" }` với tài khoản Google-only)

---

### POST `/api/users/me/2fa/setup`

Khởi tạo 2FA, lấy QR code URI.

**Response:** `{ "sharedKey": "...", "authenticatorUri": "otpauth://totp/..." }`

---

### POST `/api/users/me/2fa/enable`

Bật 2FA sau khi xác minh mã TOTP.

**Request:** `{ "code": "123456" }`

**Response:** `{ "recoveryCodes": ["abc-def", ...] }` (10 mã khôi phục, chỉ hiện 1 lần)

---

### POST `/api/users/me/2fa/disable`

Tắt 2FA.

**Request:** `{ "password": "..." }`

---

### POST `/api/users/me/ai-key`

Lưu API key Claude cá nhân (BYO, mã hóa trước khi lưu).

**Request:** `{ "apiKey": "sk-ant-..." }`

---

## 3. Tài chính (`/api/finance`)

### GET `/api/finance/summary`

Tóm tắt tài chính.

**Query:** `?date=YYYY-MM-DD` (tùy chọn)

**Response:** `{ "balance", "todayIncome", "todayExpense", "monthIncome", "monthExpense" }`

---

### GET `/api/finance/transactions`

Lấy danh sách giao dịch (có phân trang + lọc).

**Query:** `?from=&to=&type=&categoryId=&q=&page=&pageSize=`

**Response:** `{ "items": [...], "total": 50, "page": 1, "pageSize": 20 }`

---

### POST `/api/finance/transactions`

Tạo giao dịch mới.

**Request:** `{ "type": "income"|"expense", "amount": 500000, "date": "2026-08-01", "categoryId"?, "note"? }`

---

### PUT `/api/finance/transactions/{id}`

Sửa giao dịch.

---

### DELETE `/api/finance/transactions/{id}`

Xóa giao dịch.

---

### GET/POST/PUT/DELETE `/api/finance/categories`

CRUD danh mục thu/chi.

---

### GET/POST/PUT/DELETE `/api/finance/budgets`

CRUD ngân sách theo danh mục.

---

### GET/POST/PUT/DELETE `/api/finance/assets`

CRUD tài sản.

---

### GET `/api/finance/networth`

Tính Net Worth + Saving Rate + Cash Flow 6 tháng.

**Query:** `?month=YYYY-MM`

---

## 4. Sức khỏe (`/api/health`)

### GET `/api/health/logs`

Lấy nhật ký sức khỏe.

**Query:** `?from=&to=`

---

### POST `/api/health/logs`

Tạo hoặc cập nhật nhật ký ngày (upsert theo ngày).

**Request:** `{ "date": "2026-08-01", "weight"?, "heightCm"?, "sleepHours"?, "waterMl"?, "workoutMinutes"?, "note"? }`

---

### GET/POST/PUT/DELETE `/api/health/activities`

CRUD hoạt động thể chất (G5).

**Request POST:** `{ "date", "type": "running"|"walking"|"gym"|"swimming"|"cycling"|"other", "durationMin", "calories"?, "note"? }`

---

### GET `/api/health/bmi`

Tính BMI dựa trên cân nặng + chiều cao mới nhất.

---

## 5. Công việc & Mục tiêu (`/api/work`)

### GET/POST/PUT/DELETE `/api/work/tasks`

CRUD tasks.

**Query GET:** `?isDone=&priority=&goalId=&page=&pageSize=`

**Request POST:**
```json
{
  "title": "Hoàn thành báo cáo",
  "dueDate": "2026-08-10",
  "priority": "high",
  "recurrence": "none",
  "goalId"?: "guid"
}
```

---

### PATCH `/api/work/tasks/{id}/status`

Cập nhật trạng thái Kanban.

**Request:** `{ "status": "in_progress" }`

---

### GET/POST/PUT/DELETE `/api/work/goals`

CRUD mục tiêu đa cấp (G1).

**Request POST:** `{ "name", "level": "life"|"year"|"quarter"|"month"|"week", "parentGoalId"?, "description"?, "targetDate"? }`

---

### GET `/api/work/goals/tree`

Lấy cây mục tiêu toàn bộ (hierarchical).

---

### GET/POST/PUT/DELETE `/api/work/habits`

CRUD thói quen.

---

### POST `/api/work/habits/{id}/check`

Check thói quen cho ngày hôm nay.

---

### DELETE `/api/work/habits/{id}/check`

Bỏ check thói quen.

---

### GET `/api/work/habits/{id}/stats`

Thống kê thói quen: streak, best streak, completion%, heatmap 12 tuần.

---

## 6. Nhật ký (`/api/journal`)

### GET/POST/PUT/DELETE `/api/journal`

CRUD nhật ký.

**Request POST:** `{ "date", "title"?, "content": "<p>Rich text HTML</p>", "mood"? }`

**Mood values:** `happy` | `neutral` | `sad` | `stressed` | `energetic`

---

## 7. Lịch trình (`/api/events`)

### GET/POST/PUT/DELETE `/api/events`

CRUD calendar events.

**Query GET:** `?from=&to=`

---

## 8. Thống kê (`/api/stats`)

### GET `/api/stats`

Tổng hợp thống kê đa chiều.

**Query:** `?from=&to=`

**Response:**
```json
{
  "finance": { "byCategory": [...], "byMonth": [...] },
  "health": { "trend": [...] },
  "work": { "taskCompletion": ..., "habitCompletion": ... }
}
```

---

## 9. Thuốc (`/api/medications`)

### GET/POST/PUT/DELETE `/api/medications`

CRUD thuốc.

---

### POST/DELETE `/api/medications/{id}/intake`

Ghi nhận / hủy uống thuốc ngày hôm nay.

---

## 10. Tìm kiếm (`/api/search`)

### GET `/api/search`

Tìm kiếm toàn cục.

**Query:** `?q=từ+khóa`

**Response:**
```json
{
  "transactions": [...],
  "tasks": [...],
  "notes": [...],
  "journal": [...]
}
```

---

## 11. Ghi chú (`/api/notes`)

### GET/POST/PUT/DELETE `/api/notes`

CRUD ghi chú (Knowledge G6).

**Request POST:** `{ "title"?, "content", "category"?, "tags"?: ["tag1", "tag2"] }`

**Backlinks:** Content hỗ trợ `[[Tên ghi chú]]` — server resolve thành links.

---

## 12. Sự nghiệp (`/api/career`)

### GET/POST/PUT/DELETE `/api/career/skills`
### GET/POST/PUT/DELETE `/api/career/certifications`
### GET/POST/PUT/DELETE `/api/career/projects`

---

## 13. Tài liệu (`/api/documents`)

### GET `/api/documents`

**Query:** `?category=`

### POST `/api/documents`

Upload tài liệu (multipart/form-data).

**Form:** `file` (binary) + `category` (text)

### GET `/api/documents/{id}/download`

Tải file về.

### DELETE `/api/documents/{id}`

---

## 14. Thông báo (`/api/notifications`)

### GET `/api/notifications`

Lấy thông báo chưa đọc.

### PUT `/api/notifications/{id}/read`

Đánh dấu đã đọc.

### DELETE `/api/notifications/{id}`

---

## 15. AI (`/api/ai`)

### GET `/api/ai/status`

Trả về trạng thái AI (có key hay không, cached insight).

### POST `/api/ai/weekly-insight`

Tạo tổng kết tuần bằng Claude API.

**Request:** `{ "weekFrom": "2026-07-28", "weekTo": "2026-08-03" }` (tùy chọn)

**Response:** `{ "content": "...", "generatedAt": "..." }`

---

## 16. Export (`/api/export`)

### GET `/api/export`

Xuất toàn bộ dữ liệu ra JSON.

**Response:** JSON file download (Content-Disposition: attachment)

---

## 17. Admin (`/api/admin`)

> Yêu cầu role Admin.

### GET `/api/admin/users`

Danh sách tất cả người dùng.

### PUT `/api/admin/users/{id}/lock`

Khóa/mở khóa tài khoản.

**Request:** `{ "lock": true|false }`

---

## Error Response Format

```json
{
  "message": "Thông báo lỗi bằng tiếng Việt",
  "errors": {
    "field": ["Mô tả lỗi cụ thể"]
  }
}
```

**HTTP Status Codes:**

| Code | Ý nghĩa |
|------|---------|
| 200 | Thành công |
| 201 | Tạo mới thành công |
| 204 | Xóa thành công |
| 400 | Dữ liệu không hợp lệ |
| 401 | Chưa xác thực |
| 403 | Không có quyền |
| 404 | Không tìm thấy (hoặc không phải của user) |
| 409 | Xung đột (vd: email đã tồn tại) |
| 500 | Lỗi server |
