# MeUp — Kế hoạch chức năng & Lộ trình (Feature Plan + Roadmap)

Mục tiêu sản phẩm: app đa người dùng, quản lý **mọi thứ của một người theo từng ngày**.
Mỗi chức năng chạy qua pipeline 7 trạng thái trong `docs/00-process.md` (docs-first).

Ký hiệu ưu tiên: **P0** = nền bắt buộc · **P1** = MVP / giá trị cao · **P2** = sau MVP.

> Tài liệu này gộp kế hoạch tổng thể (cũ: 03) và lộ trình nâng cấp Phase 2 (cũ: 04) — **nguồn duy nhất**.

---

# PHẦN A — Chức năng nền tảng (F0–F8)

## F0 — Nền tảng & Auth · P0
Khung ứng dụng (ASP.NET Core Web API + Angular + PostgreSQL/EF Core + Docker); đăng ký/đăng nhập/đăng xuất;
JWT access + refresh; hồ sơ + đổi mật khẩu; vai trò user/admin; cô lập dữ liệu theo `UserId`. Phụ thuộc: không.

## F0E — Hồ sơ mở rộng + OAuth2 + 2FA · P0
Hồ sơ đầy đủ (avatar, SĐT, ngày sinh, giới tính, tiểu sử, múi giờ, ngôn ngữ); đổi email; xóa tài khoản;
đăng nhập Google (OAuth2); xác thực 2 lớp (TOTP) + mã khôi phục. Phụ thuộc: F0.

## F1 — Tài chính · P1
Giao dịch thu/chi (số tiền, danh mục, ngày, ghi chú); số dư + tổng ngày/tháng; danh mục; lọc/tìm/sửa/xóa;
**ngân sách theo danh mục (A1, P1 — Phase 2)**. Phụ thuộc: F0.

## F2 — Sức khỏe · P1
Nhật ký ngày (cân nặng, giờ ngủ, nước, tập, ghi chú); so sánh hôm trước; lịch sử;
**thuốc/nhắc uống (A2, Phase 2)**; biểu đồ xu hướng → F7. Phụ thuộc: F0.

## F3 — Công việc & Mục tiêu & Thói quen · P1
Task (hạn, quá hạn, hoàn thành); mục tiêu (tiến độ %); thói quen (check ngày + streak);
**task lặp lại (A3, Phase 2)**. Phụ thuộc: F0.

## F4 — Lịch trình & Sự kiện · P2
Sự kiện theo ngày/giờ, xem theo ngày; nhắc nhở (cần hạ tầng C1). Phụ thuộc: F0.

## F5 — Ghi chú & Nhật ký · P2
Nhật ký theo ngày (rich editor) ✅; **ghi chú nhanh (A5, Phase 2)**; tìm kiếm. Phụ thuộc: F0.

## F6 — Tổng quan "Hôm nay" · P1
Màn hình trung tâm gom mọi thứ của một ngày; chọn/đi ngày; trạng thái rỗng gợi ý. Phụ thuộc: F1, F2, F3.

## F7 — Thống kê & Báo cáo · P2
Tổng hợp tuần/tháng + biểu đồ cho F1/F2/F3. Phụ thuộc: F1, F2, F3.

## F8 — Cài đặt · P2
Ngôn ngữ (i18n), giao diện (dark mode), xuất/nhập dữ liệu. Phụ thuộc: F0.

---

# PHẦN B — Bảng theo dõi

| Feature | Ưu tiên | Trạng thái |
|---------|---------|------------|
| F0 Nền tảng & Auth | P0 | ✅ HOÀN TẤT (backend + frontend + test) |
| F0E Hồ sơ mở rộng + OAuth2 + 2FA | P0 | ✅ HOÀN TẤT (`docs/features/f0e-profile-auth.md`) |
| F1 Tài chính | P1 | ✅ HOÀN TẤT (`docs/features/f1-finance.md`) |
| F2 Sức khỏe | P1 | ✅ HOÀN TẤT (`docs/features/f2-health.md`) |
| F3 Công việc/Mục tiêu/Thói quen | P1 | ✅ HOÀN TẤT (`docs/features/f3-work.md`) |
| F5 Nhật ký | P2 | ✅ HOÀN TẤT — phần nhật ký (`docs/features/f5-journal.md`) |
| F6 Tổng quan / Hôm nay | P1 | ✅ HOÀN TẤT (`docs/features/f6-today.md`) — **MVP khép** |
| A1 Ngân sách (F1) | P1 | ✅ HOÀN TẤT (backend + frontend + test) |
| F4 Lịch trình & Sự kiện | P2 | ✅ HOÀN TẤT (backend + frontend + test) |
| F7 Thống kê & Báo cáo | P1 | ✅ HOÀN TẤT (backend + frontend + test; biểu đồ CSS) |
| A2 Thuốc/uống thuốc (F2) | P2 | ✅ HOÀN TẤT (theo dõi uống theo ngày; nhắc tự động cần C1) |
| A3 Task lặp lại (F3) | P2 | ✅ HOÀN TẤT (hoàn thành → tự sinh lần kế) |
| A5 Ghi chú nhanh (F5) | P2 | ✅ HOÀN TẤT (trong trang Nhật ký) |
| B2 Tìm kiếm toàn cục | P2 | ✅ HOÀN TẤT (giao dịch/nhật ký/task/sự kiện) |
| C1 Nhắc nhở + Thông báo | P1 | ✅ HOÀN TẤT (in-app + digest; email qua SMTP hoặc dev-log) |
| C2 Reset mật khẩu / verify email / khóa đăng nhập | P1 | ✅ HOÀN TẤT (xem `docs/features/c1-c2-notify-account.md`) |
| F8/C3 Cài đặt (dark mode + xuất dữ liệu) | P2 | ✅ HOÀN TẤT (i18n vi/en còn để sau) |
| C4 PWA / Offline | P2 | ✅ HOÀN TẤT (manifest + icon + service worker) |
| D AI Insights (Claude API) | P1 | ⏳ Phase 2 (cần ANTHROPIC_API_KEY) |
| **G1 Mục tiêu đa cấp + trạng thái + dashboard** (Phase 3) | P1 | ✅ HOÀN TẤT (`docs/features/g1-goal-tree.md`) — backend + FE + 11 test |
| **G2 Mood tracking (Nhật ký)** (Phase 3) | P1 | ✅ HOÀN TẤT (`docs/features/f5-journal.md`) — 5 test |
| **G4 Tài sản & Net Worth (Tài chính)** (Phase 3) | P1 | ✅ HOÀN TẤT (`docs/features/f1-finance.md`) — 5 test |
| **G3 Habit nâng cấp + heatmap (Công việc)** (Phase 3) | P2 | ✅ HOÀN TẤT (`docs/features/f3-work.md`) — 4 test |
| **→ Đợt 3A (G1+G2+G3+G4) khép — 120 test backend pass** | — | ✅ |
| **G5 Sức khỏe: BMI + hoạt động + xu hướng** (Phase 3) | P2 | ✅ HOÀN TẤT (`docs/features/f2-health.md`) — 6 test |
| **G6 Kiến thức: tags + backlinks (Notes)** (Phase 3) | P2 | ✅ HOÀN TẤT (`docs/features/g6-g8-knowledge-career-document.md`) — 6 test |
| **G7 Sự nghiệp: Skills/Certs/Projects** (Phase 3) | P2 | ✅ HOÀN TẤT (cùng doc) — 5 test |
| **G8 Tài liệu: upload + storage** (Phase 3) | P2 | ✅ HOÀN TẤT (cùng doc) — 4 test |
| **→ Đợt 3B (G5+G6+G7+G8) khép — 141 test backend pass** | — | ✅ |
| **G11 Task: Kanban + trạng thái (todo→done)** (Phase 3) | P3 | ✅ HOÀN TẤT (`docs/features/f3-work.md`) — 5 test |
| **G9 AI Assistant + RAG** (Phase 3) | P3 | ⏳ CHỜ KHÓA/DỊCH VỤ (ANTHROPIC_API_KEY + embedding + PgVector) |
| **G10 Web Push / Telegram** (Phase 3) | P3 | ⏳ CHỜ KHÓA (VAPID / Telegram bot token) |
| **→ Tổng: 146 test backend pass** | — | ✅ |

---

# PHẦN C — Phase 1 (đã xong)

**Mốc MVP = F0 + F0E + F1 + F2 + F3 + F6** ✅ — kèm F5 (Nhật ký). 61/61 test backend pass; FE build sạch.

```
F0 ─► F1, F2, F3 ─► F6 (Hôm nay)   ✅ MVP
                 └► F5 Nhật ký      ✅
```

---

# PHẦN D — Phase 2 (nâng cấp)

Phase 2 = hoàn thiện các chức năng cốt lõi + biến MeUp thành "trợ lý cá nhân".

## Track A — Hoàn thiện cốt lõi (P1)
| # | Chức năng | Thuộc | Mô tả |
|---|-----------|-------|-------|
| A1 | **Ngân sách** | F1 | Hạn mức chi theo danh mục/tháng; cảnh báo gần/vượt; thanh tiến độ. |
| A2 | **Thuốc / nhắc uống** | F2 | Danh sách thuốc + đánh dấu uống theo ngày. (Nhắc → C1.) |
| A3 | **Task lặp lại** | F3 | Lặp ngày/tuần/tháng; tự sinh theo lịch. |
| A4 | **F4 — Lịch trình & Sự kiện** | mới | Sự kiện theo ngày/giờ; gắn vào "Hôm nay". |
| A5 | **Ghi chú nhanh** | F5 | Ghi chú tự do (dùng chung hạ tầng nhật ký). |

## Track B — Phân tích (P1/P2)
| # | Chức năng | Mô tả |
|---|-----------|-------|
| B1 | **F7 — Thống kê & Báo cáo** · P1 | Tổng hợp tuần/tháng F1/F2/F3 + **biểu đồ** (CSS nhẹ, không thêm thư viện ở MVP). |
| B2 | **Tìm kiếm toàn cục** · P2 | Tìm xuyên giao dịch/nhật ký/task theo từ khóa. |

## Track C — Hạ tầng (cần dịch vụ ngoài)
| # | Hạng mục | Ghi chú |
|---|----------|---------|
| C1 | **Nhắc nhở & Thông báo** · P1 | Lập lịch nền (Hosted Service/Quartz) + in-app + **email (SMTP)**. Nền cho A2/A4. |
| C2 | **Vòng đời tài khoản** · P1 | Quên/đặt lại mật khẩu, xác thực email, rate-limit đăng nhập. Cần email (C1). |
| C3 | **Cài đặt (F8)** · P2 | i18n (vi/en), dark mode/theme, xuất/nhập (CSV/JSON). |
| C4 | **PWA / Mobile** · P2 | Service worker, cài như app, offline cơ bản, push (kết hợp C1). |

## Track D — AI Insights (Claude API) · P1
Gọi **Claude API** từ backend (`IInsightService`); không lộ key ra FE; opt-in riêng tư.
| # | Tính năng | Model (xem skill `claude-api`) |
|---|-----------|-------------------------------|
| D1 | Tổng kết tuần (tài chính+sức khỏe+công việc) | **Opus 4.8** `claude-opus-4-8` |
| D2 | Phân tích xu hướng | Opus 4.8 / **Sonnet 4.6** `claude-sonnet-4-6` |
| D3 | Tự phân loại giao dịch | **Haiku 4.5** `claude-haiku-4-5` + Batches |

Giá (per 1M token): Opus 4.8 $5/$25 · Sonnet 4.6 $3/$15 · Haiku 4.5 $1/$5.
Kỹ thuật: prompt caching cho phần hệ thống; structured outputs cho D3; đọc lại skill `claude-api` khi triển khai.

## Thứ tự đề xuất Phase 2
```
A1 Ngân sách  ─┐
F4 Lịch trình  ├─► ✅ ĐÃ XONG (tự chứa)
F7 Thống kê    ┘
   │
C1 Nhắc + Email ─► C2 (reset/verify/rate-limit)   (cần SMTP) ← tiếp theo
A2 Thuốc, A4 nhắc sự kiện  ◄─ dùng C1
D AI Insights  (cần ANTHROPIC_API_KEY)
A3, A5, B2, C3, C4  (đánh bóng)
```

## Điểm cần chốt khi triển khai
- **Email/SMTP** (cho C1/C2): dev có thể dùng Mailhog/Papercut.
- **Lập lịch nền**: Hosted Service tự viết vs Quartz/Hangfire.
- **AI**: cần `ANTHROPIC_API_KEY` (env/user-secrets) + công tắc opt-in; giới hạn dữ liệu gửi đi.
- **Biểu đồ**: MVP dùng CSS thuần; nâng cấp ng2-charts/ECharts nếu cần biểu đồ phức tạp.
