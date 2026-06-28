# MeUp — Chiến lược thương mại hóa & Mở rộng (Commercial & Scaling Strategy)

Đưa MeUp từ "app cá nhân chạy local" thành **SaaS thương mại**. Tài liệu này đặt nền **multi-tenant, billing, gating gói, kiểm soát chi phí AI, bảo mật/tuân thủ và lộ trình scale hạ tầng** — để mọi feature từ nay phát triển theo hướng bán được.

> Quan hệ tài liệu: tầm nhìn `reference.md` → khoảng cách `07-gap-analysis.md` → kế hoạch tính năng `08-phase3-plan.md` → **chiến lược thương mại (tài liệu này)**. Phần kỹ thuật vẫn chạy pipeline docs-first (`00-process.md`).

---

## 0. Default đã chốt (đổi được — xem §10)

| Quyết định | Default chọn | Lý do |
|---|---|---|
| Mô hình | **Freemium B2C subscription** (Org/Team để Phase sau) | App quản lý đời sống cá nhân → tự nhiên B2C; gating theo gói dễ thu phí. |
| Thị trường & thanh toán | **VN trước** (MoMo/VNPay), trừu tượng để cắm **Paddle** (quốc tế) sau | Ra mắt nhanh nội địa; Paddle làm merchant-of-record lo thuế khi mở quốc tế. |
| Vai trò AI | **Gói cao cấp + metering token**; cho **BYO API key** (đã có mầm `UserAiApiKey`) | AI là chi phí biến đổi lớn nhất → phải đo & chặn; BYO key giảm rủi ro lỗ. |
| Ưu tiên | **Ra bản trả phí nhanh** nhưng **đặt nền multi-tenant đúng từ đầu** | Thu tiền sớm để kiểm chứng thị trường, tránh nợ kiến trúc khó sửa. |

---

## 1. Định vị & gói sản phẩm

Ba gói (đặt tên tạm — chốt khi làm landing):

| | **Free** | **Pro** (cá nhân) | **Lifetime/Team** (sau) |
|---|---|---|---|
| Module core (Today/Finance/Health/Work/Journal/Calendar) | ✅ | ✅ | ✅ |
| Giới hạn dữ liệu | Quota mềm (vd 500 giao dịch, 100 note) | Không giới hạn hợp lý | — |
| Lưu trữ tài liệu (G8) | 100 MB | 5–20 GB | — |
| Thống kê & báo cáo nâng cao (G4/G5) | Cơ bản | Đầy đủ + xuất | — |
| **AI Assistant / RAG (G9)** | Dùng thử giới hạn / **BYO key** | Quota token theo gói | — |
| Kênh nhắc nâng cao (push/Telegram G10) | — | ✅ | — |
| Hỗ trợ / SLA | Cộng đồng | Ưu tiên | — |

**Nguyên tắc gating:** chặn ở **backend** theo *entitlements* (không tin client). Mọi giới hạn = một entitlement key (`max_transactions`, `ai_tokens_month`, `storage_mb`, `feature.kanban`…), kiểm tra tại Service.

---

## 2. Multi-tenancy — đặt nền đúng từ bây giờ

MeUp **đã** cô lập dữ liệu theo `UserId` (mọi truy vấn `Where(x => x.UserId == userId)`) — đây là nền multi-tenant B2C tốt sẵn. Việc cần thêm:

| # | Hạng mục | Mô tả |
|---|---|---|
| MT1 | **Lớp Subscription/Plan** | Entity `Plan`, `Subscription { UserId, PlanId, Status, CurrentPeriodEnd, Provider, ProviderSubId }`. Nguồn sự thật cho entitlements. |
| MT2 | **Entitlement service** | `IEntitlementService.Check(userId, key)` / `Consume(userId, key, n)`. Cache (Redis) để không query mỗi request. |
| MT3 | **Gate ở Service layer** | Trước khi ghi: kiểm quota/feature; quá hạn → 402/403 + thông điệp nâng cấp (tiếng Việt). |
| MT4 | **Chuẩn bị Org/Team (chưa bật)** | Thêm `OrgId?` nullable trên các entity (hoặc lớp ánh xạ) để sau mở B2B không phải migrate đau. **Quyết định ngay** kẻo nợ kiến trúc. |
| MT5 | **Tách dữ liệu khi xóa/đổi gói** | Hạ gói Pro→Free: dữ liệu vượt quota chỉ **đọc/khóa thêm mới**, không xóa. |

> **Khuyến nghị:** giữ **shared-DB, row-level theo UserId/OrgId** (rẻ, vận hành đơn giản) thay vì DB-per-tenant cho đến khi có khách enterprise yêu cầu.

---

## 3. Billing & thanh toán

| # | Hạng mục | Mô tả |
|---|---|---|
| BL1 | **Trừu tượng `IPaymentProvider`** | `CreateCheckout`, `CancelSub`, `HandleWebhook`. Cắm được MoMo/VNPay (VN) và Paddle/Stripe (quốc tế). |
| BL2 | **Webhook → đồng bộ Subscription** | Thanh toán thành công/hủy/hết hạn → cập nhật `Subscription` + làm mới entitlements. Idempotent (dedup theo event id — pattern `DedupKey` đã có ở Notification). |
| BL3 | **Trang Pricing + Checkout (FE)** | So sánh gói, nút nâng cấp, trạng thái gói hiện tại, lịch sử hóa đơn. |
| BL4 | **Hóa đơn & thuế** | VN: xuất hóa đơn điện tử (tích hợp sau). Quốc tế: dùng Paddle (merchant-of-record) để khỏi tự lo VAT. |
| BL5 | **Grace period & dunning** | Thẻ lỗi → thử lại + nhắc (qua C1 email/notification đã có), khóa mềm sau N ngày. |

> Bắt đầu **một** provider (VN) cho nhanh; `IPaymentProvider` đảm bảo thêm provider sau không phá code.

---

## 4. Kiểm soát chi phí AI (sống còn cho biên lợi nhuận)

AI/RAG (G9) là chi phí biến đổi lớn nhất. Bắt buộc có **trước khi** bật AI trả phí:

| # | Cơ chế | Mô tả |
|---|---|---|
| AI1 | **Metering token** | Ghi token in/out mỗi lần gọi (bảng `AiUsage { UserId, Model, TokensIn, TokensOut, CostEstimate, At }`). |
| AI2 | **Quota theo gói** | `ai_tokens_month` qua entitlement; chạm trần → chặn/đề nghị nâng cấp hoặc dùng BYO key. |
| AI3 | **BYO API key** | Tận dụng `UserAiApiKey`: user tự trả phí token → bạn không gánh chi phí. Free tier khuyến khích dùng cách này. |
| AI4 | **Tối ưu chi phí** | Prompt caching (phần system), chọn model theo tác vụ (Haiku phân loại / Sonnet–Opus phân tích), cache kết quả (đã có `WeeklyInsight` cache). |
| AI5 | **Bảo vệ riêng tư** | Opt-in từng nguồn dữ liệu gửi đi; che PII; không lộ key ra FE. Đây cũng là **điểm bán** (privacy-first). |

---

## 5. Bảo mật & tuân thủ (điều kiện để bán)

| # | Hạng mục | Mô tả |
|---|---|---|
| SC1 | **Dữ liệu cá nhân (PII)** | App giữ dữ liệu đời sống nhạy cảm (tài chính/sức khỏe) → mã hóa at-rest (DB volume), TLS in-transit, hash mật khẩu BCrypt (đã có). |
| SC2 | **Tuân thủ VN — Nghị định 13/2023 (PDPD)** + chuẩn GDPR nếu bán quốc tế | Có **chính sách bảo mật**, đồng ý xử lý dữ liệu, **xuất dữ liệu** (đã có ExportService) + **xóa tài khoản** (đã có). Bổ sung consent log. |
| SC3 | **Rate limit & chống lạm dụng** | Giới hạn API (đã có khóa đăng nhập C2); mở rộng rate-limit theo IP/user cho endpoint tốn kém (AI, upload). |
| SC4 | **Secrets** | JWT key, DB pass, provider key, ANTHROPIC_API_KEY → biến môi trường/secret manager, **không commit** (README đã cảnh báo). |
| SC5 | **Audit & quyền sở hữu** | Mọi truy cập tài nguyên kiểm `UserId`; upload/download (G8) kiểm sở hữu file; log truy cập nhạy cảm. |

---

## 6. Scale hạ tầng — lộ trình theo cột mốc người dùng

Memory dự án: hạ tầng deploy đã có (CF Pages + VM + Tunnel) nhưng đang chạy local. Lộ trình nâng theo tải thực, **không over-engineer sớm**:

| Giai đoạn | Người dùng | Hạ tầng | Việc cần làm |
|---|---|---|---|
| **S0 — Ra mắt** | 0–1k | 1 VM (API+FE) + Postgres + Redis (Docker), object storage cho file | TLS thật, backup tự động Postgres, healthcheck, log tập trung cơ bản. |
| **S1 — Tăng trưởng** | 1k–10k | Tách DB managed (hoặc VM riêng) + Redis riêng + CDN tĩnh (CF) | Connection pooling, index review, read-replica nếu cần, object storage MinIO/S3 (G8). |
| **S2 — Mở rộng** | 10k+ | API scale ngang (stateless — JWT đã stateless), LB, hàng đợi nền | Tách background services (reminder, embedding) ra worker riêng; queue (Redis/RabbitMQ). |

**Việc đặt nền ngay (rẻ, tránh nợ):**
- Backup tự động + thử khôi phục định kỳ.
- **Observability:** structured logging + error tracking (Sentry/Seq) + metric cơ bản (request rate, latency, lỗi). NFR reference: API < 300ms, dashboard < 2s, uptime 99.9%.
- Health/readiness endpoint cho deploy không gián đoạn.
- Migration an toàn (đã dùng EF migrations + auto-apply lúc khởi động — review để không khóa bảng lớn).

---

## 7. Vận hành (Ops)

- **Backup:** Postgres hằng ngày + object storage; retention; test restore hàng tháng.
- **Monitoring/alert:** lỗi tăng đột biến, latency, dung lượng đĩa, hết quota AI ngân sách.
- **Quy trình phát hành:** CI build + test (đã có 83 test BE + 29 FE) → deploy; rollback nhanh.
- **Hỗ trợ:** kênh hỗ trợ + trang trạng thái; SLA theo gói.
- **Chống lạm dụng:** giới hạn đăng ký (verify email đã có), rate-limit AI/upload, phát hiện bất thường.

---

## 8. Lộ trình thương mại hóa (đan xen Phase 3)

Đặt tên **CM** (Commercial) để phân biệt với gap tính năng (G):

### CM-A — Nền thương mại (làm trước/song song đợt 3A) · P0
```
MT1 Plan/Subscription ─► MT2 Entitlement service ─► MT3 Gate ở service
BL1 IPaymentProvider ─► BL2 Webhook đồng bộ ─► BL3 Pricing/Checkout (1 provider VN)
SC1/SC4 siết secrets + mã hóa · S0 backup + observability cơ bản
MT4 thêm OrgId? nullable (đặt nền B2B, chưa bật)
```
**DoD:** một user thật mua gói Pro qua MoMo/VNPay → entitlements bật → tính năng Pro mở; hạ gói → khóa mềm đúng.

### CM-B — Thương mại hóa AI (đi cùng G9) · P1
```
AI1 metering ─► AI2 quota theo gói ─► AI3 BYO key hoàn chỉnh ─► AI4 tối ưu chi phí ─► AI5 privacy opt-in
```
**DoD:** không thể vượt ngân sách token; user Free dùng AI qua BYO key; Pro có quota rõ ràng.

### CM-C — Sẵn sàng quy mô & tuân thủ · P1/P2
```
SC2 chính sách bảo mật + consent (PDPD/GDPR) · SC3 rate-limit mở rộng
S1 hạ tầng tăng trưởng (DB/Redis tách, CDN, object storage) · BL4/BL5 hóa đơn + dunning
```

### CM-D — B2B / Teams (khi có nhu cầu) · P2/P3
```
Bật Org/Team trên OrgId đã đặt nền · mời thành viên + phân quyền · billing theo seat · BL Paddle/Stripe quốc tế
```

---

## 9. Task breakdown khởi động (CM-A — ưu tiên ngay)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| CM-A1 | Entity `Plan`, `Subscription`; DbContext + migration; seed gói Free/Pro | backend | — | M |
| CM-A2 | `IEntitlementService` + cache Redis; map gói→entitlements | backend | CM-A1 | M |
| CM-A3 | Gate quota/feature tại các Service ghi dữ liệu (Finance/Work/Note/Document) | backend | CM-A2 | M |
| CM-A4 | `IPaymentProvider` + provider VN (MoMo **hoặc** VNPay) + checkout API | backend | CM-A1 | L |
| CM-A5 | Webhook đồng bộ Subscription (idempotent) → refresh entitlements | backend | CM-A4 | M |
| CM-A6 | FE: trang **Pricing** + Checkout + trạng thái gói + lịch sử hóa đơn | frontend | CM-A5 | L |
| CM-A7 | Siết secrets (env/secret manager) + mã hóa at-rest + backup tự động + healthcheck + error tracking | hạ tầng | — | M |
| CM-A8 | Thêm `OrgId?` nullable đặt nền B2B (chưa bật UI) | backend | CM-A1 | S |
| CM-A9 | Integration test: mua/hủy/hết hạn → entitlements đúng; gate chặn quota; webhook idempotent | test | CM-A5 | L |
| CM-A10 | Docs: `docs/features/cm-a-billing.md` + cập nhật bảng theo dõi | docs | mọi | S |

**Chốt khi bắt đầu CM-A4:** MoMo hay VNPay trước? Tài khoản merchant (cần đăng ký với nhà cung cấp — bạn tự làm phần ngoài code).

---

## 10. Điểm cần bạn xác nhận (đang dùng default ở §0)

1. **Mô hình** B2C freemium — đúng hướng? (B2B/Team đã đặt nền sẵn qua `OrgId?`.)
2. **Cổng thanh toán VN nào trước**: MoMo / VNPay / ZaloPay? (cần tài khoản merchant.)
3. **Giá & ranh giới gói**: số cụ thể cho Free vs Pro (quota giao dịch/note/storage/AI token, giá tháng/năm).
4. **AI**: bật BYO key cho Free + quota cho Pro — đồng ý? Ngân sách token trần hàng tháng?
5. **Tuân thủ**: có bán quốc tế (cần GDPR + Paddle) trong 6–12 tháng tới không, hay VN-only trước?

> Khi bạn xác nhận/đổi các điểm trên, tôi sẽ vào **CM-A1** theo pipeline docs-first (lập `docs/features/cm-a-billing.md` trước khi code).
