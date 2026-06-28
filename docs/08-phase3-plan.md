# MeUp — Kế hoạch phát triển Phase 3 (Task Breakdown)

Chia nhỏ các khoảng cách trong `docs/07-gap-analysis.md` (G1–G11) thành **task có thứ tự, phụ thuộc, ước lượng** —
sẵn sàng đưa vào pipeline `coder → tester → reviewer`.

> Mỗi gap là **một feature** chạy qua pipeline 7 trạng thái (`docs/00-process.md`). Tài liệu này là đầu ra của bước **[3] Lập kế hoạch** ở cấp roadmap.
> Khi bắt đầu một gap, sẽ tạo `docs/features/<mã>.md` riêng (Yêu cầu + Thiết kế + Plan + DoD) theo mẫu các feature đã xong.
> Ước lượng kích thước: **S** ≤ ~nửa ngày · **M** ~1 ngày · **L** ~2–3 ngày.

---

## Quy ước: mẫu task chuẩn cho 1 module CRUD

Mọi module nghiệp vụ ở MeUp đều theo cùng "khung 8 bước" (xem F3 đã làm). Dùng làm checklist nền:

| Bước | Phạm vi | File điển hình |
|------|---------|----------------|
| ① Entity + DbContext + Migration | backend | `Entities/*.cs`, `Data/AppDbContext.cs`, `dotnet ef migrations add` |
| ② Service (CRUD + nghiệp vụ, cô lập `UserId`) | backend | `Services/I*Service.cs` + `Services/*Service.cs` |
| ③ Controller + DTO + validation | backend | `Controllers/*Controller.cs`, `Dtos/*Dtos.cs` |
| ④ Models + service (FE) | frontend | `core/models/*.ts`, `core/services/*.ts` |
| ⑤ Trang/UI | frontend | `features/<x>/*.ts/.html` |
| ⑥ Nav + route + thẻ "Hôm nay" | frontend | `app.routes.ts`, `layout/shell.ts`, `features/today` |
| ⑦ Integration test + build xanh | backend/test | `MeUp.Tests/Integration/*.cs` |
| ⑧ Docs (`features/<x>.md`) + cập nhật bảng theo dõi | docs | `docs/features/`, `03-feature-plan.md`, `00-process.md` |

Mỗi bảng task bên dưới chỉ nêu **phần khác biệt/đáng lưu ý** của gap đó; các bước cơ học theo khung trên.

---

# ĐỢT 3A — Quick wins (đào sâu cái sẵn có) · P1

Rủi ro thấp, tái dùng pattern + hạ tầng Stats/biểu đồ. Khuyến nghị làm **G2 trước** (nhỏ, làm nóng), rồi **G1** (giá trị cao nhất).

## G1 · Goal Management đa cấp + trạng thái + dashboard  ✅ ĐÃ XONG

> Hoàn tất: backend + FE + 11 test (106/106). Chi tiết & DoD: `docs/features/g1-goal-tree.md`.

Mở rộng `Goal` (hiện chỉ `Name` + `Progress`) thành cây có cấp + vòng đời. **Tái dùng pattern cây Task** (`ParentTaskId`, commit `ddc436c`).

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G1.1 | Mở rộng `Goal`: `ParentGoalId?`, `Level` (life/year/quarter/month/week), `Status` (draft/active/completed/cancelled/archived), `TargetDate?`, `Description?`. Thêm static class `GoalLevel` + `GoalStatus` (kiểu `TaskPriority`). Cấu hình self-ref + index trong `AppDbContext`. Migration. | backend | — | M |
| G1.2 | `WorkService`: CRUD goal đa cấp; **rollup tiến độ** cha = trung bình con (đệ quy); lọc theo level/status; query cây 1 lượt. | backend | G1.1 | L |
| G1.3 | Mở rộng `WorkController`/`WorkDtos`: tham số `level`, `status`, `parentGoalId`; validate level/status hợp lệ; endpoint `GET /api/work/goals/tree`. | backend | G1.2 | M |
| G1.4 | FE models + service: kiểu cây goal, enum level/status. | frontend | G1.3 | S |
| G1.5 | **Goal Dashboard**: cây mục tiêu thu/mở theo cấp, badge trạng thái, thanh tiến độ rollup. Thay khối "Mục tiêu" phẳng hiện tại trong trang Work. | frontend | G1.4 | L |
| G1.6 | Integration test: tạo cây, rollup tiến độ, lọc level/status, cô lập user; FE build. | test | G1.3 | M |
| G1.7 | `docs/features/g1-goal-tree.md` + cập nhật bảng theo dõi. | docs | mọi | S |

**Chốt trước khi code:** công thức rollup = *trung bình đơn* hay *trọng số*? Goal con khác level cha có hợp lệ không (gợi ý: chỉ cho con đúng 1 cấp dưới)?

## G2 · Mood tracking cho Journal  ✅ ĐÃ XONG  *(5 test; chi tiết: `docs/features/f5-journal.md`)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G2.1 | `JournalEntry` + cột `Mood?` (string ≤20: excellent/good/normal/bad/terrible) + static class `Mood`. Migration. | backend | — | S |
| G2.2 | `JournalService`/Controller/DTO: nhận + trả `mood`; endpoint `GET /api/journal/mood-trend?from&to` trả chuỗi mood theo ngày. | backend | G2.1 | S |
| G2.3 | FE: mood selector (5 emoji) trong form nhật ký; biểu đồ xu hướng mood (tái dùng biểu đồ CSS của Stats). | frontend | G2.2 | M |
| G2.4 | Test mood lưu/trả + trend; gắn mood vào Stats tuần/tháng. | test | G2.2 | S |
| G2.5 | Cập nhật `docs/features/f5-journal.md`. | docs | mọi | S |

## G4 · Finance — Assets & Net Worth  ✅ ĐÃ XONG  *(5 test; chi tiết: `docs/features/f1-finance.md`)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G4.1 | Entity `Asset { Type(cash/bank/stock/crypto/gold), Name, Value numeric(18,2), Currency, UpdatedAt }` + `AssetType`. (Tùy chọn `AssetSnapshot` cho Net Worth trend — có thể hoãn.) DbContext + migration. | backend | — | M |
| G4.2 | `FinanceService`: CRUD asset; báo cáo **Net Worth** (tổng asset), **Saving Rate** ((thu−chi)/thu theo tháng), **Cash Flow** theo tháng. | backend | G4.1 | M |
| G4.3 | Controller + DTO: `GET/POST/PUT/DELETE /api/finance/assets`, `GET /api/finance/networth`. | backend | G4.2 | S |
| G4.4 | FE models + service. | frontend | G4.3 | S |
| G4.5 | Tab/khối **Tài sản** trong trang Finance: danh sách theo loại, tổng Net Worth, thẻ Saving Rate; biểu đồ Cash Flow (CSS). | frontend | G4.4 | M |
| G4.6 | Test asset CRUD + tính net worth/saving rate; cô lập user. | test | G4.3 | M |
| G4.7 | Cập nhật `docs/features/f1-finance.md`. | docs | mọi | S |

## G3 · Habit nâng cấp (frequency, %, best streak, heatmap)  ✅ ĐÃ XONG  *(4 test; chi tiết: `docs/features/f3-work.md`)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G3.1 | `Habit` + `Frequency` (daily/weekly), `TargetPerWeek?`. Migration. | backend | — | S |
| G3.2 | `WorkService`: tính `BestStreak`, `CompletionRate` (theo cửa sổ), trả dữ liệu heatmap (các ngày check trong N tuần). | backend | G3.1 | M |
| G3.3 | Controller/DTO mở rộng habit: trả best streak, completion %, mảng ngày check. | backend | G3.2 | S |
| G3.4 | FE: component **heatmap** (lưới ngày kiểu GitHub), hiển thị best streak + %. | frontend | G3.3 | M |
| G3.5 | Test best streak (có khoảng trống), completion rate; FE build. | test | G3.3 | S |
| G3.6 | Cập nhật `docs/features/f3-work.md`. | docs | mọi | S |

---

# ĐỢT 3B — Module mới (độc lập) · P2

Mỗi module tự chứa, theo trọn "khung 8 bước". Có thể làm song song.

## G7 · Career Module (Skills / Certifications / Projects)  ✅ ĐÃ XONG  *(5 test)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G7.1 | Entities `Skill { Name, Level(1–5), Category }`, `Certification { Name, Issuer, IssuedAt, ExpiresAt? }`, `CareerProject { Name, Role, Description, StartedAt, EndedAt? }`. DbContext + migration. | backend | — | M |
| G7.2 | `CareerService` CRUD (cô lập UserId) cho 3 nhóm. | backend | G7.1 | M |
| G7.3 | `CareerController` (`api/career`) + DTO + validation. | backend | G7.2 | M |
| G7.4 | FE models + service. | frontend | G7.3 | S |
| G7.5 | Trang **Sự nghiệp** 3 khối (skills có thanh mức, certs, projects). | frontend | G7.4 | L |
| G7.6 | Nav + route + (tùy chọn) số liệu vào "Hôm nay"/Stats. | frontend | G7.5 | S |
| G7.7 | Integration test 3 nhóm + cô lập user. | test | G7.3 | M |
| G7.8 | `docs/features/g7-career.md`. | docs | mọi | S |

**Liên kết tương lai:** Skill ↔ Goal/Project; Certification ↔ Document (G8).

## G8 · Document Module (upload + phân loại + storage)  ✅ ĐÃ XONG  *(4 test)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G8.1 | Trừu tượng `IFileStorage` (Save/Get/Delete) + `LocalFileStorage` (tái dùng pattern avatar `wwwroot/uploads`). Đăng ký DI. | backend | — | M |
| G8.2 | Entity `Document { Category(cv/cert/contract/invoice/personal), FileName, ContentType, Size, StorageKey, UploadedAt }`. Migration. | backend | — | S |
| G8.3 | `DocumentService`: upload (validate **type + size**, cô lập UserId), list theo category, download (stream), delete (xóa cả file). | backend | G8.1,G8.2 | M |
| G8.4 | `DocumentsController` (`api/documents`) — multipart upload + download có kiểm quyền sở hữu. | backend | G8.3 | M |
| G8.5 | FE: trang **Tài liệu** — upload (drag/drop), lọc theo loại, tải/xóa. | frontend | G8.4 | M |
| G8.6 | Nav + route. | frontend | G8.5 | S |
| G8.7 | Test upload/list/download/delete + chặn truy cập file người khác + giới hạn type/size. | test | G8.4 | M |
| G8.8 | `docs/features/g8-document.md`. | docs | mọi | S |

**Chốt trước khi code:** giới hạn size/type cho phép; local trước, để `IFileStorage` mở đường MinIO/S3 sau.

## G5 · Health — chỉ số & hoạt động (BMI / Activity / Calories)  ✅ ĐÃ XONG  *(6 test)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G5.1 | Thêm `HeightCm?` (hồ sơ user **hoặc** HealthLog) để tính BMI; entity `Activity { Type(running/walking/gym/swimming/cycling), DurationMin, Calories? }` gắn ngày. Migration. | backend | — | M |
| G5.2 | `HealthService`: BMI = cân nặng/(cao²); CRUD activity; báo cáo Weight/Activity/Calories trend. | backend | G5.1 | M |
| G5.3 | Controller/DTO: trả BMI tính sẵn; endpoint activity + trend. | backend | G5.2 | S |
| G5.4 | FE: hiển thị BMI + phân loại; nhập hoạt động có loại; biểu đồ xu hướng. | frontend | G5.3 | M |
| G5.5 | Test BMI + activity + trend. | test | G5.3 | S |
| G5.6 | Cập nhật `docs/features/f2-health.md`. | docs | mọi | S |

## G6 · Knowledge — Tags / Categories / Backlinks  ✅ ĐÃ XONG  *(6 test)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G6.1 | `Note` + `Title?`, `Category?`, `Tags` (mảng/jsonb hoặc bảng `NoteTag`). Bảng `NoteLink { FromNoteId, ToNoteId }`. Migration. | backend | — | M |
| G6.2 | `NoteService`: lưu note → **parse `[[tiêu đề]]`** dựng backlinks; truy vấn "linked references"; lọc theo tag/category. | backend | G6.1 | L |
| G6.3 | Controller/DTO: tag/category/backlinks; tích hợp `SearchService` (B2) cho note. | backend | G6.2 | M |
| G6.4 | FE: editor có tag/category; hiển thị backlinks; (tùy chọn) graph view — **hoãn nếu nặng**. | frontend | G6.3 | L |
| G6.5 | Test backlink parsing + lọc tag + search. | test | G6.3 | M |
| G6.6 | `docs/features/g6-knowledge.md`. | docs | mọi | S |

---

# ĐỢT 3C — AI Assistant + RAG (tầm nhìn cốt lõi) · P3

Phụ thuộc dữ liệu phong phú từ 3A/3B. Làm **tăng dần**: hạ tầng → 1 nguồn → mở rộng. Đọc lại skill `claude-api` khi code; key chỉ ở backend, opt-in.

## G9 · RAG Infrastructure + Smart Search + Goal Analysis + Planning

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G9.1 | **PgVector**: bật extension trên Postgres; bảng `Embedding { OwnerType, OwnerId, Chunk, Vector, UpdatedAt }` + index vector. Cấu hình EF (Npgsql pgvector). Migration. | backend | — | M |
| G9.2 | **Embedding pipeline** (background service incremental): chunk + embed **Notes trước** (nguồn nhỏ, an toàn); hàng đợi cập nhật khi dữ liệu đổi. Chốt provider embedding. | backend | G9.1 | L |
| G9.3 | **Smart Search**: retrieval top-k theo vector → trả đoạn liên quan; API `POST /api/ai/search`. | backend | G9.2 | M |
| G9.4 | Mở rộng embedding sang Goals/Tasks/Journal/Finance/Health (theo opt-in từng nguồn). | backend | G9.2 | L |
| G9.5 | **Goal Analysis**: dùng tiến độ goal (G1) + lịch sử → Claude API tạo nhận định "chậm/đúng tiến độ". | backend | G9.4, G1 | M |
| G9.6 | **Planning**: prompt Claude tạo kế hoạch N ngày từ một mục tiêu → sinh task (gắn vào G1/G3). | backend | G9.4 | L |
| G9.7 | FE: ô hỏi-đáp Smart Search; thẻ Goal Analysis trong dashboard; nút "Lập kế hoạch" ở Goal. | frontend | G9.3,G9.5,G9.6 | L |
| G9.8 | **Riêng tư**: công tắc opt-in từng nguồn; giới hạn dữ liệu gửi đi; che dữ liệu nhạy cảm; không lộ key. | backend/FE | G9.2 | M |
| G9.9 | Test retrieval + opt-in + chi phí (mock Claude API trong test). | test | G9.3+ | M |
| G9.10 | `docs/features/g9-ai-rag.md`. | docs | mọi | S |

**Chốt trước khi code:** provider embedding (dịch vụ nào / chiều vector); ngân sách token; chính sách dữ liệu gửi đi.

## G10 · Notification — kênh mở rộng · P3

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G10.1 | **Web Push** (tái dùng service worker C4): VAPID keys, lưu subscription, gửi push khi có thông báo. | backend/FE | — | M |
| G10.2 | **Telegram bot** (rẻ, không phí): liên kết chat id, gửi nhắc qua bot. | backend | — | M |
| G10.3 | SMS — **hoãn** (cần nhà cung cấp trả phí); chỉ làm khi có nhu cầu thật. | — | — | — |

## G11 · Task — Kanban / nhiều trạng thái · P3  ✅ ĐÃ XONG  *(5 test; Sprint vẫn hoãn)*

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| G11.1 | `TaskItem` + `Status` (todo/in_progress/review/done/cancelled) thay cho chỉ `IsDone`; migrate dữ liệu cũ. | backend | — | M |
| G11.2 | Controller/Service hỗ trợ lọc/đổi status. | backend | G11.1 | S |
| G11.3 | FE **Kanban** kéo-thả theo cột status. | frontend | G11.2 | L |
| G11.4 | Sprint — **hoãn** (nặng, ít giá trị cho cá nhân). | — | — | — |

---

# Tổng hợp & thứ tự đề xuất

```
3A  G2 Mood ─► G1 Goal đa cấp ─► G4 Assets ─► G3 Habit heatmap     ✅ ĐÃ XONG (P1)
        │
3B  G7 Career │ G8 Document │ G5 Health │ G6 Knowledge             ✅ ĐÃ XONG (P2)
        │
3C  G9 RAG (9.1→9.2→9.3→9.4→9.5/9.6) ─► G10 push/telegram          (P3, cần dữ liệu từ 3A/3B)
        └ G11 Kanban (tùy nhu cầu, bất kỳ lúc nào)
```

**Ước lượng thô:** 3A ≈ 1–2 tuần · 3B ≈ 2–3 tuần · 3C ≈ 3–5 tuần.

**Khuyến nghị bắt đầu:** **G2 (Mood)** — nhỏ nhất, chạm trọn pipeline để làm nóng, rồi sang **G1 (Goal đa cấp)** là hạng mục giá trị cao nhất của đợt 3A.

**Cập nhật bảng theo dõi:** khi khởi động mỗi gap, thêm dòng vào `docs/03-feature-plan.md` (Phần B) và đổi trạng thái trong `docs/00-process.md`.
