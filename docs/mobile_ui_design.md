# MeUp Mobile — UI/UX Design

> **Công nghệ:** Capacitor + Angular · **Nền tảng:** iOS + Android · **Design:** Dark mode + Light mode

---

## Design System Mobile

### Color Palette (kế thừa từ web)
| Token | Dark | Light | Dùng cho |
|-------|------|-------|----------|
| `--bg` | `#0f141b` | `#f4f6fb` | Nền toàn màn hình |
| `--surface` | `#1a212b` | `#ffffff` | Cards, sheets |
| `--primary` | `#5b7cfa` | `#4361ee` | CTA, active tab |
| `--success` | `#34c46f` | `#1f9d55` | Thu nhập, healthy |
| `--danger` | `#ef5a6e` | `#e23b50` | Chi tiêu, cảnh báo |
| `--muted` | `#9aa5b3` | `#76808f` | Text phụ |

### Typography
- **Font**: Inter (thay Segoe UI) — chuẩn hơn cho mobile
- **Scale**: 12 / 14 / 16 / 20 / 24 / 32px
- **Weight**: 400 (body) / 600 (heading) / 700 (big number)

### Spacing & Touch Targets
- Minimum touch target: **44×44px** (iOS HIG)
- Base unit: **8px** (4 / 8 / 12 / 16 / 24 / 32)
- Border radius: `8` (input) / `12` (card) / `20` (bottom sheet) / `999` (pill)

### Elevation (Dark mode)
- Level 0: `#0f141b` (background)
- Level 1: `#1a212b` (cards)
- Level 2: `#1e2733` (elevated sheets)
- Shadow: `0 8px 32px rgba(0,0,0,0.4)` với blur

---

## Màn hình 1 — Hôm Nay (Today)

![Today Screen](file:///C:/Users/ADMIN/.gemini/antigravity-ide/brain/1dd30d15-9223-4e90-a7ac-4b4c7cf7a9cf/mobile_today_screen_1784107604613.png)

### Cấu trúc layout
```
StatusBar (native)
├── Header: Avatar + "Hôm nay" + Date + notification bell
├── Date Navigator: ← Thứ 3, 15/7/2026 →
├── Finance Summary Row (horizontal scroll 3 cards)
│   ├── Card: Thu +8.5M (teal)
│   ├── Card: Chi -3.2M (red)  
│   └── Card: Số dư 5.3M (blue)
├── Sức khỏe Section
│   └── Metric pills: ⚖️70kg · 😴7.5h · 💧2L · 🏃Done
├── Công việc Section
│   └── Task list (top 3, "Xem thêm" link)
├── Thói quen Section
│   └── Habit quick-check row
└── FAB: + (gradient blue-purple, bottom right)

Bottom Tab Bar (safe area)
├── 🏠 Hôm nay [ACTIVE - blue glow]
├── 💰 Tài chính
├── 💪 Sức khỏe
├── ✅ Công việc
└── ☰ Thêm (More)
```

### Interactions
- **Swipe left/right** trên date navigator → đổi ngày
- **Pull to refresh** → reload data hôm nay
- **Tap FAB** → Quick Add bottom sheet (chọn loại: Giao dịch / Task / Nhật ký)
- **Tap section header** → navigate sang màn hình tương ứng

---

## Màn hình 2 — Tài Chính (Finance)

![Finance Screen](file:///C:/Users/ADMIN/.gemini/antigravity-ide/brain/1dd30d15-9223-4e90-a7ac-4b4c7cf7a9cf/mobile_finance_screen_1784107625503.png)

### Cấu trúc layout
```
StatusBar
├── Header: "Tài chính" + search icon
├── Month Navigator: ← Tháng 7/2026 →
├── Summary Card (gradient)
│   ├── Thu: +8,500,000đ  (teal)
│   ├── Chi: -3,200,000đ  (red)
│   └── Số dư: +5,300,000đ (white)
├── Budget Section (collapsible)
│   ├── Progress bar: Ăn uống 65% (orange)
│   ├── Progress bar: Di chuyển 30% (teal)
│   └── Progress bar: Mua sắm 90% (red warning)
├── Transactions List (grouped by date)
│   ├── "Hôm nay" header
│   │   ├── Row: ☕ Cà phê · Ăn uống · -45,000đ [swipeable]
│   │   └── Row: 💰 Lương · Thu nhập · +8,000,000đ
│   └── "Hôm qua" header
│       └── Row: 🛒 Siêu thị · -320,000đ
└── FAB: + "Thêm"

Bottom Tab Bar (Finance tab active)
```

### Interactions
- **Swipe left** trên transaction row → lộ nút 🗑️ Xóa + ✏️ Sửa
- **Tap transaction** → Detail / Edit bottom sheet
- **FAB tap** → Add Transaction bottom sheet (xem mockup 4)
- **Long press** transaction → multi-select mode

---

## Màn hình 3 — Sức Khỏe & Công Việc

![Work & Health Screens](file:///C:/Users/ADMIN/.gemini/antigravity-ide/brain/1dd30d15-9223-4e90-a7ac-4b4c7cf7a9cf/mobile_work_health_screens_1784107650550.png)

### Công Việc layout
```
StatusBar
├── Header: "Công việc" + filter icon
├── Segment tabs: Tất cả | Hôm nay | Quá hạn | Đang làm
├── Task List
│   ├── ⬤ [priority] ○ Task title · "Hạn: 16/7" badge [swipeable]
│   ├── ✅ [done] Task đã xong (strikethrough, muted)
│   └── 🔴 [overdue] Task quá hạn (red accent)
├── "Thói quen" Section
│   ├── Habit row: title + 7-day dots + 🔥 streak badge
│   └── [tap circle] → check today + haptic feedback
└── FAB: + Add Task / Habit

Bottom Tab Bar (Work tab active)
```

### Sức Khỏe layout
```
StatusBar
├── Header: "Sức khỏe" + date
├── BMI Ring: donut chart 22.1 "Bình thường" (teal)
├── 2×2 Metric Grid
│   ├── ⚖️ Cân nặng: [70.5] kg · stepper +/-
│   ├── 😴 Giờ ngủ: [7.5] h · slider
│   ├── 💧 Nước: [2.0] L · +/- buttons
│   └── 🏃 Hoạt động: [text input]
├── "So với hôm qua" diff row
│   └── ↑ 0.5kg · ↓ 0.5h ngủ · ↑ 0.5L nước
└── "Lưu nhật ký" button (pinned bottom)

Bottom Tab Bar (Health tab active)
```

### Interactions — Công việc
- **Tap ○** → complete task với animation checkmark + haptic
- **Swipe left** → delete/edit
- **Long press** → multi-select, bulk complete/delete
- **Kanban toggle** (header button) → switch sang Kanban view

### Interactions — Sức khỏe
- **Numeric keyboard** auto-focuses khi tap metric card
- **Stepper +/-** có haptic feedback mỗi bước
- **Slide to save** hoặc tap "Lưu" → optimistic update

---

## Màn hình 4 — Add Transaction (Bottom Sheet)

![Add Transaction Bottom Sheet](file:///C:/Users/ADMIN/.gemini/antigravity-ide/brain/1dd30d15-9223-4e90-a7ac-4b4c7cf7a9cf/mobile_add_transaction_bottomsheet_1784107671280.png)

### Cấu trúc
```
Backdrop (blur + dim)
└── Bottom Sheet (slides up, drag handle)
    ├── Drag handle bar
    ├── "Thêm giao dịch"  +  [✕ Đóng]
    ├── Toggle: [Chi tiêu] | [Thu nhập]
    ├── Amount Display: "45,000 đ" (large, centered)
    ├── Category Scroll (horizontal pills)
    │   └── 🍜Ăn uống · 🚗Di chuyển · 🛒Mua sắm · ...
    ├── Note input: "Ghi chú..."
    ├── Date picker row: 📅 "Hôm nay, 15/7"
    └── [Lưu giao dịch] (full-width gradient button)
    
Safe area bottom padding
```

### Interactions
- **Drag down** sheet → dismiss
- **Tap backdrop** → dismiss
- **Tab Chi/Thu** → thay màu accent (đỏ/xanh)
- **Amount**: native number keyboard tự mở
- **Category tap** → haptic + selection highlight
- **Save**: haptic medium + close sheet + optimistic update list

---

## Navigation Architecture

```
App
├── /login          Auth screens (full screen)
├── /register
└── /app            Shell với Bottom Tab Bar
    ├── /today      [Tab 1] 🏠 Hôm nay
    ├── /finance    [Tab 2] 💰 Tài chính
    ├── /health     [Tab 3] 💪 Sức khỏe  
    ├── /work       [Tab 4] ✅ Công việc
    └── /more       [Tab 5] ☰ Thêm
        ├── /journal        Nhật ký
        ├── /calendar       Lịch
        ├── /stats          Thống kê
        ├── /knowledge      Kiến thức
        ├── /career         Sự nghiệp
        ├── /documents      Tài liệu
        ├── /notifications  Thông báo
        ├── /settings       Cài đặt
        └── /profile        Hồ sơ
```

### Bottom Tab Bar spec
| Tab | Icon | Label | Badge |
|-----|------|-------|-------|
| Hôm nay | `home` | Hôm nay | - |
| Tài chính | `wallet` | Tài chính | - |
| Sức khỏe | `heart-pulse` | Sức khỏe | - |
| Công việc | `check-square` | Công việc | overdue count |
| Thêm | `grid` | Thêm | notification dot |

---

## Components Mobile mới cần build

| Component | Mô tả |
|-----------|-------|
| `MobileTabBarComponent` | Bottom navigation 5 tab |
| `BottomSheetComponent` | Draggable sheet, backdrop |
| `QuickAddSheetComponent` | FAB → chọn loại + form |
| `SwipeableListItemComponent` | Swipe reveal actions |
| `PullToRefreshDirective` | Pull-to-refresh gesture |
| `MetricStepperComponent` | +/- input cho Health |
| `DateNavigatorComponent` | ← date → với swipe |
| `HapticService` | Wrapper Capacitor Haptics |
| `PlatformService` | isIOS/isAndroid/isCapacitor |

---

## Bước tiếp theo

Sau khi duyệt UI/UX này, sẽ thực thi theo thứ tự:

1. **M0** — Cài Capacitor, cấu hình `capacitor.config.ts`, build thử
2. **M1** — Tạo `MobileTabBarComponent` + `PlatformService`, cập nhật Shell layout
3. **M2** — Implement từng màn hình theo mockup trên
4. **M3** — Native: Push notification, Haptics, Camera
5. **M5** — Build APK debug → test trên device thật
