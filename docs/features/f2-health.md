# F2 — Sức khỏe

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1 (MVP). Phụ thuộc: **F0** (auth + cô lập theo UserId).

> **Phase 3 · G5 — Chỉ số & hoạt động ✅:** thêm `HealthLog.HeightCm` → **BMI** tự tính (weight/(h/100)²);
> entity `Activity` (running/walking/gym/swimming/cycling/other + thời lượng + calo) CRUD `api/health/activities`;
> `GET /api/health/trends?from&to` trả chuỗi cân nặng/BMI/calo. FE: ô chiều cao + thẻ BMI + mục Hoạt động + biểu đồ xu hướng.
> Thêm 6 test (`G5HealthMetricsTests`). Nguồn: `docs/07-gap-analysis.md` (G5).

> **Test: 45/45 pass** (thêm 9 integration cho F2: upsert không trùng ngày, ngày trống trả rỗng,
> validate vượt ngưỡng, summary so sánh hôm trước, không có lần trước → previous null, xóa rồi 404,
> lọc khoảng ngày, cô lập theo user, 401).
> Kiểm chứng API thật: route `DateOnly` bind OK; summary today (69.80) vs previous (70.50) đúng.
> Lưu ý: GET `logs/{date}` khi không có dữ liệu trả **204 No Content** (HttpClient map thành `null`).

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Ghi **nhật ký sức khỏe theo ngày** (mỗi ngày một bản ghi) và xem nhanh xu hướng gần đây:
cân nặng, giờ ngủ, lượng nước, thời gian tập, ghi chú.

## 1.2 User stories
- Là người dùng, tôi muốn **ghi chỉ số ngày hôm nay** (cân nặng, giờ ngủ, nước, buổi tập, ghi chú).
- Là người dùng, tôi muốn **sửa lại** chỉ số của một ngày (ghi đè, không tạo trùng).
- Là người dùng, tôi muốn **xem lịch sử gần đây** và **so sánh với lần ghi trước** (tăng/giảm).
- Là người dùng, tôi muốn **xóa** nhật ký của một ngày.
- Là người dùng, tôi muốn **dữ liệu của tôi tách biệt** — không ai khác đọc được.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-H1** Mỗi người dùng có **tối đa một bản ghi cho một ngày** (khóa duy nhất `UserId + Date`).
- **AC-H2** Ghi nhật ký là **upsert**: cùng ngày → cập nhật; chưa có → tạo mới. Tất cả chỉ số **tùy chọn** (có thể để trống).
- **AC-H3** Ràng buộc giá trị: cân nặng 0–500 (kg); giờ ngủ 0–24; nước 0–20000 (ml); tập 0–1440 (phút); ghi chú ≤500 ký tự. Sai → 400.
- **AC-H4** Truy vấn chỉ trả dữ liệu của **chính người dùng**; ngày không có dữ liệu → trả `null` (200), không lỗi.
- **AC-H5** `summary` cho một ngày trả về **bản ghi ngày đó** + **bản ghi gần nhất trước đó** để so sánh.
- **AC-H6** Lịch sử trả danh sách theo ngày giảm dần, lọc được theo khoảng ngày.
- **AC-H7** Toàn bộ thông báo lỗi/giao diện **tiếng Việt**.

## 1.4 Trường hợp biên
- Giá trị âm/vượt ngưỡng; giờ ngủ > 24; nước cực lớn → 400.
- Ghi nhật ký toàn trường trống → vẫn tạo bản ghi rỗng (đánh dấu đã có ngày đó) — chấp nhận.
- Xóa ngày chưa có dữ liệu → 404.
- So sánh khi chưa có bản ghi trước → `previous = null` (không lỗi).

## 1.5 Ngoài phạm vi (để P2)
- Thuốc / nhắc uống.
- Biểu đồ xu hướng (sẽ làm ở F7 — Thống kê); F2 chỉ hiển thị lịch sử dạng bảng + so sánh hôm trước.

---

# 2. THIẾT KẾ (Design)

## 2.1 Mô hình dữ liệu
```
HealthLog
  Id            Guid PK
  UserId        Guid FK → AspNetUsers (cascade)
  Date          date (DateOnly)
  Weight        decimal(5,2)? (kg)
  SleepHours    decimal(4,1)? (giờ)
  WaterMl       int?          (ml)
  WorkoutMinutes int?         (phút)
  Note          string? (≤500)
  CreatedAt     DateTime
  UpdatedAt     DateTime
  UNIQUE index (UserId, Date)
```

## 2.2 API (REST) — yêu cầu đăng nhập, cô lập theo UserId
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/health/logs?from=&to=` | Lịch sử (ngày giảm dần, lọc khoảng ngày) |
| GET | `/api/health/logs/{date}` | Bản ghi của một ngày (hoặc `null`) |
| PUT | `/api/health/logs/{date}` | **Upsert** nhật ký của ngày |
| DELETE | `/api/health/logs/{date}` | Xóa nhật ký của ngày |
| GET | `/api/health/summary?date=` | Bản ghi ngày + bản ghi gần nhất trước đó (so sánh) |

`{date}` dạng `YYYY-MM-DD` (DateOnly). `summary` không có `date` → mặc định hôm nay.

## 2.3 Phân tầng & cô lập
- `HealthController → HealthService → AppDbContext`; mọi truy vấn `Where(x => x.UserId == userId)`.
- Upsert: tìm bản ghi `(userId, date)`; có → cập nhật + `UpdatedAt`; không → tạo mới.

## 2.4 Frontend (Angular, standalone + signals)
- `health.models.ts`, `health.service.ts`.
- Trang **Sức khỏe** (`/app/health`):
  - Chọn ngày (mặc định hôm nay) → tải nhật ký ngày đó vào **form upsert**; nút Lưu / Xóa.
  - Khối **So sánh** với lần ghi trước (mũi tên ▲▼ + chênh lệch cho cân nặng, giờ ngủ, nước, tập).
  - **Lịch sử gần đây** (bảng các ngày gần nhất).
- Thêm mục **♥ Sức khỏe** vào sidebar + route; cập nhật thẻ Sức khỏe ở **Hôm nay** (chỉ số hôm nay + link).

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| H1 | Entity `HealthLog` + DbContext + migration | backend | — | M |
| H2 | `HealthService` (upsert/get/list/delete/summary, cô lập UserId) | backend | H1 | M |
| H3 | `HealthController` + DTO + validation | backend | H2 | S |
| H4 | Models + `health.service` (FE) | frontend | H3 | S |
| H5 | Trang Sức khỏe: form ngày + so sánh + lịch sử | frontend | H4 | L |
| H6 | Nav + route + thẻ Hôm nay | frontend | H5 | S |
| H7 | Integration test (upsert, cô lập, summary, validate, list) + build xanh | backend/test | H3 | M |
| H8 | Cập nhật README + 03-feature-plan | docs | mọi task | S |

**Quyết định:**
- Một bản ghi/ngày (upsert) thay vì nhiều mục rời — khớp ý "nhật ký ngày" và dễ so sánh.
- "Buổi tập" lưu **số phút** (`WorkoutMinutes`) để so sánh/cộng dồn được; chi tiết để trong ghi chú.
- Thuốc/nhắc và biểu đồ hoãn sang P2.

**DoD:** upsert theo ngày chạy; so sánh hôm trước đúng; lịch sử hiển thị; dữ liệu cô lập theo user; `dotnet test` xanh; `ng build` sạch; kiểm chứng API thật.
