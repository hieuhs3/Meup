# G1 — Mục tiêu đa cấp + Trạng thái + Dashboard

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · **Code ✅** · **Test ✅** · Review ⏳
Ưu tiên: P1 (đợt 3A). Phụ thuộc: **F3** (Work/Goal/Task đã có). Nguồn: `docs/07-gap-analysis.md` (G1), `docs/08-phase3-plan.md`.

> **Đã triển khai** (backend + frontend + test). Test: **106/106 pass** (thêm 11 cho G1: mặc định level/status, rollup trung bình, completed=100%, cancelled/archived loại khỏi mẫu số, cha sai cấp/không tồn tại/tự làm cha → 400, cây lồng nhau, lọc level/status, xóa cascade, cô lập user). FE `ng build` sạch.
> Migration `AddGoalTree` cũng gộp luôn cột `Priority` (Tasks) còn sót chưa migrate, với default hợp lệ `"medium"`.

> **Ghi chú đính chính gap-analysis:** `07` ghi "Goal phẳng, progress nhập tay" — thực tế code hiện tại đã tính `Progress` **tự động** = % task con đã xong (`WorkService.ProgressOf`, `GoalDto.Progress`). G1 mở rộng cơ chế này thành **rollup theo cây**.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Biến `Goal` phẳng thành **cây mục tiêu nhiều cấp** có **vòng đời (trạng thái)** và **dashboard**, đúng tầm nhìn `reference.md` §3.2 (Life → Year → Quarter → Month → Weekly).

## 1.2 User stories
- Là người dùng, tôi muốn tạo mục tiêu ở **nhiều cấp** (đời/năm/quý/tháng/tuần) và **gắn mục tiêu con vào mục tiêu cha**.
- Tôi muốn mỗi mục tiêu có **trạng thái** (nháp/đang chạy/hoàn thành/hủy/lưu trữ) để quản lý vòng đời.
- Tôi muốn **tiến độ cha tự tổng hợp** từ các mục tiêu con + task trực thuộc, không phải tự cập nhật tay.
- Tôi muốn một **Goal Dashboard** xem cây mục tiêu thu/mở, badge trạng thái, thanh tiến độ theo cấp.
- Tôi muốn đặt **ngày mục tiêu (target date)** và **mô tả** ngắn cho mỗi mục tiêu.
- Dữ liệu của tôi **tách biệt** theo người dùng.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-L1** `Level` ∈ {life, year, quarter, month, week}; mặc định `year`. Giá trị khác → 400.
- **AC-L2** `Status` ∈ {draft, active, completed, cancelled, archived}; mặc định `active`. Giá trị khác → 400.
- **AC-T1** `ParentGoalId` (tùy chọn) phải là mục tiêu **của chính người dùng** và có **cấp cao hơn** (ordinal nhỏ hơn) mục tiêu con; vi phạm → 400. Không cho **chu trình** (gán cha là chính nó/con cháu) → 400.
- **AC-P1** **Tiến độ rollup** (read-only, server tính):
  - Mục tiêu **lá** (không có goal con): `progress` = % task trực thuộc đã xong (giữ hành vi hiện tại); không có task → 0.
  - Mục tiêu **cha**: `progress` = **trung bình đơn** của: [tiến độ từng goal con trực tiếp] + [% task trực thuộc trực tiếp nếu có task] (mỗi nhánh con & "rổ task" tính là 1 phần tử).
  - Mục tiêu `completed` → coi như 100% khi tính rollup cho cha; `cancelled`/`archived` → **loại khỏi** mẫu số rollup.
- **AC-D1** `GET /api/work/goals/tree` trả cây lồng nhau (kèm progress rollup + đếm con) cho dashboard.
- **AC-X1** Mọi truy vấn chỉ trả dữ liệu của chính người dùng; id người khác → 404. Thông báo lỗi/giao diện tiếng Việt.
- **AC-COMPAT** Goal cũ (đang phẳng) sau migration: `Level=year`, `Status=active`, `ParentGoalId=null` — không mất dữ liệu, vẫn hiển thị & tính progress như cũ.

## 1.4 Trường hợp biên
- Xóa mục tiêu cha → **xóa cả cây con** (cascade) và task thuộc các goal đó (GoalId cascade sẵn có). Cảnh báo ở FE trước khi xóa.
- Mục tiêu cha không có con & không task → progress 0.
- Gán cha sai cấp / tạo vòng → 400, không lưu.
- Mục tiêu con `archived/cancelled` không kéo tụt tiến độ cha.

## 1.5 Ngoài phạm vi (để sau)
- Lịch sử tiến độ theo thời gian (biểu đồ trend) → gộp vào F7/Stats sau.
- Kéo-thả sắp xếp lại cây trên dashboard (làm bản đọc/CRUD trước).
- Liên kết Goal ↔ Skill (G7) — khi có Career.

---

# 2. THIẾT KẾ (Design)

## 2.1 Thay đổi mô hình dữ liệu — `Goal` (mở rộng, gắn `UserId`)
```
Goal (mở rộng)
  Id Guid PK · UserId Guid FK(cascade)
  Name string(≤150)
  Description string(≤1000)?          ← MỚI
  Level string(≤10)  = "year"          ← MỚI  (life|year|quarter|month|week)
  Status string(≤12) = "active"        ← MỚI  (draft|active|completed|cancelled|archived)
  TargetDate date?                     ← MỚI
  ParentGoalId Guid?  FK(self, cascade)← MỚI
  Progress int   (KHÔNG dùng nhập tay — server tính rollup; giữ cột cho tương thích/cache)
  CreatedAt DateTime
  index (UserId) · index (UserId, ParentGoalId)
```
Static class (kiểu `TaskPriority`): `GoalLevel` (+ `Ordinal(level)`), `GoalStatus` (+ `IsValid`, `Normalize`).
`Ordinal`: life=0, year=1, quarter=2, month=3, week=4. Ràng buộc cha-con: `Ordinal(parent) < Ordinal(child)`.

## 2.2 Cấu hình EF (`AppDbContext`)
```csharp
builder.Entity<Goal>(e =>
{
    e.Property(x => x.Name).HasMaxLength(150).IsRequired();
    e.Property(x => x.Description).HasMaxLength(1000);
    e.Property(x => x.Level).HasMaxLength(10).HasDefaultValue(GoalLevel.Year);
    e.Property(x => x.Status).HasMaxLength(12).HasDefaultValue(GoalStatus.Active);
    e.HasIndex(x => x.UserId);
    e.HasIndex(x => new { x.UserId, x.ParentGoalId });
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.Parent).WithMany(g => g.Children)
        .HasForeignKey(x => x.ParentGoalId).OnDelete(DeleteBehavior.Cascade); // xóa cha → xóa cây con
});
```
> Lưu ý cascade nhiều đường (Postgres cho phép): xóa Goal cha → cascade Goal con (self-ref) **và** Task (qua `Task.GoalId` cascade sẵn có). Cần migration kiểm tra không tạo vòng cascade lỗi trên Postgres.

## 2.3 Thuật toán rollup tiến độ (trong `WorkService`)
Tải **1 lượt** mọi goal của user + tổng hợp task theo GoalId (đã có sẵn pattern `agg` trong `GetGoalsAsync`), rồi tính đệ quy trên cây trong bộ nhớ (tránh N+1):
```
progress(goal):
  buckets = []
  for child in directChildren(goal) where child.Status not in {cancelled, archived}:
      buckets.add( child.Status == completed ? 100 : progress(child) )
  if goal has direct tasks:
      buckets.add( round(doneTasks/totalTasks * 100) )
  return buckets.empty ? 0 : round(average(buckets))
```
Memoize theo Id để mỗi node tính 1 lần.

## 2.4 API (REST) — gốc `api/work`, đăng nhập, cô lập UserId
| Method | Endpoint | Ghi chú |
|--------|----------|---------|
| GET | `/api/work/goals?level=&status=` | Danh sách phẳng (lọc tùy chọn) + progress rollup — **mở rộng** endpoint cũ |
| GET | `/api/work/goals/tree` | **MỚI** — cây lồng nhau cho dashboard |
| POST | `/api/work/goals` | Tạo: thêm `level`, `status`, `description`, `targetDate`, `parentGoalId` |
| PUT | `/api/work/goals/{id}` | Sửa: tên + 5 trường trên (validate cấp cha + chống vòng) |
| DELETE | `/api/work/goals/{id}` | Xóa cây con (cascade) |

`/summary` (Hôm nay) giữ nguyên hợp đồng; `goalsAvgProgress` nay tính trên rollup.

## 2.5 DTO (mở rộng `WorkDtos.cs`)
- `GoalDto` += `Level`, `Status`, `Description?`, `TargetDate?`, `ParentGoalId?`, `ChildCount`. (Giữ `Progress`, `TaskCount`, `DoneCount`.)
- `GoalTreeNodeDto(... , IReadOnlyList<GoalTreeNodeDto> Children)` cho endpoint tree.
- `CreateGoalRequest`/`UpdateGoalRequest` += `Level`, `Status`, `Description?`, `TargetDate?`, `ParentGoalId?` với `[RegularExpression]` cho level/status (thông báo tiếng Việt như các DTO khác).

## 2.6 Frontend (Angular, standalone + signals)
- `work.models.ts`: thêm enum level/status, trường mới, kiểu `GoalTreeNode`.
- `work.service.ts`: thêm `getGoalTree()`; cập nhật create/update.
- **Goal Dashboard** (thay khối "Mục tiêu" phẳng trong trang Work `/app/work`):
  - Cây thu/mở theo cấp; mỗi node: tên, **badge cấp** + **badge trạng thái** (màu), **thanh tiến độ rollup**, nút thêm-con/sửa/xóa.
  - Form tạo/sửa: chọn cấp, trạng thái, mục tiêu cha (lọc theo cấp hợp lệ), ngày mục tiêu, mô tả.
  - Xác nhận khi xóa (cảnh báo "xóa cả cây con & task thuộc").
- Thẻ "Hôm nay" giữ nguyên (dùng `goalsAvgProgress`).

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G1.1 | Mở rộng entity `Goal` + `GoalLevel`/`GoalStatus`; cấu hình EF self-ref; **migration** (default level/status cho rows cũ) | backend | — | M |
| G1.2 | `WorkService`: CRUD đa cấp + **rollup đệ quy** (tải 1 lượt) + validate cấp cha/chống vòng + lọc level/status + `GetGoalTreeAsync` | backend | G1.1 | L |
| G1.3 | `WorkController` + `WorkDtos`: endpoint tree, tham số lọc, DTO mở rộng + validation | backend | G1.2 | M |
| G1.4 | FE models + `work.service` (tree + trường mới) | frontend | G1.3 | S |
| G1.5 | **Goal Dashboard** (cây thu/mở, badge, progress, form cha-con) thay khối goal phẳng | frontend | G1.4 | L |
| G1.6 | Integration test: tạo cây, rollup (gồm completed/cancelled), validate cấp/vòng, lọc, cô lập user, compat goal cũ; `dotnet test` xanh; `ng build` sạch | test | G1.3 | M |
| G1.7 | Cập nhật `README`, `03-feature-plan` (Phần B), `00-process` (bảng theo dõi) | docs | mọi | S |

**Quyết định đã chốt (default — đổi được):**
- Rollup = **trung bình đơn** (mỗi goal con + rổ task = 1 phần tử ngang nhau). *Không* dùng trọng số ở v1.
- Ràng buộc cấp cha-con: **cha phải cấp cao hơn** (ordinal nhỏ hơn), **không bắt buộc liền kề** (cho linh hoạt: Year → Month vẫn hợp lệ).
- `completed` = 100% trong rollup; `cancelled`/`archived` **loại khỏi** mẫu số.
- Goal cũ → mặc định `Level=year`, `Status=active`.
- Xóa cha = **xóa cả cây con** (cascade) — có xác nhận ở FE.

**DoD:** tạo/sửa/xóa goal đa cấp; tiến độ cha tự tổng hợp đúng (kể cả completed/cancelled); validate cấp & chống vòng; dashboard cây hiển thị đúng; goal cũ tương thích; `dotnet test` xanh; `ng build` sạch; kiểm chứng API thật (tạo cây 2–3 cấp → progress rollup khớp).
