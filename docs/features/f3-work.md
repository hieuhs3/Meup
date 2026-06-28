# F3 — Công việc & Mục tiêu & Thói quen

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1 (MVP). Phụ thuộc: **F0** (auth + cô lập theo UserId).

> **Phase 3 mở rộng:**
> - **G1 — Mục tiêu đa cấp ✅** (cây đời→năm→quý→tháng→tuần + trạng thái + rollup + dashboard): xem `docs/features/g1-goal-tree.md`.
> - **G3 — Habit nâng cấp ✅:** thêm `Frequency` (daily/weekly) + `TargetPerWeek`; DTO trả `bestStreak`, `completionRate`
>   (30 ngày), `recentChecks` (12 tuần). FE: form tần suất + thống kê + **heatmap** 12 tuần. Thêm 4 test (`G3HabitHeatmapTests`).
> - **G11 — Kanban ✅:** `TaskItem.Status` (todo/in_progress/review/done/cancelled), đồng bộ `IsDone`;
>   `PUT /api/work/tasks/{id}/status`; FE có công tắc **Cây / Bảng Kanban** (5 cột, đổi trạng thái bằng dropdown).
>   Thêm 5 test (`G11TaskStatusTests`). Sprint vẫn hoãn.

> **Test: 56/56 pass** (thêm 11 integration cho F3: task quá hạn + toggle, lọc trạng thái, validate,
> goal tiến độ + ngoài khoảng, habit streak liên tiếp + idempotent + bỏ check, streak đứt khi có khoảng trống,
> summary đếm số liệu, cô lập theo user, 401).
> Kiểm chứng API thật: task quá hạn `isOverdue=true`; habit 3 ngày liên tiếp → `streak=3`; summary đúng.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Quản lý **việc cần làm (task)**, **mục tiêu dài hạn (goal)** và **thói quen (habit)** trong một chỗ.

## 1.2 User stories
- Là người dùng, tôi muốn **thêm/sửa/xóa task**, **đánh dấu hoàn thành**, đặt **hạn (due)**; task **quá hạn** được làm nổi bật.
- Là người dùng, tôi muốn tạo **mục tiêu** với **tiến độ 0–100%** và cập nhật tiến độ.
- Là người dùng, tôi muốn tạo **thói quen** và **check theo ngày**, xem **chuỗi ngày liên tiếp (streak)**.
- Là người dùng, tôi muốn **dữ liệu của tôi tách biệt** — không ai khác đọc được.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-T1** Task: `title` bắt buộc (≤200); `dueDate` tùy chọn; `isDone` mặc định false. Sửa/xóa/đánh dấu hoàn thành được.
- **AC-T2** Task **quá hạn** = chưa hoàn thành **và** `dueDate < hôm nay`; API trả cờ `isOverdue`.
- **AC-T3** Lọc task theo trạng thái: `all | active | done`.
- **AC-G1** Goal: `name` bắt buộc (≤150); `progress` là số nguyên **0–100**; ngoài khoảng → 400.
- **AC-H1** Habit: `name` bắt buộc (≤150). Check/bỏ check theo một **ngày**; mỗi (habit, ngày) tối đa một lần (idempotent).
- **AC-H2** `streak` = số ngày **liên tiếp tính lùi từ ngày tham chiếu** mà thói quen được check; nếu ngày tham chiếu chưa check → streak = 0.
- **AC-X1** Mọi truy vấn chỉ trả dữ liệu của **chính người dùng**; id của người khác → **404**.
- **AC-X2** Toàn bộ thông báo lỗi/giao diện **tiếng Việt**.

## 1.4 Trường hợp biên
- Task không title; dueDate quá khứ (vẫn tạo được, đánh dấu quá hạn nếu chưa xong).
- Goal progress âm / > 100 → 400.
- Check habit hai lần cùng ngày → không tạo trùng (giữ một bản ghi); bỏ check ngày chưa check → không lỗi.
- Streak có ngày trống ở giữa → dừng đếm tại khoảng trống.

## 1.5 Ngoài phạm vi (để P2)
- Lặp lại task (recurring), nhắc nhở (thuộc F4).
- Mục tiêu con / cây mục tiêu; lịch sử tiến độ.
- Biểu đồ streak (F7).

---

# 2. THIẾT KẾ (Design)

## 2.1 Mô hình dữ liệu (mỗi bảng gắn `UserId`)
```
TaskItem
  Id Guid PK · UserId Guid FK(cascade)
  Title string(≤200) · IsDone bool
  DueDate date? · CompletedAt DateTime? · CreatedAt DateTime
  index (UserId, IsDone)

Goal
  Id Guid PK · UserId Guid FK(cascade)
  Name string(≤150) · Progress int(0–100) · CreatedAt DateTime
  index (UserId)

Habit
  Id Guid PK · UserId Guid FK(cascade)
  Name string(≤150) · CreatedAt DateTime
  index (UserId)

HabitCheck
  Id Guid PK · HabitId Guid FK(cascade) · UserId Guid
  Date date
  UNIQUE index (HabitId, Date)
```

## 2.2 API (REST) — gốc `api/work`, yêu cầu đăng nhập, cô lập theo UserId
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/work/tasks?status=all|active|done` | Danh sách task (kèm `isOverdue`) |
| POST | `/api/work/tasks` | Tạo task |
| PUT | `/api/work/tasks/{id}` | Sửa task (title, dueDate, isDone) |
| POST | `/api/work/tasks/{id}/toggle` | Đảo trạng thái hoàn thành |
| DELETE | `/api/work/tasks/{id}` | Xóa task |
| GET | `/api/work/goals` | Danh sách mục tiêu |
| POST | `/api/work/goals` | Tạo mục tiêu |
| PUT | `/api/work/goals/{id}` | Sửa (tên + tiến độ) |
| DELETE | `/api/work/goals/{id}` | Xóa mục tiêu |
| GET | `/api/work/habits?date=` | Danh sách thói quen (kèm `checked` & `streak` tại `date`) |
| POST | `/api/work/habits` | Tạo thói quen |
| PUT | `/api/work/habits/{id}` | Đổi tên |
| DELETE | `/api/work/habits/{id}` | Xóa thói quen |
| POST | `/api/work/habits/{id}/check?date=` | Check thói quen cho ngày (idempotent) |
| DELETE | `/api/work/habits/{id}/check?date=` | Bỏ check |
| GET | `/api/work/summary?date=` | Tổng quan cho "Hôm nay" |

`summary` trả: tasksTotal/Done/Overdue, goalsCount/avgProgress, habitsTotal/checkedToday.

## 2.3 Phân tầng & cô lập
- `WorkController (api/work) → WorkService → AppDbContext`; mọi truy vấn `Where(x => x.UserId == userId)`.
- Lấy theo id luôn kèm UserId; không thấy → null → 404.

## 2.4 Tính streak
- Tải các ngày đã check của habit (cửa sổ ~1 năm) vào tập hợp; đếm lùi từ `date`:
  `while set chứa d: streak++; d = d - 1 ngày`. Ngày tham chiếu chưa check → streak 0.

## 2.5 Frontend (Angular, standalone + signals)
- `work.models.ts`, `work.service.ts`.
- Trang **Công việc** (`/app/work`), 3 khối:
  - **Việc cần làm**: form thêm nhanh (title + hạn), danh sách có checkbox hoàn thành, hạn, **quá hạn nổi bật (đỏ)**, lọc all/active/done, xóa.
  - **Mục tiêu**: thêm, thanh tiến độ + chỉnh %, xóa.
  - **Thói quen**: thêm, nút check hôm nay (đổi trạng thái), hiển thị **🔥 streak**, xóa.
- Sidebar **✓ Công việc** + route; thẻ "Hôm nay" hiện số liệu tổng quan + link.

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| W1 | Entities (TaskItem, Goal, Habit, HabitCheck) + DbContext + migration | backend | — | M |
| W2 | `WorkService` (task/goal/habit CRUD + streak + summary, cô lập UserId) | backend | W1 | L |
| W3 | `WorkController` + DTO + validation | backend | W2 | M |
| W4 | Models + `work.service` (FE) | frontend | W3 | S |
| W5 | Trang Công việc: 3 khối task/goal/habit | frontend | W4 | L |
| W6 | Nav + route + thẻ Hôm nay | frontend | W5 | S |
| W7 | Integration test (task/goal/habit, streak, cô lập, summary) + build xanh | backend/test | W3 | M |
| W8 | Cập nhật README + 03-feature-plan | docs | mọi task | S |

**Quyết định:**
- Gộp 3 nhóm vào **một** controller `api/work` + **một** `WorkService` cho gọn (giống FinanceService gộp category+transaction).
- Entity đặt tên `TaskItem` (tránh trùng `System.Threading.Tasks.Task`).
- Streak tính lùi từ ngày tham chiếu; recurring/nhắc/biểu đồ hoãn P2.

**DoD:** task CRUD + toggle + cờ quá hạn; goal tiến độ; habit check + streak đúng; dữ liệu cô lập; `dotnet test` xanh; `ng build` sạch; kiểm chứng API thật.
