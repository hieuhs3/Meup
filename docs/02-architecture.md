# MeUp — Kiến trúc & Mô hình dữ liệu

Phiên bản: MVP 1.0

## 1. Nguyên tắc

- HTML + CSS + JavaScript thuần, **không framework, không bước build**.
- Single Page App: một `index.html`, chuyển khu vực bằng JS (không reload).
- Toàn bộ trạng thái nằm trong một object `state`, được lưu/đọc qua `localStorage`.
- Tách biệt rõ: **dữ liệu** (store) — **logic** (tính toán) — **giao diện** (render).

## 2. Cấu trúc thư mục

```
MeUp/
├── index.html          # Khung HTML + sidebar + vùng nội dung + modal
├── styles.css          # Toàn bộ CSS
├── app.js              # Điểm vào: điều hướng, store, render các view
├── docs/               # Tài liệu
└── tests/              # Test logic thuần (chạy bằng Node)
    └── logic.test.js
```

> Ghi chú: MVP gom logic trong `app.js` cho gọn. Nếu file phình to (>800 dòng) sẽ tách
> `store.js`, `finance.js`, `health.js`, `work.js` ở vòng sau.

## 3. Mô hình dữ liệu (localStorage)

Khóa lưu: `meup.data` → JSON của object `state`:

```
state = {
  version: 1,
  finance: {
    transactions: [
      { id, type: "income"|"expense", amount: number, category: string,
        date: "YYYY-MM-DD", note: string }
    ]
  },
  health: {
    logs: [
      { date: "YYYY-MM-DD", weight: number|null, sleep: number|null,
        water: number|null, workout: string }
    ]   // mỗi ngày tối đa 1 bản ghi, key theo date
  },
  work: {
    tasks:  [ { id, title: string, due: "YYYY-MM-DD"|null, done: boolean } ],
    goals:  [ { id, title: string, progress: number /*0-100*/ } ],
    habits: [ { id, title: string, history: ["YYYY-MM-DD", ...] /*ngày đã làm*/ } ]
  }
}
```

## 4. Hợp đồng module (contract trong app.js)

**Store**
- `Store.load()` → đọc state từ localStorage (hoặc state rỗng mặc định).
- `Store.save()` → ghi state hiện tại xuống localStorage.
- `Store.export()` → trả chuỗi JSON; `Store.import(json)` → nạp & validate.
- `uid()` → sinh id duy nhất.

**Logic thuần (không chạm DOM — để test được)**
- `finance.balance(txs)`, `finance.monthlyTotals(txs, "YYYY-MM")`.
- `health.latest(logs)`, `health.deltaFromPrevious(logs)`.
- `work.streak(habit, today)`, `work.overdueTasks(tasks, today)`, `work.clampProgress(n)`.

**Render (đọc state → vẽ HTML)**
- `renderDashboard()`, `renderFinance()`, `renderHealth()`, `renderWork()`.
- `navigate(view)` đổi khu vực hiển thị.

**An toàn**
- `escapeHtml(str)` dùng cho mọi dữ liệu người dùng trước khi đưa vào DOM (YC-G5).

## 5. Luồng dữ liệu

```
Người dùng thao tác (form/nút)
      │
      ▼
  cập nhật state  ──►  Store.save()  ──►  localStorage
      │
      ▼
  render lại view hiện tại  ──►  DOM
```

## 6. Quyết định kỹ thuật

- **localStorage** thay vì IndexedDB: dữ liệu cá nhân nhỏ, đơn giản, đủ dùng.
- **Không bundler**: mở file là chạy, không phụ thuộc Node để dùng app.
- **Test bằng Node**: tách logic thuần ra để chạy `node tests/logic.test.js` không cần trình duyệt.
- **Ngày tháng** dùng chuỗi `YYYY-MM-DD` để so sánh/lọc đơn giản.
