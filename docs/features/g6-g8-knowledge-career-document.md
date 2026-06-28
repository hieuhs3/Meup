# G6 / G7 / G8 — Kiến thức · Sự nghiệp · Tài liệu (Phase 3 — Đợt 3B)

Trạng thái: Code ✅ · Test ✅ — **HOÀN TẤT**. Nguồn: `docs/07-gap-analysis.md`, `docs/08-phase3-plan.md`.
Phụ thuộc: **F0** (auth + cô lập theo UserId).

---

## G6 — Knowledge (Notes nâng cấp kiểu Obsidian)

- **Data:** `Note` += `Title?`, `Category?`, `Tags` (Postgres `text[]`, default `'{}'`). Ghi chú nhanh cũ (chỉ Content) vẫn chạy.
- **Backlinks:** parse cú pháp `[[tiêu đề]]` trong nội dung → `outLinks`; `backlinks` = các note khác trỏ tới tiêu đề note này (tính trong bộ nhớ trên toàn bộ note của user).
- **API:** `GET /api/notes?tag=&category=&q=` (lọc thẻ/nhóm/từ khóa); DTO trả `title/category/tags/outLinks/backlinks`.
- **FE:** trang **Kiến thức** (`/app/knowledge`) — form tiêu đề/nội dung/nhóm/thẻ, danh sách có thẻ bấm-lọc, hiển thị "được nhắc bởi" (backlinks). Ghi chú nhanh vẫn ở trang Nhật ký.
- **Test:** 6 (`G6KnowledgeTests`): tạo có tiêu đề/nhóm/thẻ, backlinks+outLinks, lọc thẻ, tìm từ khóa, ghi chú nhanh chỉ nội dung, cô lập/401.

## G7 — Career (Skills / Certifications / Projects)

- **Data:** `Skill` (name, category?, level 1–5), `Certification` (name, issuer?, issuedAt?, expiresAt?), `CareerProject` (name, role?, description?, startedAt?, endedAt?).
- **API:** `api/career/skills|certifications|projects` — CRUD đầy đủ, cô lập UserId.
- **FE:** trang **Sự nghiệp** (`/app/career`) — 3 khối; skill có thanh sao mức độ.
- **Test:** 5 (`G7CareerTests`): skill CRUD + clamp level, level ngoài khoảng → 400, cert/project CRUD, cô lập/401.

## G8 — Document (upload + phân loại + storage)

- **Storage:** trừu tượng `IFileStorage` + `LocalFileStorage` (lưu `ContentRoot/storage/documents`, **ngoài wwwroot** → không phục vụ tĩnh; chống path traversal). Đổi sang MinIO/S3 sau chỉ cần thay implementation.
- **Data:** `Document` (category, fileName, contentType, size, storageKey, uploadedAt). Phân loại: cv/certificate/contract/invoice/personal/other.
- **Quy tắc:** giới hạn **10MB**, whitelist phần mở rộng (pdf/doc/xls/ppt/ảnh/txt/csv/zip). Tải lên multipart; tải về qua controller **kiểm quyền sở hữu** (stream file).
- **API:** `GET /api/documents?category=`, `POST /api/documents` (multipart), `GET /api/documents/{id}/download`, `DELETE`.
- **FE:** trang **Tài liệu** (`/app/documents`) — chọn loại + upload, lọc theo loại, tải/xóa.
- **Test:** 4 (`G8DocumentTests`): upload/list/download/delete, định dạng không hỗ trợ → 400, tải/xóa của người khác → 404, 401.

---

**Tổng Đợt 3B:** G5 (6) + G6 (6) + G7 (5) + G8 (4) = 21 test mới. Toàn bộ backend **141/141 pass**, FE build sạch.
