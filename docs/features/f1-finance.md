# F1 — Tài chính

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1 (MVP). Phụ thuộc: **F0** (auth + cô lập theo UserId).

> **Test: 36/36 pass** (thêm 11 integration cho F1: seed danh mục mặc định, xóa danh mục gỡ liên kết,
> tổng hợp số dư, validate số tiền/danh mục/khác loại, lọc theo loại & từ khóa, phân trang, cô lập theo user, 401).
> Kiểm chứng API thật: số dư = thu − chi đúng (vd 5.000.000 − 1.200.000 = 3.800.000), tổng ngày/tháng đúng.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Theo dõi thu/chi theo ngày: ghi giao dịch, phân loại theo danh mục, xem số dư và tổng thu/chi
theo ngày và theo tháng. Tiền tệ: **VND**.

## 1.2 User stories
- Là người dùng, tôi muốn **ghi một khoản thu hoặc chi** (số tiền, danh mục, ngày, ghi chú).
- Là người dùng, tôi muốn **xem số dư hiện tại** và **tổng thu/chi** theo ngày, theo tháng.
- Là người dùng, tôi muốn **phân loại** giao dịch theo danh mục của riêng tôi.
- Là người dùng, tôi muốn **lọc / tìm / sửa / xóa** giao dịch.
- Là người dùng, tôi muốn **dữ liệu tài chính của tôi tách biệt** — không ai khác đọc được.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-F1** Tạo giao dịch: `type` ∈ {income, expense}; `amount` > 0; `date` bắt buộc; `note` tùy chọn; `categoryId` tùy chọn.
- **AC-F2** Nếu có `categoryId`: danh mục phải **thuộc về người dùng** và **cùng loại** (`type`) với giao dịch; ngược lại → 400.
- **AC-F3** Mọi truy vấn chỉ trả dữ liệu của **chính người dùng**; truy cập id của người khác → **404** (không lộ tồn tại).
- **AC-F4** Số dư = Σ thu − Σ chi (toàn bộ). Tổng ngày/tháng tính theo `date` (không theo giờ tạo).
- **AC-F5** Lọc giao dịch theo: khoảng ngày (`from`/`to`), `type`, `categoryId`, từ khóa `q` (trong ghi chú). Có phân trang.
- **AC-F6** Sửa/xóa giao dịch & danh mục chỉ tác động bản ghi của mình.
- **AC-F7** Xóa danh mục: các giao dịch liên quan **không bị xóa**, chỉ gỡ liên kết (`categoryId` = null).
- **AC-F8** Lần đầu chưa có danh mục → hệ thống tạo sẵn **bộ danh mục mặc định** (tiếng Việt).
- **AC-F9** Toàn bộ thông báo lỗi/giao diện **tiếng Việt**.

## 1.4 Trường hợp biên
- Số tiền 0 / âm / không phải số → 400.
- `type` sai giá trị; `date` thiếu/sai định dạng.
- `categoryId` không tồn tại / của người khác / khác loại.
- Lọc khoảng ngày đảo (from > to) → trả rỗng (không lỗi).
- Phân trang vượt quá số trang → danh sách rỗng, `total` vẫn đúng.

## 1.5 Ngoài phạm vi (để P2)
- **Ngân sách theo danh mục** (budget) và cảnh báo vượt ngân sách.
- Biểu đồ xu hướng (sẽ làm ở F7 — Thống kê).
- Nhiều loại tiền tệ / quy đổi.

---

# 2. THIẾT KẾ (Design)

## 2.1 Mô hình dữ liệu (mỗi bảng gắn `UserId` để cô lập)
```
Category
  Id        Guid PK
  UserId    Guid FK → AspNetUsers (cascade)
  Name      string (≤50)
  Type      string ("income" | "expense")
  Color     string? (≤7, vd "#4361ee")
  CreatedAt DateTime
  index (UserId, Type)

Transaction
  Id         Guid PK
  UserId     Guid FK → AspNetUsers (cascade)
  Type       string ("income" | "expense")
  Amount     decimal(18,2)  (> 0)
  CategoryId Guid? FK → Category (SET NULL khi xóa danh mục)
  Date       date (DateOnly)
  Note       string? (≤500)
  CreatedAt  DateTime
  index (UserId, Date)
```
> `Type` lưu dạng chuỗi (giống quy ước `Roles`) cho JSON đơn giản; hằng trong `FinanceType`.

## 2.2 API (REST) — đều yêu cầu đăng nhập, cô lập theo UserId
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/finance/categories?type=` | Danh sách danh mục (tự tạo mặc định nếu rỗng) |
| POST | `/api/finance/categories` | Tạo danh mục |
| PUT | `/api/finance/categories/{id}` | Sửa danh mục |
| DELETE | `/api/finance/categories/{id}` | Xóa danh mục (gỡ liên kết giao dịch) |
| GET | `/api/finance/transactions?from=&to=&type=&categoryId=&q=&page=&pageSize=` | Danh sách (lọc + phân trang) |
| POST | `/api/finance/transactions` | Tạo giao dịch |
| PUT | `/api/finance/transactions/{id}` | Sửa giao dịch |
| DELETE | `/api/finance/transactions/{id}` | Xóa giao dịch |
| GET | `/api/finance/summary?date=YYYY-MM-DD` | Số dư + tổng thu/chi theo ngày & tháng của `date` |

**Phân trang:** `page` (1-based, mặc định 1), `pageSize` (mặc định 20, tối đa 100). Trả `{ items, total, page, pageSize }`.

## 2.3 Phân tầng & cô lập dữ liệu
- `Controller (FinanceController) → FinanceService → AppDbContext`.
- `FinanceService` nhận `userId` rõ ràng; **mọi** truy vấn `Where(x => x.UserId == userId)`.
- Lấy bản ghi theo id luôn kèm điều kiện UserId; không thấy → trả null → controller 404.

## 2.4 Danh mục mặc định (seed lần đầu)
- Chi: Ăn uống, Đi lại, Mua sắm, Hóa đơn, Giải trí, Sức khỏe, Khác.
- Thu: Lương, Thưởng, Đầu tư, Khác.

## 2.5 Frontend (Angular, standalone + signals)
- `finance.models.ts`, `finance.service.ts`.
- Trang **Tài chính** (`/app/finance`):
  - Thẻ tổng quan: **Số dư**, **Thu/Chi hôm nay**, **Thu/Chi tháng này** (gọi `/summary`).
  - Form thêm giao dịch nhanh (loại, số tiền, danh mục, ngày mặc định hôm nay, ghi chú).
  - Danh sách giao dịch + bộ lọc (khoảng ngày, loại, danh mục, từ khóa) + sửa/xóa + phân trang.
  - Khu quản lý danh mục (thêm/sửa/xóa).
- Định dạng tiền `vi-VN` (vd `1.250.000 ₫`).
- Thêm mục **💰 Tài chính** vào sidebar + route; cập nhật thẻ Tài chính ở **Hôm nay** (link sang trang).

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G1 | Entity `Category`, `Transaction` + DbContext + migration | backend | — | M |
| G2 | `FinanceService` (CRUD + lọc + summary + seed mặc định, cô lập UserId) | backend | G1 | L |
| G3 | `FinanceController` + DTO + validation | backend | G2 | M |
| G4 | Models + `finance.service` (FE) | frontend | G3 | S |
| G5 | Trang Tài chính: tổng quan + form + danh sách/lọc + danh mục | frontend | G4 | L |
| G6 | Nav + route + thẻ Hôm nay | frontend | G5 | S |
| G7 | Integration test (CRUD, cô lập, lọc, summary) + build xanh | backend/test | G3 | M |
| G8 | Cập nhật README + 03-feature-plan | docs | mọi task | S |

**Quyết định:**
- Danh mục **thuộc về user** (không dùng enum cố định) để người dùng tự tùy biến; seed bộ mặc định lần đầu.
- Xóa danh mục **không** xóa giao dịch (SET NULL) — tránh mất dữ liệu lịch sử.
- Ngân sách (budget) hoãn sang P2.

**DoD:** CRUD giao dịch & danh mục chạy; số dư + tổng ngày/tháng đúng; lọc/tìm hoạt động; dữ liệu cô lập theo user; `dotnet test` xanh; `ng build` sạch; kiểm chứng API chạy thật.
