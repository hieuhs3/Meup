# MeUp — Hướng dẫn Mobile (Capacitor)

> Capacitor 8 · Android · iOS · Angular 20

---

## 1. Tổng quan

MeUp chạy trên mobile thông qua **Capacitor** — bridge layer cho phép Angular web app chạy như native app trên Android và iOS.

**App ID**: `com.meup.app`

**Native features được tích hợp:**
- Camera (chụp/chọn ảnh đại diện)
- Haptic Feedback (rung nhẹ khi tương tác)
- Status Bar (màu sắc & style)
- Splash Screen
- Push Notifications

---

## 2. Cấu trúc mobile

```
frontend/
├── android/           # Android Studio project (auto-generated)
│   └── app/src/main/
│       ├── assets/public/  # Angular build được copy vào đây
│       └── AndroidManifest.xml
├── ios/               # Xcode project (cần Mac)
├── capacitor.config.ts # Cấu hình Capacitor
└── dist/frontend/browser/  # Angular build output → sync vào Android/iOS
```

---

## 3. Cài đặt Capacitor

### Packages đã cài

```json
// package.json (dependencies)
"@capacitor/core": "^8.4.2",
"@capacitor/cli": "^8.4.2",
"@capacitor/android": "^8.4.2",
"@capacitor/ios": "^8.4.2",
"@capacitor/app": "^8.1.0",
"@capacitor/camera": "^8.2.1",
"@capacitor/haptics": "^8.0.2",
"@capacitor/push-notifications": "^8.1.1",
"@capacitor/splash-screen": "^8.0.1",
"@capacitor/status-bar": "^8.0.2"
```

---

## 4. Cấu hình Capacitor (`capacitor.config.ts`)

```typescript
const config: CapacitorConfig = {
  appId: 'com.meup.app',
  appName: 'MeUp',
  webDir: 'dist/frontend/browser',

  // Dev mode: trỏ về Angular dev server (hot reload)
  ...(isDev && {
    server: {
      url: 'http://10.0.2.2:4200',  // Android emulator → host machine
      cleartext: true,
    },
  }),

  plugins: {
    SplashScreen: {
      launchShowDuration: 1500,
      backgroundColor: '#0f141b',   // dark theme
      splashFullScreen: true,
      splashImmersive: true,
    },
    StatusBar: {
      style: 'dark',
      backgroundColor: '#0f141b',
      overlaysWebView: false,
    },
    PushNotifications: {
      presentationOptions: ['badge', 'sound', 'alert'],
    },
  },

  android: {
    allowMixedContent: isDev,          // cho phép HTTP trong debug
    captureInput: true,
    webContentsDebuggingEnabled: isDev,
  },

  ios: {
    contentInset: 'always',            // tránh content bị notch che
    scrollEnabled: true,
    limitsNavigationsToAppBoundDomains: true,
  },
};
```

---

## 5. Workflow phát triển

### Dev với Android Emulator

```bash
# Terminal 1: Angular dev server
cd frontend && npm start  # http://localhost:4200

# Terminal 2: Backend API
cd backend/MeUp.Api && dotnet run --launch-profile http

# Android Emulator (đã được cấu hình trong capacitor.config.ts):
# App trỏ tới http://10.0.2.2:4200 → live reload khi sửa Angular
```

**Lưu ý networking:**
- `10.0.2.2` là địa chỉ đặc biệt của Android emulator trỏ về host machine's localhost.
- iOS Simulator dùng `localhost` bình thường.

---

### Build và Sync

```bash
# Bước 1: Build Angular
cd frontend && npm run build

# Bước 2: Sync sang Android/iOS (copy build + update plugins)
npx cap sync android
npx cap sync ios  # cần Mac

# Bước 3: Mở Android Studio / Xcode
npx cap open android
npx cap open ios
```

---

## 6. Native Plugins

### Camera (ảnh đại diện)

```typescript
// platform.service.ts / profile feature
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';

const image = await Camera.getPhoto({
  quality: 90,
  allowEditing: false,
  resultType: CameraResultType.Blob,
  source: CameraSource.Photos,  // hoặc Camera.CAMERA
});
// → upload blob lên /api/users/me/avatar
```

**Permissions cần khai báo:**

Android (`AndroidManifest.xml`):
```xml
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
<uses-permission android:name="android.permission.CAMERA" />
```

iOS (`Info.plist`):
```xml
<key>NSCameraUsageDescription</key>
<string>Để chụp ảnh đại diện</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>Để chọn ảnh đại diện</string>
```

---

### Haptics

```typescript
// haptics.service.ts
import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';

// Rung nhẹ khi tap
await Haptics.impact({ style: ImpactStyle.Light });

// Rung success khi lưu
await Haptics.notification({ type: NotificationType.Success });
```

Service `HapticsService` bọc lại và gracefully degrade trên web (no-op).

---

### Status Bar

```typescript
// Trong app bootstrap
import { StatusBar, Style } from '@capacitor/status-bar';

// Dark status bar (phù hợp dark theme)
await StatusBar.setStyle({ style: Style.Dark });
await StatusBar.setBackgroundColor({ color: '#0f141b' });
```

---

### Push Notifications (cấu hình tương lai)

```typescript
// push-notification.service.ts
import { PushNotifications } from '@capacitor/push-notifications';

// Yêu cầu quyền
await PushNotifications.requestPermissions();

// Đăng ký nhận push
await PushNotifications.register();

// Lắng nghe token registration
PushNotifications.addListener('registration', (token) => {
  // Gửi token lên server để lưu
});
```

**Chú ý**: Push Notifications cần VAPID key (Web) hoặc FCM/APNs credential (Native). Hiện tại chưa triển khai fully (G10).

---

## 7. CORS cho Capacitor

Backend cần cho phép origin từ Capacitor:

```json
// appsettings.json
{
  "Cors": {
    "Origins": [
      "http://localhost:4200",    // Angular web
      "capacitor://localhost",    // iOS native production
      "http://localhost",          // iOS Simulator
      "http://10.0.2.2:4200",     // Android emulator → Angular dev
      "http://10.0.2.2"           // Android emulator → API
    ]
  }
}
```

---

## 8. Safe Area (Notch & Home Indicator)

CSS để tránh content bị che bởi notch (iOS) và home indicator:

```scss
// Áp dụng padding safe area
.main-content {
  padding-top: env(safe-area-inset-top);
  padding-bottom: env(safe-area-inset-bottom);
}

// Mobile tab bar
.mobile-tab-bar {
  padding-bottom: env(safe-area-inset-bottom);
}
```

---

## 9. Build APK Release (Android)

```bash
# 1. Build Angular production
cd frontend && npm run build

# 2. Comment out server block trong capacitor.config.ts (prod mode)

# 3. Sync
npx cap sync android

# 4. Mở Android Studio
npx cap open android

# Trong Android Studio:
# Build → Generate Signed Bundle / APK
# → Chọn APK
# → Tạo keystore (lần đầu) hoặc dùng keystore có sẵn
# → Build Release APK
```

**Output**: `android/app/release/app-release.apk`

---

## 10. Kiểm tra trên device thật

```bash
# Kết nối Android device (bật Developer Mode + USB Debugging)
adb devices  # kiểm tra device đã được nhận

# Run trực tiếp trên device
npx cap run android --target <device-id>
```

---

## 11. Troubleshooting

| Vấn đề | Giải pháp |
|--------|-----------|
| App không load được API | Kiểm tra `10.0.2.2` trong config; đảm bảo backend đang chạy |
| CORS error trên Android | Thêm `http://10.0.2.2:4200` vào CORS origins |
| Camera permission bị từ chối | Kiểm tra AndroidManifest.xml và Info.plist |
| Hot reload không hoạt động | Đảm bảo `server.url` trong capacitor.config.ts trỏ đúng |
| Build failed: OOM | Tăng heap size Android Studio: `-Xmx4g` |
| iOS cần Mac | Không thể build iOS trên Windows — cần Mac/MacBook |
