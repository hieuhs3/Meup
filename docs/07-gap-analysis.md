# MeUp — Phân tích khoảng cách & Lộ trình Phase 3 (Gap Analysis)

So sánh **tầm nhìn PersonalOS** (`docs/reference.md`) với **thực trạng MeUp** (đã xong Phase 1 + Phase 2).
Mục tiêu: xác định rõ những gì còn thiếu, đặc tả từng khoảng cách, và đề xuất lộ trình **Phase 3**.

> Tài liệu phân tích — **chưa phải cam kết triển khai**. Mỗi hạng mục khi làm sẽ chạy qua pipeline 7 trạng thái trong `docs/00-process.md` (docs-first).
> Ký hiệu ưu tiên: **P0** nền bắt buộc · **P1** giá trị cao · **P2** sau MVP · **P3** tầm nhìn dài hạn.

---

## 1. Bảng đối chiếu tổng quan (12 module)

| # | Module (PersonalOS) | MeUp hiện tại | Mức độ | Khoảng cách chính |
|---|---------------------|---------------|--------|-------------------|
| 3.1 | Identity | Auth, JWT, refresh, 2FA (TOTP), Google OAuth, hồ sơ đầy đủ | ✅ Đủ | Gần như không thiếu |
| 3.2 | Goal Management | `Goal` phẳng: `Name` + `Progress` (0–100) | ⚠️ Nông | Cấp đa tầng, trạng thái vòng đời, Goal Dashboard |
| 3.3 | Task Management | Task + sub-task tree, ưu tiên, lặp lại, hạn | ⚠️ Khá đủ | Kanban, Sprint, Checklist (status nhiều bước) |
| 3.4 | Habit Tracking | `Habit` + `HabitCheck` theo ngày | ⚠️ Nông | Frequency/Target, Best streak, Completion %, Heatmap |
| 3.5 | Knowledge Mgmt | `Note` text tự do | ⚠️ Nông | Tags, Categories, Backlinks, search ngữ nghĩa |
| 3.6 | Journal | Nhật ký rich-text theo ngày | ⚠️ Khá đủ | Mood tracking + Mood trend |
| 3.7 | Finance | Thu/chi, danh mục, ngân sách | ⚠️ Khá đủ | Assets, Net Worth, Saving Rate, Cash Flow |
| 3.8 | Health | Cân nặng, ngủ, nước, tập, thuốc | ⚠️ Khá đủ | BMI, Body Fat, Calories, loại hoạt động, trend |
| 3.9 | Career | — | ❌ Trống | Skills, Certifications, Projects, Career Goals |
| 3.10 | Document | — | ❌ Trống | Lưu CV/Cert/Contract + storage (MinIO/S3) |
| 3.11 | Notification | In-app + email/digest | ⚠️ Khá đủ | Telegram, SMS, Web Push |
| 3.12 | AI Assistant | Tổng kết tuần + báo cáo ngày (stub) | ⏳ Mầm | Smart Search ngữ nghĩa, RAG, Goal Analysis, Planning |
| 7 | AI Architecture | — | ❌ Trống | Embedding pipeline + Vector DB (Qdrant/PgVector) + Retrieval |

**Tóm tắt:** MeUp đã hoàn tất xuất sắc phần "nền tảng quản lý theo ngày". So với PersonalOS:
- **3 mảng trống hoàn toàn:** Career (3.9), Document (3.10), AI/RAG Architecture (3.12 + 7).
- **Phần lớn còn lại:** đã có nhưng **nông** — cần đào sâu chứ không xây mới từ đầu.

---

## 2. Đặc tả từng khoảng cách

### G1 · Goal Management đa cấp — P1
**Hiện tại:** `Goal { Id, UserId, Name, Progress }` (`backend/MeUp.Api/Entities/WorkEntities.cs`). Phẳng, không trạng thái.
**Reference yêu cầu:** Life → Year → Quarter → Month → Weekly; trạng thái Draft/Active/Completed/Cancelled/Archived; Goal Dashboard theo dõi tiến độ.
**Gap:**
- Thêm `ParentGoalId` (tự tham chiếu) + `Level` (life/year/quarter/month/week).
- Thêm `Status` (enum 5 trạng thái) + `TargetDate`, `Description`.
- Tiến độ cha tự tổng hợp từ con (rollup) thay vì nhập tay.
- Trang Goal Dashboard: cây mục tiêu + % theo cấp.
**Chi phí:** Vừa (1 migration, mở rộng `WorkService`/`WorkController`, 1 trang FE). Tái dùng pattern cây Task vừa làm (commit `ddc436c`).

### G2 · Mood tracking cho Journal — P1
**Hiện tại:** `JournalEntry` không có mood (`Entities/JournalEntry.cs`).
**Reference:** Mood {Excellent/Good/Normal/Bad/Terrible} + Mood Trend (weekly/monthly).
**Gap:** thêm cột `Mood` (nullable), selector ở FE, biểu đồ xu hướng (tận dụng `StatsService` + biểu đồ CSS sẵn có).
**Chi phí:** Nhỏ.

### G3 · Habit nâng cấp — P2
**Hiện tại:** `Habit { Name }` + check ngày. Streak tính ở service (cần xác minh).
**Reference:** Frequency, Target, Current Streak, Best Streak, Completion Rate, Heatmap.
**Gap:** thêm `Frequency`/`TargetPerWeek` vào entity; tính `BestStreak` + `CompletionRate`; component heatmap (lưới ngày kiểu GitHub) ở FE.
**Chi phí:** Vừa (heatmap là phần tốn công nhất).

### G4 · Finance — Assets & Net Worth — P1
**Hiện tại:** `Transaction` + `Budget` + `Category`. Không có khái niệm tài sản.
**Reference:** Asset {Cash/Bank/Stock/Crypto/Gold}; báo cáo Net Worth, Saving Rate, Cash Flow.
**Gap:**
- Entity `Asset { Type, Name, Value, Currency, UpdatedAt }` + snapshot theo thời gian để vẽ Net Worth trend.
- Báo cáo: Saving Rate = (thu − chi)/thu; Cash Flow theo tháng; Net Worth = tổng asset.
**Chi phí:** Vừa.

### G5 · Health — chỉ số & hoạt động — P2
**Hiện tại:** `HealthLog` (cân nặng, ngủ, nước, tập-text) + `Medication`.
**Reference:** BMI, Body Fat, Calories; Activities có loại (Running/Walking/Gym/Swimming/Cycling); Weight/Activity/Calories trend.
**Gap:**
- BMI tự tính từ cân nặng + chiều cao (chiều cao lấy từ hồ sơ hoặc thêm field).
- Tách `Activity { Type, Duration, Calories }` thay cho ô tập dạng text.
- Biểu đồ xu hướng (đã có hạ tầng Stats).
**Chi phí:** Vừa.

### G6 · Knowledge — Tags/Categories/Backlinks — P2
**Hiện tại:** `Note { Content }` text tự do.
**Reference:** Notes + Tags + Categories + Backlinks (kiểu Obsidian) + Search.
**Gap:**
- `Title`, `Tags` (many-to-many hoặc mảng), `Category`.
- Backlinks: parse cú pháp `[[note]]` → bảng liên kết; hiển thị "linked references".
- Tích hợp vào Tìm kiếm toàn cục (đã có B2).
**Chi phí:** Vừa–Lớn (backlink parsing + graph view nếu muốn).

### G7 · Career Module — P2 (mới hoàn toàn)
**Reference:** Skills (mức thành thạo), Certifications (AWS/Azure…), Projects, Career Goals (Junior→Architect).
**Gap:** module CRUD mới, theo đúng pattern module hiện tại (Entity → DbContext → Migration → Service → Controller → DTO → FE feature + route + service + model).
**Chi phí:** Vừa (thuần CRUD, ít rủi ro). Có thể gắn Skills ↔ Goal/Project để liên kết dữ liệu.

### G8 · Document Module — P2 (mới hoàn toàn)
**Reference:** Categories {CV/Certificate/Contract/Invoice/Personal}; Storage {Local/MinIO/S3}.
**Gap:**
- `Document { Category, FileName, ContentType, Size, StorageKey, UploadedAt }`.
- Upload/download; storage local trước (tái dùng pattern avatar upload `wwwroot/uploads`), trừu tượng `IFileStorage` để sau cắm MinIO/S3.
- Liên kết Document ↔ Certification (Career) / Transaction (hóa đơn).
**Chi phí:** Vừa (cần chú ý bảo mật file: kiểm tra type/size, cô lập theo UserId).

### G9 · AI Assistant + RAG — P3 (tầm nhìn cốt lõi)
**Hiện tại:** `AiInsightService` + `WeeklyInsight` cache + báo cáo ngày; gọi Claude API (cần `ANTHROPIC_API_KEY`), opt-in (`UserAiApiKey`).
**Reference (module quan trọng nhất):**
- **Smart Search** ngữ nghĩa: "Tôi đã học gì về Docker tháng trước?"
- **Goal Analysis:** "Bạn chậm tiến độ AWS 20%."
- **Planning:** "Lập kế hoạch học K8s trong 30 ngày."
- **AI Architecture (mục 7):** User Data → Chunking → Embedding → Vector DB → Retrieval.
**Gap:**
- **PgVector** (đã có Postgres → rẻ hơn dựng Qdrant riêng): bật extension + bảng `embeddings`.
- Pipeline embedding cho Goals/Tasks/Notes/Journal/Finance/Health (background service incremental).
- Retrieval + Claude API (RAG) cho Smart Search, Goal Analysis, Planning.
- Bảo mật/riêng tư: opt-in từng nguồn dữ liệu; giới hạn dữ liệu gửi đi; không lộ key ra FE.
**Mô hình (xem skill `claude-api`):** Smart Search/Planning → Opus 4.8 (`claude-opus-4-8`) hoặc Sonnet 4.6; embedding qua provider embedding (cần chốt).
**Chi phí:** Lớn — nên chia nhỏ: (a) PgVector + embedding 1 nguồn (Notes) → (b) Smart Search → (c) Goal Analysis → (d) Planning.

### G10 · Notification — kênh mở rộng — P3
**Hiện tại:** in-app + email (SMTP/dev-log) + digest.
**Reference:** thêm Telegram, SMS, Web Push.
**Gap:** Web Push (đã có service worker từ C4 → khả thi nhất); Telegram bot (rẻ, không tốn phí SMS); SMS (cần nhà cung cấp, để cuối).
**Chi phí:** Nhỏ–Vừa mỗi kênh; làm theo nhu cầu.

### G11 · Task — Kanban/Sprint — P3
**Hiện tại:** task tree + done/chưa-done.
**Reference:** Kanban Board, Sprint, Status nhiều bước (Todo/InProgress/Review/Done/Cancelled).
**Gap:** thêm `Status` nhiều trạng thái cho `TaskItem`; FE Kanban (kéo-thả). Sprint là tính năng nặng, cân nhắc bỏ qua nếu không dùng.
**Chi phí:** Vừa (kéo-thả FE).

---

## 3. Lộ trình Phase 3 đề xuất

Chia 3 đợt theo nguyên tắc: **đào sâu cái sẵn có trước (giá trị nhanh) → bổ sung module trống → cuối cùng là AI/RAG (đột phá nhưng đắt)**.

### Đợt 3A — Quick wins (đào sâu, rẻ) · ~1–2 tuần
```
G1 Goal đa cấp + dashboard   (P1)
G2 Mood tracking (Journal)   (P1)
G4 Finance Assets/Net Worth  (P1)
G3 Habit heatmap + %         (P2)
```
Tận dụng pattern + hạ tầng Stats/biểu đồ sẵn có. Rủi ro thấp, nâng trải nghiệm rõ rệt.

### Đợt 3B — Module mới (độc lập) · ~2–3 tuần
```
G7 Career (Skills/Certs/Projects)   (P2)
G8 Document (upload + phân loại)    (P2)
G5 Health metrics (BMI/Activity)    (P2)
G6 Knowledge tags/backlinks         (P2)
```
Mỗi module độc lập, có thể làm song song hoặc cuốn chiếu.

### Đợt 3C — AI Assistant + RAG (tầm nhìn) · ~3–5 tuần
```
G9a PgVector + embedding (Notes)
G9b Smart Search ngữ nghĩa
G9c Goal Analysis
G9d Planning
G10 Web Push / Telegram (kèm theo)
```
Đây là thứ biến MeUp từ "gom nhiều app" thành "Digital Brain" đúng tầm nhìn. Làm sau cùng vì phụ thuộc dữ liệu của các module trên (càng nhiều dữ liệu, RAG càng giá trị).

### Sơ đồ phụ thuộc
```
Phase 1+2 (✅ xong)
   │
   ├─► 3A Quick wins ─┐
   ├─► 3B Module mới ─┤
   │                  └─► 3C AI/RAG (cần dữ liệu phong phú từ 3A/3B)
   └─► G11 Kanban, G10 kênh notify  (tùy nhu cầu, bất kỳ lúc nào)
```

---

## 4. Điểm cần chốt trước khi triển khai

- **AI/RAG (G9):** chốt provider embedding + bật PgVector; xác định chính sách riêng tư (opt-in từng nguồn, giới hạn dữ liệu gửi đi). Đọc lại skill `claude-api` khi code.
- **Document (G8):** local storage trước hay MinIO ngay? Trừu tượng `IFileStorage` để không khóa cứng.
- **Goal rollup (G1):** tiến độ cha = trung bình con hay theo trọng số? Cần quyết định công thức.
- **Phạm vi:** PersonalOS rất rộng — nên chốt MeUp **không** làm hết (vd Sprint, SMS) nếu không phục vụ nhu cầu thật của người dùng cá nhân.
