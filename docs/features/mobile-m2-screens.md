# Mobile M2 — Tối ưu màn hình mobile

Ngày: 2026-07-15
Trạng thái: ✅ **HOÀN TẤT**
Phụ thuộc: M1 hoàn tất

---

## 1. Mục tiêu

Tối ưu các màn hình chính cho trải nghiệm mobile:
- **Today**: DateNavigator, finance summary pills, metric chips, FAB quick-add
- **Finance**: FAB + bottom sheet thêm giao dịch
- Không phá vỡ desktop UX (dùng `platform.useMobileLayout()` để phân nhánh)

---

## 2. File đã tạo / chỉnh sửa

### 2.1 [NEW] `frontend/src/app/core/components/date-navigator.ts`

Component điều hướng ngày mobile-friendly:
- Nút ◀ / ▶ tròn 40×40px
- Label ngày ở giữa
- Chip "Về hôm nay" xuất hiện khi không phải ngày hiện tại
- Input signals: `label`, `isToday` — Output events: `prev`, `next`, `goToday`

### 2.2 [NEW] `frontend/src/app/core/components/quick-add-fab.ts`

FAB thêm nhanh cho Today (mobile only):
- Gradient button tròn, vị trí fixed bottom-right (trên tab bar)
- Tap → mini-menu 4 action bung ra với animation
- Actions: Giao dịch → /finance · Sức khỏe → /health · Task → /work · Nhật ký → /journal
- Đóng khi navigate hoặc tap backdrop

### 2.3 [NEW] `frontend/src/app/core/components/swipeable-item.directive.ts`

Directive swipe-to-reveal (sẵn sàng để dùng cho list items):
- Touch start/move/end tracking
- Snap sang trạng thái mở khi kéo qua 40% threshold
- `close()` method để đóng từ bên ngoài
- `maxSwipe` input (mặc định 88px)

### 2.4 [MODIFY] `frontend/src/app/features/today/today.ts`

- Thêm import `PlatformService`, `DateNavigator`, `QuickAddFab`
- Thêm `platform = inject(PlatformService)`
- Thêm CSS: `.summary-row`, `.sum-pill`, `.metric-row`, `.metric-chip`

### 2.5 [MODIFY] `frontend/src/app/features/today/today.html`

Layout phân nhánh desktop / mobile:
- **Desktop**: date-bar ngang như cũ + cards đầy đủ
- **Mobile**:
  - `<app-date-navigator>` thay date-bar
  - Summary row: 3 pill cards (Thu / Chi / Số dư) cuộn ngang
  - Finance card: text gọn "Xem lịch sử →"
  - Health card: metric chips ngang (⚖️ · 😴 · 💧 · 🏃) thay text dài
  - `<app-quick-add-fab>` fixed bottom-right

### 2.6 [NEW] `frontend/src/app/features/finance/mobile-finance-sheet.ts`

Bottom sheet thêm giao dịch mobile:
- Slide-up animation 300ms, backdrop blur
- Toggle Chi / Thu (đổi màu accent đỏ/xanh)
- Nhập số tiền: input lớn 2rem, underline focus
- Category scroll ngang: chips với màu danh mục
- Note text + date picker
- Save button gradient blue-purple full-width
- Gọi `FinanceService.createTransaction()`

### 2.7 [MODIFY] `frontend/src/app/features/finance/finance.ts`

- Thêm `PlatformService`, `MobileFinanceSheet` imports
- Thêm `platform = inject(PlatformService)`
- Thêm `showMobileSheet = signal(false)`
- Thêm public `reloadAfterSave()` method

### 2.8 [MODIFY] `frontend/src/app/features/finance/finance.html`

- Thêm FAB `+` (fixed bottom, chỉ mobile)
- Thêm `<app-mobile-finance-sheet>` (khi `showMobileSheet()`)

---

## 3. Build kết quả

- Angular build: ✅ **5.9s, không lỗi** (main 542.59 kB)
- `npx cap sync android`: ✅ 0.21s
- Lỗi đã fix: arrow fn trong template, method name sai, private method từ template, implicit any

---

## 4. Kiến trúc Mobile-first pattern

Mẫu áp dụng nhất quán trong M2:
```typescript
// Component inject PlatformService
readonly platform = inject(PlatformService);
```
```html
<!-- Template phân nhánh -->
@if (!platform.useMobileLayout()) {
  <!-- Desktop UI -->
} @else {
  <!-- Mobile UI -->
}

<!-- Mobile-only components -->
@if (platform.useMobileLayout()) {
  <app-quick-add-fab />
}
```

---

## 5. Bước tiếp theo — M3: Native Features

- `HapticsService` — vibration khi hoàn thành task / lưu
- `PushNotificationService` — FCM push notification
- Camera cho avatar upload (thay input file)
- StatusBar style sync với dark mode
