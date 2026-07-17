# Mobile M0 — Setup Capacitor

Ngày: 2026-07-15
Trạng thái: ✅ **HOÀN TẤT** (build thử → add Android → sync → CORS → sẵn sàng mở Android Studio)
Phụ thuộc: Angular 20 frontend đã build được, backend API sẵn sàng

---

## 1. Mục tiêu

Cài đặt Capacitor vào Angular project hiện tại, cấu hình đầy đủ cho iOS + Android,
build thử APK debug để xác nhận pipeline hoạt động.

---

## 2. Những gì đã làm

### 2.1 Cài Capacitor packages (2026-07-15)

```bash
# Chạy tại: frontend/
npm install @capacitor/core @capacitor/cli \
  @capacitor/android @capacitor/ios \
  @capacitor/push-notifications @capacitor/camera \
  @capacitor/haptics @capacitor/status-bar \
  @capacitor/app @capacitor/splash-screen --save
```

**Kết quả:** 77 packages thêm vào, tổng 621 packages, không có lỗi critical.

### 2.2 Khởi tạo Capacitor project (2026-07-15)

```bash
# Chạy tại: frontend/
npx cap init "MeUp" "com.meup.app" --web-dir dist/frontend/browser
```

**Kết quả:** Tạo ra `frontend/capacitor.config.ts` với App ID `com.meup.app`.

### 2.3 Cập nhật `capacitor.config.ts` (2026-07-15)

File: `frontend/capacitor.config.ts`

Các cấu hình đã thêm:
- **SplashScreen**: màu nền `#0f141b` (dark), tự ẩn sau 1.5s, fullscreen + immersive (Android)
- **StatusBar**: style dark, màu `#0f141b`, không overlay WebView
- **PushNotifications**: options badge + sound + alert
- **Camera**: khai báo (permissions cấu hình trong AndroidManifest / Info.plist)
- **android**: allowMixedContent + webContentsDebuggingEnabled (chỉ dev)
- **ios**: contentInset = 'always' (tránh content bị notch che)
- **server.url** (chỉ dev): `http://10.0.2.2:4200` — Android emulator trỏ về host machine để hot-reload

---

## 3. Những gì cần làm tiếp (M0 còn lại)

### 2.4 Build Angular production (2026-07-15)

```bash
# frontend/
npm run build
```

**Kết quả:** Build thành công trong 11.3s.
```
main-F7Z2G7LN.js   505.68 kB (118.73 kB gzip)
polyfills           34.59 kB
styles               6.27 kB
Output: dist/frontend/browser/
```

### 2.5 Add Android platform (2026-07-15)

```bash
npx cap add android
```

**Kết quả:** Tạo `frontend/android/` — Android Studio project với 6 plugin:
- `@capacitor/app@8.1.0`
- `@capacitor/camera@8.2.1`
- `@capacitor/haptics@8.0.2`
- `@capacitor/push-notifications@8.1.1`
- `@capacitor/splash-screen@8.0.1`
- `@capacitor/status-bar@8.0.2`

### 2.6 Cập nhật CORS backend (2026-07-15)

File: `backend/MeUp.Api/appsettings.json`

Thêm vào mảng `Cors.Origins`:
```json
"capacitor://localhost",   // iOS production (Capacitor native)
"http://localhost",        // iOS Simulator
"http://10.0.2.2:4200",    // Android emulator → Angular dev server
"http://10.0.2.2"          // Android emulator → API (nếu dùng local)
```

### 2.7 Sync Capacitor Android (2026-07-15)

```bash
npx cap sync android
```

**Kết quả:** Copy `dist/frontend/browser/` → `android/app/src/main/assets/public/` thành công trong 0.185s.


---

## 4. Ghi chú kỹ thuật

### Android Emulator → host machine
Android emulator không thể truy cập `localhost` của máy host trực tiếp.
Phải dùng địa chỉ đặc biệt `10.0.2.2` (đã set trong `capacitor.config.ts` dev mode).

### iOS Simulator → host machine
iOS simulator có thể dùng `localhost` bình thường → set `server.url = 'http://localhost:4200'` khi test trên iOS.

### Dev workflow (sau khi setup xong)
```bash
# Terminal 1: chạy Angular dev server
cd frontend && npm start

# Terminal 2: chạy API
cd backend/MeUp.Api && dotnet run --launch-profile http

# Trên Android emulator: app đã trỏ vào Angular server qua 10.0.2.2:4200
# → edit code Angular → thấy ngay trên emulator (live reload)
```

### Production workflow
```bash
cd frontend
npm run build                    # build Angular
npx cap sync                     # copy sang Android/iOS
npx cap open android             # mở Android Studio → build APK release
```

---

## 5. File thay đổi trong M0

| File | Thao tác | Mô tả | Trạng thái |
|------|---------|-------|----------|
| `frontend/package.json` | Cập nhật | Thêm 10 Capacitor packages | ✅ Xong |
| `frontend/capacitor.config.ts` | Tạo mới | Cấu hình đầy đủ: SplashScreen, StatusBar, Push, Camera, Android/iOS options | ✅ Xong |
| `frontend/android/` | Tạo mới (auto) | Android Studio project | ✅ Xong |
| `frontend/ios/` | Tạo mới (auto) | Xcode project | ⏳ Cần Mac |
| `backend/MeUp.Api/appsettings.json` | Cập nhật | Thêm 4 CORS origins cho mobile | ✅ Xong |
| `docs/10-mobile-plan.md` | Tạo mới | Kế hoạch tổng thể mobile | ✅ Xong |
| `docs/features/mobile-m0-setup.md` | Tạo mới | Tài liệu chi tiết M0 | ✅ Xong (file này) |
