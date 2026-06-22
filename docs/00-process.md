# MeUp — Quy trình & Agents

Tài liệu này định nghĩa các **vai trò (agents)** và **luồng làm việc (flow)** cho dự án.
Nguyên tắc cốt lõi: **không code khi chưa có tài liệu chốt.**
Quy trình được áp dụng **riêng cho từng chức năng (feature)**, không làm gộp cả app một lần.

## 1. Các agent (vai trò)

| Agent | Vai trò | Đầu ra |
|-------|---------|--------|
| `spec-analyst` | Phân tích yêu cầu | tài liệu yêu cầu của feature |
| `architect` | Thiết kế kiến trúc & data model | tài liệu thiết kế của feature |
| `planner` | Lập kế hoạch: chia nhỏ task, thứ tự, phụ thuộc | bảng task của feature |
| `coder` | Lập trình | mã nguồn |
| `tester` | Kiểm thử | test + báo cáo lỗi |
| `reviewer` | Review trước khi deploy | báo cáo review |

Mỗi agent được định nghĩa trong `.claude/agents/`.

## 2. Pipeline cho MỖI chức năng

Mỗi chức năng đi qua **7 trạng thái** theo thứ tự. Mỗi trạng thái có cổng duyệt
(người dùng duyệt mới sang bước sau):

```
[1] Yêu cầu        →  spec-analyst   →  user stories + tiêu chí chấp nhận
        │
[2] Thiết kế       →  architect      →  API, data model, luồng dữ liệu
        │
[3] Lập kế hoạch   →  planner        →  chia task nhỏ, thứ tự, phụ thuộc, ước lượng   ★ MỚI
        │
[4] Code           →  coder          →  mã nguồn theo kế hoạch
        │
[5] Test           →  tester         →  test pass / báo lỗi  (lỗi → quay lại [4])
        │
[6] Review         →  reviewer       →  chốt chất lượng
        │
[7] Deploy         →                 →  đóng gói + triển khai
```

Một chức năng phải hoàn tất (hoặc tới mốc đã thống nhất) trước khi bắt đầu chức năng kế.

## 3. Trạng thái của một chức năng (status)

Mỗi chức năng mang một trong các trạng thái sau (dùng cho bảng theo dõi bên dưới):

`Chưa bắt đầu` · `Yêu cầu` · `Thiết kế` · **`Lập kế hoạch`** · `Code` · `Test` · `Review` · `Deploy` · `Hoàn tất`

## 4. Bảng theo dõi chức năng

Danh sách chi tiết & kế hoạch từng chức năng: xem `docs/03-feature-plan.md`.

| # | Chức năng | Ưu tiên | Trạng thái |
|---|-----------|---------|------------|
| F0 | Nền tảng & Auth (đăng ký/đăng nhập/quản lý user) | P0 | ✅ HOÀN TẤT (backend + frontend + test) |
| F1 | Tài chính | P1 | Chưa bắt đầu |
| F2 | Sức khỏe | P1 | Chưa bắt đầu |
| F3 | Công việc & mục tiêu/thói quen | P1 | Chưa bắt đầu |
| F4 | Lịch trình & Sự kiện | P2 | Chưa bắt đầu |
| F5 | Ghi chú & Nhật ký | P2 | Chưa bắt đầu |
| F6 | Tổng quan / Hôm nay | P1 | Chưa bắt đầu |
| F7 | Thống kê & Báo cáo | P2 | Chưa bắt đầu |
| F8 | Cài đặt | P2 | Chưa bắt đầu |

**Mốc MVP** = F0 + F1 + F2 + F3 + F6.

## 5. Phạm vi tổng thể (đã chốt với người dùng)

- Nền tảng: **Web app full-stack, đa người dùng** (client–server).
- **Tài khoản & bảo mật:** đăng ký / đăng nhập / quản lý người dùng; dữ liệu mỗi người tách biệt.
- **Khả năng mở rộng (scale):** backend API tách biệt frontend, DB quan hệ, sẵn sàng cache & mở rộng ngang.
- **Trọng tâm theo ngày:** màn hình "Hôm nay" gom mọi thứ của user trong một ngày.
- Công nghệ (đã chốt):
  - **Backend:** ASP.NET Core Web API (.NET) — REST.
  - **Frontend:** Angular.
  - **Database:** PostgreSQL qua EF Core.
  - **Auth:** ASP.NET Core Identity + JWT (access + refresh token).
  - **Scale/triển khai:** Redis cache, Docker.
