# Mobile M1 — Mobile UX Foundation

Ngày: 2026-07-15
Trạng thái: 🔄 **Đang thực thi**
Phụ thuộc: M0 hoàn tất

---

## 1. Mục tiêu

Xây dựng nền tảng UX cho mobile: phát hiện môi trường chạy, Bottom Tab Bar 5 tab,
mobile header, và CSS mobile-first. Desktop vẫn hoạt động bình thường với sidebar.

---

## 2. File đã tạo / chỉnh sửa

### 2.1 [NEW] `frontend/src/app/core/services/platform.service.ts`

Service phát hiện môi trường:
- `platform` signal: `'android' | 'ios' | 'web'`
- `isNative` signal: true khi chạy trong Capacitor native
- `isAndroid` / `isIOS` / `isWeb` signals
- `useMobileLayout` signal: true khi native OR `window.innerWidth < 768`
  → auto-update khi resize window

### 2.2 [NEW] `frontend/src/app/layout/mobile-tab-bar/mobile-tab-bar.ts`

Bottom Tab Bar component:
- 4 tab chính: Hôm nay · Tài chính · Sức khỏe · Công việc
- Nút "Thêm" (⋯) → mở `BottomMoreSheet`
- Active indicator: glow bar phía trên + icon scale animation
- Badge thông báo (dot đỏ) trên tab "Thêm" khi có unread notification
- `moreActive` signal: tự detect khi route không thuộc 4 tab chính
- Đóng sheet tự động khi navigate

### 2.3 [NEW] `frontend/src/app/layout/mobile-tab-bar/bottom-more-sheet.ts`

Sheet "Thêm" — các route còn lại:
- 11 mục: Lịch trình · Nhật ký · Kiến thức · Sự nghiệp · Tài liệu
  · Thống kê · Gợi ý AI · Tìm kiếm · Thông báo · Hồ sơ · Cài đặt
- Grid 4 cột với icon + label
- Backdrop blur + slide-up animation 300ms
- Tap backdrop để đóng
- Admin item hiện có điều kiện

### 2.4 [MODIFY] `frontend/src/app/layout/shell.ts`

Cập nhật Shell layout:
- Inject `PlatformService`
- Import `MobileTabBar`
- Sidebar: chỉ render khi `!platform.useMobileLayout()`
- Mobile header: sticky, logo + avatar (tap → /profile)
- `<app-mobile-tab-bar>`: chỉ render khi `platform.useMobileLayout()`
- Content: thêm `.mobile-content` class → padding-bottom để không bị tab bar che

### 2.5 [MODIFY] `frontend/src/styles.scss`

- Thêm `@import` Google Fonts **Inter** (400/500/600/700)
- Thêm `font-family: 'Inter', ...` cho html, body
- `.shell.mobile`: flex-direction column
- `-webkit-tap-highlight-color: transparent` — xóa highlight mobile
- `-webkit-overflow-scrolling: touch` — momentum scrolling iOS
- Cập nhật `@media (max-width: 767px)`: chỉ áp dụng padding/cards

---

## 3. Kiến trúc điều hướng

```
Shell
├── [Desktop] Sidebar (240px) + Content (flex: 1)
└── [Mobile]  Mobile Header (52px)
             + Content (flex: 1, pb = 56px + safe-area)
             + MobileTabBar (fixed bottom, 56px + safe-area)
                 ├── Tab: Hôm nay → /app/today
                 ├── Tab: Tài chính → /app/finance
                 ├── Tab: Sức khỏe → /app/health
                 ├── Tab: Công việc → /app/work
                 └── Thêm → BottomMoreSheet (overlay)
```

---

## 4. CSS Mobile quan trọng

```scss
/* Safe area cho notch/home indicator */
padding-bottom: env(safe-area-inset-bottom, 0px);
padding-top: env(safe-area-inset-top, 0px);

/* Tab bar height */
height: calc(56px + env(safe-area-inset-bottom, 0px));

/* Content không bị che */
padding-bottom: calc(56px + env(safe-area-inset-bottom, 0px) + 8px);

/* Momentum scrolling iOS */
-webkit-overflow-scrolling: touch;
```

---

## 5. Build kết quả

- Angular build: ✅ **8.5s, không có lỗi** (main 524.90 kB, styles 14.51 kB)
- `npx cap sync android`: ✅ 0.22s, 6 plugin, thành công
- Test trên emulator: cần mở Android Studio (`npx cap open android`)

---

## 6. Bước tiếp theo — M2: Tối ưu từng màn hình

- `today`: pull-to-refresh, FAB quick-add, swipe date navigator
- `finance`: bottom sheet form, swipe-to-delete transactions
- `health`: numeric stepper cards, BMI ring
- `work`: swipe-to-complete, habit one-tap, Kanban responsive
