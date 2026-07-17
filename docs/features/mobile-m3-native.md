# Mobile M3 — Native Features

Ngày: 2026-07-15
Trạng thái: ✅ **HOÀN TẤT**
Phụ thuộc: M2 hoàn tất

---

## 1. Mục tiêu
Tích hợp sâu các tính năng native của thiết bị di động bằng Capacitor Plugins:
- **Haptics (Phản hồi rung)**: Tạo cảm giác tương tác thật khi thao tác.
- **StatusBar**: Tự động đổi màu và kiểu (Dark/Light) theo theme của hệ thống/ứng dụng.
- **Push Notifications**: Đăng ký nhận token, yêu cầu quyền và lắng nghe sự kiện push notification.
- **Camera**: Cho phép chụp ảnh trực tiếp từ camera hoặc chọn ảnh từ thư viện để cập nhật avatar một cách tiện lợi.

---

## 2. File đã tạo / chỉnh sửa

### 2.1 [NEW] `frontend/src/app/core/services/haptics.service.ts`
- Tạo wrapper cho `@capacitor/haptics`.
- Hỗ trợ các kiểu rung: `light()` (habit check-in, tick checkbox), `medium()` (mở bottom sheet/confirm), `heavy()` (xóa item, hoàn thành task lớn), `save()` (double-tap nhẹ khi thành công), và `error()` (rung dồn dập khi thất bại).
- Graceful fallback: tự động no-op trên web browser bình thường.

### 2.2 [NEW] `frontend/src/app/core/services/push-notification.service.ts`
- Quản lý vòng đời push notification bằng `@capacitor/push-notifications`.
- Các bước thực hiện:
  1. Kiểm tra & yêu cầu quyền `checkPermissions()` / `requestPermissions()`.
  2. Đăng ký với APNS / FCM qua `PushNotifications.register()`.
  3. Lắng nghe `registration` thành công để nhận token FCM.
  4. Lắng nghe `pushNotificationReceived` (foreground) để tự động gọi `NotificationService.refreshUnread()`.
  5. Lắng nghe `pushNotificationActionPerformed` (background/app closed) để cập nhật trạng thái khi click.
- Placeholder để đồng bộ token với API backend (gửi lên endpoint `/notifications/register-token`).

### 2.3 [MODIFY] `frontend/src/app/core/services/theme.service.ts`
- Tích hợp `@capacitor/status-bar` trực tiếp vào theme logic.
- Khi đổi sang theme `dark`: chuyển Status Bar sang style tối (`Style.Dark`) và set màu nền status bar Android thành `#1e1e2e`.
- Khi đổi sang theme `light`: chuyển Status Bar sang style sáng (`Style.Light`) và set màu nền Android thành `#f4f6fb`.

### 2.4 [MODIFY] `frontend/src/app/features/finance/mobile-finance-sheet.ts`
- Inject `HapticsService`.
- Rung `medium` khi mở bottom sheet (`ngOnInit`).
- Rung `light` khi đổi loại giao dịch (Chi tiêu / Thu nhập) hoặc chọn danh mục.
- Rung `save` khi lưu thành công giao dịch mới.
- Rung `error` khi gặp lỗi validate hoặc API fail.

### 2.5 [MODIFY] `frontend/src/app/features/profile/profile.ts`
- Tích hợp `@capacitor/camera` và `PlatformService`.
- Giao diện:
  - Trên desktop: Hiển thị `<input type="file">` truyền thống.
  - Trên mobile native: Hiển thị nút "📸 Chụp ảnh / Chọn từ thư viện" sang trọng, dễ chạm.
- Logic Camera:
  - Gọi `Camera.getPhoto()` với nguồn `Prompt` (cho phép user chọn chụp ảnh mới hoặc lấy ảnh từ thư viện thiết bị).
  - Sử dụng `fetch` để chuyển đổi `image.webPath` cục bộ thành Blob, đóng gói thành File object và gọi API upload ảnh đại diện có sẵn của backend.

### 2.6 [MODIFY] `frontend/src/app/layout/shell.ts`
- Inject và kích hoạt `PushNotificationService.initPush()` trong `ngOnInit()` để tự động kích hoạt quyền thông báo ngay khi người dùng đăng nhập và truy cập vào giao diện chính của ứng dụng.

---

## 3. Build kết quả
- Angular build: ✅ **Đầy đủ, không lỗi** (Tất cả bundle đã tạo thành công).
- npx cap sync android: ✅ **Đồng bộ thành công** 6 native plugins sang Android Studio project.
