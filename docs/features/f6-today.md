# F6 — Tổng quan "Hôm nay"

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1 (capstone MVP). Phụ thuộc: **F1, F2, F3** (đọc dữ liệu của chúng) — tất cả đã xong.

> Không cần backend mới (tái dùng `summary?date=`). `ng build` sạch.
> Kiểm chứng API thật: đổi ngày làm đổi thu/chi (17/6: +5.000.000/−1.200.000 → 18/6: 0/0) và nhật ký
> sức khỏe (70.50kg → 69.80kg) đúng theo ngày; trạng thái rỗng hiển thị gợi ý. **🎉 Khép mốc MVP (F0+F1+F2+F3+F6).**

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Màn hình trung tâm gom **mọi thứ của một ngày** vào một chỗ; cho phép xem **ngày bất kỳ** và đi lùi/tới.

## 1.2 User stories
- Là người dùng, tôi muốn mở app và **thấy ngay tổng quan hôm nay** (tài chính, sức khỏe, công việc).
- Là người dùng, tôi muốn **chọn một ngày khác** (hôm qua, tuần trước) để xem lại.
- Là người dùng, khi một mảng **chưa có dữ liệu** ngày đó, tôi muốn **gợi ý hành động** để bắt đầu.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-6.1** Có thanh chọn ngày: nút **◀** (lùi 1 ngày), ô **ngày**, nút **▶** (tới 1 ngày), nút **Hôm nay** (về ngày hiện tại). Mặc định mở ở hôm nay.
- **AC-6.2** Khi đổi ngày, **cả 3 thẻ** (Tài chính/Sức khỏe/Công việc) tải lại theo ngày đang chọn.
- **AC-6.3** Thẻ **Tài chính**: số dư hiện tại + thu/chi của ngày đó (gọi `GET /api/finance/summary?date=`).
- **AC-6.4** Thẻ **Sức khỏe**: chỉ số nhật ký của ngày đó (gọi `GET /api/health/summary?date=` → `today`). Không có → trạng thái rỗng.
- **AC-6.5** Thẻ **Công việc**: việc xong/tổng, quá hạn (tính theo ngày đó), tiến độ mục tiêu TB, thói quen check trong ngày (gọi `GET /api/work/summary?date=`).
- **AC-6.6** Mỗi thẻ rỗng có **gợi ý + link** sang trang tương ứng để thêm dữ liệu.
- **AC-6.7** Mỗi thẻ là **link** sang trang chi tiết của mảng đó.
- **AC-6.8** Toàn bộ giao diện tiếng Việt; định dạng tiền `vi-VN`.

## 1.4 Trường hợp biên
- Ngày tương lai (▶ vượt hôm nay) vẫn cho xem; dữ liệu thường rỗng → hiển thị trạng thái rỗng.
- Ngày không có bất kỳ dữ liệu nào → cả 3 thẻ ở trạng thái rỗng có gợi ý.
- Đổi ngày nhanh nhiều lần → mỗi lần tải lại theo ngày mới nhất (không cần hủy request cũ ở MVP).

---

# 2. THIẾT KẾ (Design)

## 2.1 Backend
**Không cần thay đổi** — 3 endpoint summary đã nhận `?date=`:
- `GET /api/finance/summary?date=YYYY-MM-DD` → `{ balance, dayIncome, dayExpense, monthIncome, monthExpense }`
- `GET /api/health/summary?date=YYYY-MM-DD` → `{ today, previous }`
- `GET /api/work/summary?date=YYYY-MM-DD` → `{ tasksTotal, tasksDone, tasksOverdue, goalsCount, goalsAvgProgress, habitsTotal, habitsCheckedToday }`

> `habitsCheckedToday` và `tasksOverdue` của Work summary đã tính theo `date` truyền vào (xem `WorkService.GetSummaryAsync`).

## 2.2 Frontend (Angular)
- Nâng cấp `features/today/today.ts`:
  - `date = signal(todayIso())`; controls đổi ngày → cập nhật signal → `reload()`.
  - `reload()` gọi song song 3 service `getSummary(date)`.
  - 3 thẻ là `<a routerLink>` sang `/app/finance|health|work`; trạng thái rỗng kèm gợi ý.
  - Dùng `effect()` hoặc gọi reload thủ công trong các hàm đổi ngày.
- Tận dụng `finance.service`, `health.service`, `work.service` đã có (đều có `getSummary(date?)`).

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Lớn |
|---|------|---------|-----|
| T1 | Thanh chọn ngày (◀ / ngày / ▶ / Hôm nay) + state | frontend | S |
| T2 | 3 thẻ theo ngày + trạng thái rỗng + link | frontend | M |
| T3 | `ng build` sạch + kiểm chứng chạy thật | — | S |

**Quyết định:**
- F6 **không cần backend mới** — tái dùng các `summary?date=` đã có.
- Số dư tài chính giữ nghĩa "hiện tại" (toàn thời gian); thu/chi hiển thị theo ngày chọn — khớp ý "số dư hiện tại + hoạt động trong ngày".

**DoD:** chọn/đi ngày hoạt động; 3 thẻ cập nhật theo ngày; trạng thái rỗng có gợi ý; `ng build` sạch; kiểm chứng API thật.
