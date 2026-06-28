# F5 — Nhật ký (Journal) + bộ soạn thảo rich-text

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P1. Phụ thuộc: **F0** (auth + cô lập theo UserId).

> **Phase 3 · G2 — Mood tracking ✅:** thêm `JournalEntry.Mood` (nullable ≤20 ∈ excellent/good/normal/bad/terrible)
> + static `Mood`/`Score` 1–5; `mood` trong upsert/DTO (regex → 400 nếu sai); `GET /api/journal/mood-trend?from&to`
> trả `[{date,mood,score}]` (loại bài không mood, sắp ngày tăng). FE: bộ chọn 5 emoji + emoji cạnh tiêu đề +
> biểu đồ cột xu hướng (CSS). Thêm 5 test (`G2MoodTests`). Nguồn: `docs/07-gap-analysis.md` (G2).

> **Test: 61/61 pass** (thêm 5 integration cho F5: CRUD, tiêu đề quá dài → 400, lọc ngày + tìm theo
> tiêu đề/nội dung, cô lập theo user, 401). `ng build` sạch.
> Kiểm chứng API thật: lưu/đọc giữ nguyên định dạng HTML (`<h2><b><i><ul>`), tìm trong nội dung hoạt động.
> Editor: tự dựng (`core/components/rich-editor.ts`, contenteditable + execCommand, ControlValueAccessor) —
> không thêm thư viện; nội dung hiển thị qua `[innerHTML]` (Angular sanitize, chống XSS).

> Phạm vi đợt này: phần **Nhật ký** của F5 với **bộ soạn thảo rich-text nhúng** ("mini Word").
> Ghi chú nhanh (free note) có thể bổ sung sau, dùng chung hạ tầng này.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Cho người dùng **viết nhật ký theo ngày** với định dạng phong phú (đậm/nghiêng, tiêu đề, danh sách,
trích dẫn, link…), xem lại, sửa, xóa và tìm kiếm.

## 1.2 User stories
- Là người dùng, tôi muốn **viết một bài nhật ký** có tiêu đề, gắn ngày, nội dung **định dạng được** (như Word thu nhỏ).
- Là người dùng, tôi muốn **xem danh sách** nhật ký gần đây và mở lại để **sửa**.
- Là người dùng, tôi muốn **xóa** một bài và **tìm** theo từ khóa.
- Là người dùng, tôi muốn **dữ liệu tách biệt** — không ai khác đọc được.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-J1** Bài nhật ký gồm: `date` (bắt buộc, mặc định hôm nay), `title` (tùy chọn ≤200), `contentHtml` (nội dung HTML có định dạng).
- **AC-J2** Trình soạn thảo hỗ trợ tối thiểu: **đậm, nghiêng, gạch chân, gạch ngang, tiêu đề (H1/H2), đoạn thường, danh sách chấm, danh sách số, trích dẫn, chèn link, xóa định dạng**.
- **AC-J3** CRUD đầy đủ; danh sách sắp theo `date` giảm dần (rồi `updatedAt`).
- **AC-J4** Lọc theo khoảng ngày (`from`/`to`) và tìm từ khóa `q` (trong tiêu đề + nội dung).
- **AC-J5** Mọi truy vấn chỉ trả dữ liệu của **chính người dùng**; id của người khác → **404**.
- **AC-J6** Nội dung HTML khi hiển thị lại phải được **làm sạch** (chống XSS) — render qua cơ chế sanitize của Angular (`[innerHTML]`).
- **AC-J7** Giao diện tiếng Việt.

## 1.4 Trường hợp biên
- Nội dung rỗng / chỉ khoảng trắng → vẫn cho lưu (nhật ký có thể chỉ có tiêu đề), hoặc chặn nếu cả tiêu đề lẫn nội dung trống.
- HTML chứa script/handler độc → bị sanitize khi hiển thị; lưu nguyên nhưng không thực thi.
- Nội dung rất dài → cột `contentHtml` kiểu text (không giới hạn cứng ở MVP; có thể đặt trần ~100k sau).

## 1.5 Quyết định thiết kế
- **Bộ soạn thảo tự dựng** dựa trên `contenteditable` + `document.execCommand` thay vì thêm thư viện (Quill/TipTap) — tránh rủi ro tương thích Angular 20 và phụ thuộc npm; an toàn offline. Có thể thay bằng Quill sau nếu cần.
- Lưu **HTML** (không phải Markdown) cho đơn giản hiển thị; sanitize ở tầng hiển thị.

---

# 2. THIẾT KẾ (Design)

## 2.1 Mô hình dữ liệu
```
JournalEntry
  Id          Guid PK
  UserId      Guid FK → AspNetUsers (cascade)
  Date        date (DateOnly)
  Title       string? (≤200)
  ContentHtml text
  CreatedAt   DateTime
  UpdatedAt   DateTime
  index (UserId, Date)
```

## 2.2 API (REST) — `api/journal`, yêu cầu đăng nhập, cô lập theo UserId
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/journal?from=&to=&q=` | Danh sách (lọc khoảng ngày + tìm từ khóa) |
| GET | `/api/journal/{id}` | Một bài |
| POST | `/api/journal` | Tạo bài |
| PUT | `/api/journal/{id}` | Sửa bài |
| DELETE | `/api/journal/{id}` | Xóa bài |

DTO: `JournalEntryDto(Id, Date, Title, ContentHtml, CreatedAt, UpdatedAt)`;
`UpsertJournalRequest(Date, Title?, ContentHtml)`.

## 2.3 Phân tầng & cô lập
- `JournalController → JournalService → AppDbContext`; mọi truy vấn `Where(x => x.UserId == userId)`; lấy theo id luôn kèm UserId → không thấy → 404.
- Tìm kiếm: `EF.Functions.ILike` trên `Title` hoặc `ContentHtml`.

## 2.4 Frontend (Angular, standalone + signals)
- **`RichEditor`** (component tái dùng, `core/components/rich-editor`):
  - `contenteditable` div + thanh công cụ nút lệnh; mỗi nút gọi `document.execCommand`.
  - Triển khai **ControlValueAccessor** để dùng với reactive forms (`formControlName`).
  - Phát nội dung `innerHTML` khi gõ; `writeValue` đổ HTML vào div.
- `journal.models.ts`, `journal.service.ts`.
- Trang **Nhật ký** (`/app/journal`):
  - Danh sách bài (tiêu đề + ngày + trích đoạn); ô tìm + lọc ngày.
  - Nút **Viết mới** → form (ngày, tiêu đề, RichEditor) → Lưu.
  - Mở bài để **sửa** (đổ vào cùng form); **Xóa** có xác nhận.
  - Hiển thị nội dung qua `[innerHTML]` (Angular sanitize).
- Sidebar **📓 Nhật ký** + route.

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Lớn |
|---|------|---------|-----|
| J1 | Entity + DbContext + migration | backend | S |
| J2 | `JournalService` + DTO + `JournalController` | backend | M |
| J3 | `RichEditor` component (contenteditable + toolbar, CVA) | frontend | M |
| J4 | Models + `journal.service` + trang Nhật ký | frontend | L |
| J5 | Nav + route | frontend | S |
| J6 | Integration test + build xanh | backend/test | M |

**DoD:** soạn thảo có định dạng và lưu được; CRUD + tìm hoạt động; nội dung hiển thị an toàn (sanitize); dữ liệu cô lập theo user; `dotnet test` xanh; `ng build` sạch; kiểm chứng API thật.
