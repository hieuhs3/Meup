# MeUp Mobile — Kế hoạch tổng thể

Ngày bắt đầu: 2026-07-15
Trạng thái: 🔄 **Đang thực thi — M1 Mobile UX Foundation**

---

## Quyết định kiến trúc

| Hạng mục | Quyết định |
|----------|-----------|
| Công nghệ | **Capacitor + Angular** (tái sử dụng 80–90% code web hiện tại) |
| Nền tảng | **iOS + Android** (cả hai) |
| Backend | Dùng chung API ASP.NET Core hiện có, không viết lại |
| Auth | JWT access + refresh token — không đổi |
| Build | `ng build` → `npx cap sync` → Android Studio / Xcode |

---

## Lộ trình (Roadmap)

| Giai đoạn | Nội dung | Trạng thái |
|-----------|----------|-----------|
| **M0** | Setup Capacitor, cấu hình, build thử | ✅ Hoàn tất |
| **M1** | Mobile UX Foundation: Bottom Tab Bar, PlatformService, layout | ✅ Hoàn tất |
| **M2** | Tối ưu màn hình: Today, Finance, Health, Work | ✅ Hoàn tất |
| **M3** | Native features: Push, Haptics, Camera | ✅ Hoàn tất |
| **M4** | Offline & sync queue | ✅ Hoàn tất |
| **M5** | Build release APK/IPA, CI/CD, deploy store | ✅ Hoàn tất |

---

## Tài liệu chi tiết theo giai đoạn

- `docs/features/mobile-m0-setup.md` — M0: Cài đặt & cấu hình Capacitor
- `docs/features/mobile-m1-ux.md` — M1: Mobile UX Foundation *(tạo khi bắt đầu M1)*
- `docs/features/mobile-m2-screens.md` — M2: Tối ưu màn hình *(tạo khi bắt đầu M2)*
- `docs/features/mobile-m3-native.md` — M3: Native features *(tạo khi bắt đầu M3)*
- `docs/features/mobile-m4-offline.md` — M4: Offline *(tạo khi bắt đầu M4)*
- `docs/features/mobile-m5-deploy.md` — M5: Build & Deploy *(tạo khi bắt đầu M5)*

---

## Cấu trúc file mới thêm vào project

```
frontend/
├── capacitor.config.ts              [M0] Cấu hình Capacitor
├── android/                         [M0] Android project (auto-generated)
├── ios/                             [M0] iOS project (auto-generated)
└── src/app/
    ├── core/
    │   └── services/
    │       ├── platform.service.ts  [M1] Detect mobile/web/iOS/Android
    │       ├── haptics.service.ts   [M3] Wrapper Capacitor Haptics
    │       └── push-notification.service.ts [M3] FCM push
    ├── layout/
    │   ├── shell.ts                 [M1] Cập nhật: ẩn sidebar, thêm tab bar
    │   └── mobile-tab-bar/          [M1] Bottom Tab Bar component (mới)
    └── features/
        ├── today/                   [M2] Pull-to-refresh, FAB, swipe date
        ├── finance/                 [M2] Bottom sheet form, swipe-to-delete
        ├── health/                  [M2] Numeric stepper, BMI ring
        └── work/                    [M2] Swipe-to-complete, habit one-tap
```

---

## Design System Mobile

### Colors (kế thừa CSS variables)
- Background: `#0f141b` (dark) / `#f4f6fb` (light)
- Surface: `#1a212b` (dark) / `#ffffff` (light)
- Primary: `#5b7cfa` (dark) / `#4361ee` (light)
- Success: `#34c46f` | Danger: `#ef5a6e`

### Mobile-specific rules
- Touch target tối thiểu: **44 × 44 px**
- Safe area: `env(safe-area-inset-*)` cho notch + home indicator
- Font: **Inter** (thay Segoe UI)
- Bottom Tab Bar height: `56px` + safe area bottom

### Bottom Tab Bar — 5 tab
| Tab | Icon | Route | Badge |
|-----|------|-------|-------|
| Hôm nay | home | `/app/today` | — |
| Tài chính | wallet | `/app/finance` | — |
| Sức khỏe | heart-pulse | `/app/health` | — |
| Công việc | check-square | `/app/work` | overdue count |
| Thêm | grid | bottom sheet | notification dot |

---

## Liên kết tài liệu liên quan

- `docs/02-architecture.md` — Kiến trúc tổng thể
- `docs/03-feature-plan.md` — Kế hoạch chức năng
- `docs/06-deploy.md` — Hướng dẫn deploy
- `docs/09-commercial-strategy.md` — Chiến lược thương mại
