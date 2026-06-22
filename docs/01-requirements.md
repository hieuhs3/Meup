# MeUp — Tài liệu yêu cầu (Requirements)

Phiên bản: MVP 1.0 · Ngôn ngữ giao diện: Tiếng Việt

## Yêu cầu chung (toàn app)

- **YC-G1** Toàn bộ giao diện tiếng Việt.
- **YC-G2** Dữ liệu lưu `localStorage`, tồn tại sau khi tải lại trang.
- **YC-G3** Mọi dữ liệu nhập đều được validate (số tiền > 0, ngày hợp lệ, trường bắt buộc không rỗng).
- **YC-G4** Xuất toàn bộ dữ liệu ra file JSON để sao lưu; nhập lại từ file JSON.
- **YC-G5** Chống XSS: dữ liệu người dùng nhập được hiển thị an toàn (không render HTML thô).
- **YC-G6** Điều hướng giữa 4 khu vực: Tổng quan, Tài chính, Sức khỏe, Công việc & mục tiêu.

---

## 1. Tài chính

**Mục tiêu:** Theo dõi thu/chi và biết tình hình tiền bạc theo tháng.

**User stories**
- Là người dùng, tôi muốn ghi một giao dịch thu/chi để theo dõi dòng tiền.
- Là người dùng, tôi muốn xem số dư hiện tại để biết còn bao nhiêu.
- Là người dùng, tôi muốn xem tổng thu và tổng chi theo tháng để kiểm soát chi tiêu.
- Là người dùng, tôi muốn xóa giao dịch ghi nhầm.

**Tiêu chí chấp nhận**
- **TC-F1** Thêm giao dịch gồm: loại (thu/chi), số tiền, danh mục, ngày, ghi chú (tùy chọn).
- **TC-F2** Số dư = tổng thu − tổng chi, cập nhật ngay sau mỗi thay đổi.
- **TC-F3** Có bộ lọc theo tháng; tổng thu/chi tính đúng theo tháng đang chọn.
- **TC-F4** Danh sách giao dịch sắp xếp mới nhất lên đầu.
- **TC-F5** Xóa giao dịch cập nhật lại số dư và tổng.

**Trường hợp biên**
- Số tiền ≤ 0 hoặc không phải số → báo lỗi, không lưu.
- Chưa có giao dịch → hiển thị số dư 0 và trạng thái rỗng.
- Danh mục để trống → dùng "Khác".

---

## 2. Sức khỏe

**Mục tiêu:** Ghi lại các chỉ số sức khỏe hằng ngày và thấy xu hướng.

**User stories**
- Là người dùng, tôi muốn ghi cân nặng, giờ ngủ, lượng nước, buổi tập theo ngày.
- Là người dùng, tôi muốn xem lịch sử gần đây để thấy mình đang tiến bộ hay không.

**Tiêu chí chấp nhận**
- **TC-H1** Ghi nhật ký một ngày gồm: ngày, cân nặng (kg), giờ ngủ (giờ), nước (ly/lít), ghi chú buổi tập.
- **TC-H2** Mỗi ngày một bản ghi; ghi lại cùng ngày sẽ cập nhật bản ghi đó.
- **TC-H3** Hiển thị danh sách các bản ghi gần đây (mới nhất trước).
- **TC-H4** Hiển thị chỉ số mới nhất và so sánh với bản ghi trước (tăng/giảm).

**Trường hợp biên**
- Bỏ trống một chỉ số → cho phép (chỉ số đó coi như chưa ghi).
- Giá trị âm → báo lỗi.
- Chưa có dữ liệu → trạng thái rỗng.

---

## 3. Công việc & mục tiêu

**Mục tiêu:** Quản lý việc cần làm, mục tiêu dài hạn và thói quen.

**User stories**
- Là người dùng, tôi muốn thêm task và đánh dấu hoàn thành.
- Là người dùng, tôi muốn đặt mục tiêu có tiến độ phần trăm.
- Là người dùng, tôi muốn theo dõi thói quen hằng ngày (đã làm hôm nay chưa).

**Tiêu chí chấp nhận**
- **TC-W1** Thêm/xóa task; đánh dấu hoàn thành chuyển trạng thái và hiển thị khác biệt.
- **TC-W2** Task có thể có hạn (ngày); task quá hạn được đánh dấu nổi bật.
- **TC-W3** Mục tiêu có tên + tiến độ 0–100%; chỉnh tiến độ cập nhật ngay.
- **TC-W4** Thói quen: danh sách habit, mỗi habit check/bỏ check cho "hôm nay"; đếm chuỗi ngày (streak) liên tiếp.

**Trường hợp biên**
- Tiến độ ngoài 0–100 → kẹp về biên gần nhất.
- Task không có tên → báo lỗi.
- Đổi ngày (qua ngày mới) → trạng thái check "hôm nay" reset.

---

## 4. Tổng quan (Dashboard)

**Mục tiêu:** Nhìn nhanh tình hình tổng thể.

**Tiêu chí chấp nhận**
- **TC-D1** Thẻ Tài chính: số dư hiện tại + thu/chi tháng này.
- **TC-D2** Thẻ Sức khỏe: chỉ số mới nhất (cân nặng, giờ ngủ).
- **TC-D3** Thẻ Công việc: số task chưa xong, số task quá hạn, số habit đã làm hôm nay.
- **TC-D4** Khi chưa có dữ liệu, mỗi thẻ hiển thị trạng thái rỗng gợi ý hành động.

---

## Ngoài phạm vi MVP (làm sau)

- Đồng bộ đám mây / nhiều thiết bị.
- Biểu đồ nâng cao, báo cáo PDF.
- Nhắc nhở/thông báo.
- Đăng nhập, nhiều người dùng.
