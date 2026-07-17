# Mobile M4 — Offline & Performance

Ngày: 2026-07-15
Trạng thái: ✅ **HOÀN TẤT**
Phụ thuộc: M3 hoàn tất

---

## 1. Mục tiêu
Xây dựng giải pháp hỗ trợ chạy ngoại tuyến (Offline-first / Offline-friendly) cho ứng dụng di động:
- **GET Request Cache**: Lưu trữ dữ liệu lấy từ API vào `localStorage` khi online và tự động trả về dữ liệu cache khi offline, giúp ứng dụng không bị trắng trang.
- **POST/PUT/DELETE Sync Queue**: Thu thập các thao tác ghi dữ liệu của người dùng khi không có mạng, lưu trữ vào hàng đợi và tự động đồng bộ tuần tự (FIFO) khi có mạng trở lại.

---

## 2. File đã tạo / chỉnh sửa

### 2.1 [NEW] `frontend/src/app/core/services/sync.service.ts`
- Quản lý trạng thái kết nối mạng của ứng dụng (`isOnline` signal).
- Lắng nghe các sự kiện `online` và `offline` từ window để cập nhật trạng thái kết nối tự động.
- Quản lý dữ liệu cache cho các URL API với prefix `meup.cache.`.
- Quản lý hàng đợi request offline (`meup.offline.queue`):
  - `enqueue()`: Đóng gói request (URL, Method, Body, Headers) và lưu vào `localStorage`.
  - `syncQueue()`: Thực hiện gửi tuần tự các request offline lên server sử dụng `HttpClient` khi phát hiện thiết bị online trở lại.

### 2.2 [NEW] `frontend/src/app/core/interceptors/offline.interceptor.ts`
- Interceptor phân luồng xử lý:
  - **GET**: Khi online, gửi request bình thường và lưu dữ liệu trả về vào cache. Khi offline, tìm dữ liệu trong cache để trả về một `HttpResponse` giả lập (200 OK) hoặc trả về lỗi 503 nếu không có cache.
  - **POST/PUT/DELETE**: Khi online, gửi request trực tiếp. Khi offline, lưu thông tin request vào hàng đợi và trả về response giả lập `202 Accepted` kèm thông báo *"Được lưu để đồng bộ khi có mạng"*.
- Loại trừ các URL liên quan đến `/auth/` để không cache nhầm token/phiên làm việc.

### 2.3 [MODIFY] `frontend/src/app/app.config.ts`
- Đăng ký `offlineInterceptor` vào ứng dụng Angular.
- Đặt `offlineInterceptor` chạy trước `authInterceptor` để chặn request offline ngay lập tức, tránh việc refresh token vô ích khi không có kết nối mạng.

---

## 3. Quy trình đồng bộ mạng

```
[Mạng offline]
Thao tác ghi (POST/PUT/DELETE) ──> Interceptor ──> Lưu vào Sync Queue ──> Phản hồi 202 OK (Giao diện cập nhật)
                                                                 
[Mạng online trở lại]
Sự kiện "online" phát ra ──> SyncService.syncQueue() ──> Gửi request FIFO ──> Xóa khỏi queue
```

---

## 4. Build kết quả
- Angular build: ✅ **Thành công** (608 kB, warning budget nhỏ chấp nhận được).
- npx cap sync android: ✅ **Hoàn tất** (0.195s).
