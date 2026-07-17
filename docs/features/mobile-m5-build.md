# Mobile M5 — Build & Deploy Handbook

Ngày: 2026-07-15
Trạng thái: ✅ **HOÀN TẤT**
Phụ thuộc: M4 hoàn tất

---

## 1. Quy trình phát triển ứng dụng di động

Kể từ khi tích hợp Capacitor, quy trình cập nhật giao diện và logic của ứng dụng di động như sau:

```bash
# Bước 1: Di chuyển vào thư mục frontend
cd frontend

# Bước 2: Build code Angular (Frontend) sang production bundle
npm run build

# Bước 3: Đồng bộ source code web đã build vào các project Native (Android/iOS)
npx cap sync
```

---

## 2. Hướng dẫn Build Android (APK / AAB)

Thư mục Android native nằm tại: `frontend/android/`.

### 2.1 Mở ứng dụng trong Android Studio
Bạn có thể mở trực tiếp Android Studio hoặc chạy lệnh:
```bash
npx cap open android
```

### 2.2 Build Debug APK bằng Command Line
Nếu máy tính đã cài đặt JDK và Gradle, bạn có thể build trực tiếp từ terminal của dự án mà không cần mở Android Studio:
```bash
cd frontend/android
./gradlew assembleDebug  # Hoặc gradlew.bat trên Windows
```
**Kết quả:** File APK thử nghiệm sẽ xuất hiện tại `frontend/android/app/build/outputs/apk/debug/app-debug.apk`. Bạn có thể gửi file này trực tiếp vào điện thoại Android để cài đặt và test.

#### 💡 Khắc phục lỗi: SDK location not found
Nếu quá trình build báo lỗi thiếu SDK location, đó là vì máy tính chưa cấu hình biến môi trường `ANDROID_HOME` hoặc file `local.properties`. 
**Cách xử lý:**
1. Hãy mở thư mục `frontend/android` trong Android Studio (`npx cap open android`), Android Studio sẽ tự động tạo file `local.properties` cho bạn.
2. Hoặc bạn có thể tự tạo file `local.properties` tại đường dẫn `frontend/android/local.properties` và thêm dòng cấu hình sau (thay thế bằng đường dẫn chứa SDK trên máy bạn):
   ```properties
   sdk.dir=C\:\\Users\\Tên_User\\AppData\\Local\\Android\\Sdk
   ```

### 2.3 Cấu hình Release Build (Ký số Keystore)
Để đưa app lên Google Play Store, bạn cần tạo file AAB (Android App Bundle) đã được ký số:

1. **Tạo Keystore** (nếu chưa có):
   ```bash
   keytool -genkey -v -keystore meup-release-key.jks -keyalg RSA -keysize 2048 -validity 10000 -alias meup-alias
   ```
2. **Cấu hình trong Android Studio**:
   - Mở Android Studio.
   - Chọn **Build > Generate Signed Bundle / APK...**
   - Chọn **Android App Bundle** (khuyên dùng cho Store) hoặc **APK**.
   - Trỏ tới file Keystore vừa tạo, nhập mật khẩu và chọn **Build Type: release**.

---

## 3. Hướng dẫn Build iOS (IPA)
*(Yêu cầu sử dụng hệ điều hành macOS và cài đặt Xcode)*

### 3.1 Thêm iOS platform & Sync
```bash
# Thêm platform iOS
npx cap add ios

# Đồng bộ code Angular sang Xcode project
npx cap sync ios
```

### 3.2 Mở project trong Xcode
```bash
npx cap open ios
```

### 3.3 Đăng ký tài khoản Developer và ký ứng dụng (Signing)
1. Trong Xcode, chọn project **App** ở cột bên trái.
2. Mở tab **Signing & Capabilities**.
3. Chọn **Automatically manage signing**.
4. Chọn **Team** của bạn (Tài khoản Apple Developer).
5. Thay đổi **Bundle Identifier** nếu cần (mặc định: `com.meup.app`).

### 3.4 Build và xuất file IPA
1. Chọn Target thiết bị là **Any iOS Device (arm64)**.
2. Chọn **Product > Archive** trên thanh menu.
3. Khi quá trình Archive kết thúc, cửa sổ Organizer sẽ hiện ra. Bấm **Distribute App** để xuất bản lên TestFlight hoặc xuất file IPA để test ad-hoc.

---

## 4. Lưu ý về môi trường API (Endpoints)
- **Môi trường Emulator (Android)**: Do emulator chạy trên sandbox riêng, nó sử dụng IP `10.0.2.2` để truy cập vào `localhost` của máy chủ phát triển.
- **Môi trường Thiết bị thật (Test qua Wi-Fi)**: Thiết bị di động và máy chủ API cần kết nối chung mạng Wi-Fi. Bạn phải thay đổi URL API trong file cấu hình environment thành IP LAN của máy tính phát triển (ví dụ: `http://192.168.1.100:5000`).
- **Môi trường Production (Deploy Store)**: Cấu hình API trỏ về domain chính thức của hệ thống (ví dụ: `https://api.meup.vn`).

---

## 5. Walkthrough bàn giao toàn bộ dự án Mobile

Toàn bộ quá trình chuẩn bị ứng dụng di động cho dự án MeUp đã được lập tài liệu chi tiết theo từng giai đoạn trong thư mục `docs/`:
1. 📋 [docs/10-mobile-plan.md](file:///d:/018_Anthorpic/MeUp/docs/10-mobile-plan.md) — Kế hoạch tổng thể và lộ trình di động.
2. 📋 [docs/features/mobile-m0-setup.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m0-setup.md) — Cấu hình khởi tạo Capacitor, dependencies và whitelist CORS backend.
3. 📋 [docs/features/mobile-m1-ux.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m1-ux.md) — Nền tảng UI/UX: Bottom Tab Bar, Mobile Layout Shell, Safe Area CSS và detect môi trường.
4. 📋 [docs/features/mobile-m2-screens.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m2-screens.md) — Tối ưu hóa màn hình Today, Finance Sheet và các metric chips.
5. 📋 [docs/features/mobile-m3-native.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m3-native.md) — Phản hồi xúc giác Haptics, tự động chỉnh StatusBar theo theme, xin quyền Push Notifications và Camera upload avatar.
6. 📋 [docs/features/mobile-m4-offline.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m4-offline.md) — Giải pháp chạy offline: cache GET requests và sync queue lưu POST/PUT/DELETE.
7. 📋 [docs/features/mobile-m5-build.md](file:///d:/018_Anthorpic/MeUp/docs/features/mobile-m5-build.md) — Tài liệu hướng dẫn build và đóng gói release (file này).
