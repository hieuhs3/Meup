# MeUp — Kiến trúc Frontend

> Angular 20 · Standalone Components · Signals · TypeScript 5.9 · SCSS · Capacitor 8

---

## 1. Cấu trúc thư mục

```
frontend/src/
├── app/
│   ├── app.ts              # Root component
│   ├── app.routes.ts       # Routing configuration
│   ├── app.config.ts       # App-level providers
│   ├── core/               # Shared infrastructure
│   │   ├── api.config.ts   # API base URL config
│   │   ├── components/     # Shared components (date-navigator, etc.)
│   │   ├── guards/         # Route guards (authGuard, adminGuard)
│   │   ├── interceptors/   # HTTP interceptors (auth token)
│   │   ├── models/         # TypeScript interfaces/types
│   │   ├── pipes/          # Custom pipes
│   │   └── services/       # Core services (auth, theme, etc.)
│   ├── features/           # Feature modules (17 features)
│   │   ├── auth/           # login, register, forgot-password, etc.
│   │   ├── today/          # Tổng quan hôm nay
│   │   ├── finance/        # Tài chính
│   │   ├── health/         # Sức khỏe
│   │   ├── work/           # Công việc & mục tiêu
│   │   ├── journal/        # Nhật ký
│   │   ├── calendar/       # Lịch trình
│   │   ├── knowledge/      # Ghi chú
│   │   ├── career/         # Sự nghiệp
│   │   ├── documents/      # Tài liệu
│   │   ├── stats/          # Thống kê
│   │   ├── insights/       # AI Insights
│   │   ├── search/         # Tìm kiếm
│   │   ├── notifications/  # Thông báo
│   │   ├── settings/       # Cài đặt
│   │   ├── profile/        # Hồ sơ
│   │   └── admin/          # Quản trị
│   └── layout/             # Shell + navigation
│       ├── shell.ts        # Main layout wrapper
│       └── mobile-tab-bar/ # Tab bar cho mobile
├── environments/           # env config (dev/prod)
├── styles.scss             # Global styles + CSS variables
├── index.html              # Root HTML
└── main.ts                 # Bootstrap
```

---

## 2. Routing

File: `app.routes.ts`

```typescript
export const routes: Routes = [
  // Public routes (không cần auth)
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'forgot-password', component: ForgotPassword },
  { path: 'reset-password', component: ResetPassword },
  { path: 'confirm-email', component: ConfirmEmail },

  // Protected routes (cần JWT)
  {
    path: 'app',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'today', component: Today },
      { path: 'finance', component: Finance },
      { path: 'health', component: Health },
      { path: 'work', component: Work },
      { path: 'calendar', component: Calendar },
      { path: 'journal', component: Journal },
      { path: 'knowledge', component: Knowledge },
      { path: 'career', component: Career },
      { path: 'documents', component: Documents },
      { path: 'stats', component: StatsPage },
      { path: 'insights', component: Insights },
      { path: 'search', component: Search },
      { path: 'notifications', component: Notifications },
      { path: 'settings', component: Settings },
      { path: 'profile', component: Profile },
      { path: 'admin', component: AdminUsers, canActivate: [adminGuard] },
      { path: '', redirectTo: 'today', pathMatch: 'full' },
    ],
  },
  { path: '', redirectTo: 'app', pathMatch: 'full' },
  { path: '**', redirectTo: 'app' },
];
```

### Route Guards

| Guard | Điều kiện | Redirect |
|-------|-----------|---------|
| `authGuard` | accessToken trong localStorage | `/login` |
| `adminGuard` | Role "Admin" trong token | `/app/today` |

---

## 3. Core Services

### AuthService (`core/services/auth.service.ts`)

```typescript
// Quản lý toàn bộ auth flow
login(email, password): Observable<LoginResponse>
loginTwoFactor(twoFactorToken, code): Observable<AuthResponse>
googleLogin(idToken): Observable<AuthResponse>
register(email, password, displayName): Observable<AuthResponse>
logout(): void
refresh(): Observable<AuthResponse>
isLoggedIn(): boolean
getToken(): string | null
getUserId(): string | null
```

**Token storage**: `localStorage` với keys `accessToken`, `refreshToken`, `userId`.

**Auto-refresh**: HTTP interceptor tự động refresh token khi nhận 401.

---

### HTTP Interceptor (`core/interceptors/`)

```typescript
// Tự động thêm Bearer token vào header
// Tự động retry với refresh token khi nhận 401
// Redirect về /login khi refresh thất bại
```

---

### ThemeService (`core/services/theme.service.ts`)

```typescript
// Dark mode toggle
// Lưu preference vào localStorage
// Áp CSS class 'dark' lên document.body
```

---

### PlatformService (`core/services/platform.service.ts`)

```typescript
// Detect mobile vs web
// Sử dụng Capacitor Device API
isMobile(): boolean
isAndroid(): boolean
isIos(): boolean
```

---

### HapticsService (`core/services/haptics.service.ts`)

```typescript
// Trigger native haptic feedback trên mobile
// No-op trên web
impact(style: 'light'|'medium'|'heavy'): Promise<void>
notification(type: 'success'|'warning'|'error'): Promise<void>
```

---

### SyncService (`core/services/sync.service.ts`)

```typescript
// Offline support: queue requests khi offline
// Replay queue khi online trở lại
// Dùng localStorage làm offline store
```

---

### Feature Services

| Service | File | Domain |
|---------|------|--------|
| `FinanceService` | `finance.service.ts` | Transactions, categories, assets, net worth |
| `HealthService` | `health.service.ts` | Health logs, activities, BMI |
| `WorkService` | `work.service.ts` | Tasks, goals, habits, Kanban |
| `JournalService` | `journal.service.ts` | Journal entries |
| `EventService` | `event.service.ts` | Calendar events |
| `NoteService` | `note.service.ts` | Knowledge notes |
| `CareerService` | `career.service.ts` | Skills, certs, projects |
| `DocumentService` | `document.service.ts` | File upload/download |
| `NotificationService` | `notification.service.ts` | In-app notifications |
| `SearchService` | `search.service.ts` | Global search |
| `StatsService` | `stats.service.ts` | Statistics |
| `AiService` | `ai.service.ts` | AI insights |
| `UsersService` | `users.service.ts` | Profile, avatar, 2FA |
| `AdminService` | `admin.service.ts` | User management |
| `PushNotificationService` | `push-notification.service.ts` | Native push (Capacitor) |

---

## 4. Angular 20 — Standalone Components

Toàn bộ component dùng **Standalone** (không có NgModule):

```typescript
@Component({
  selector: 'app-finance',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './finance.html',
  styleUrl: './finance.scss',
})
export class Finance {
  // Dùng inject() thay constructor injection (Angular 16+ best practice)
  private financeService = inject(FinanceService);

  // Signals cho reactive state
  transactions = signal<Transaction[]>([]);
  isLoading = signal(false);

  // Computed values
  total = computed(() => this.transactions().reduce(...));
}
```

---

## 5. API Configuration

File: `core/api.config.ts`

```typescript
export const API_CONFIG = {
  // Dev: full URL; Prod: '' (same origin, nginx proxies /api)
  baseUrl: environment.apiOrigin + '/api',
  googleClientId: environment.googleClientId,  // '' = ẩn nút Google
};
```

File: `environments/environment.ts` (dev):
```typescript
export const environment = {
  production: false,
  apiOrigin: 'http://localhost:5149',
  googleClientId: '',
};
```

File: `environments/environment.prod.ts` (prod):
```typescript
export const environment = {
  production: true,
  apiOrigin: '',  // same origin (nginx)
  googleClientId: '',
};
```

---

## 6. Layout & Navigation

### Shell Component (`layout/shell.ts`)

- Wrapper layout cho toàn bộ authenticated routes.
- Sidebar navigation (desktop) + Mobile tab bar (mobile).
- Hiển thị avatar + tên user.
- Toggle dark mode.

### Mobile Tab Bar (`layout/mobile-tab-bar/`)

- Hiển thị ở bottom trên mobile (Capacitor).
- Các tab chính: Today, Finance, Health, Work, Journal.
- Ẩn trên desktop.

---

## 7. Shared Components

| Component | Mô tả |
|-----------|-------|
| `DateNavigator` | Chọn ngày (trái/phải + date picker) — dùng trong Today, Finance, Health |

---

## 8. Styling System

File: `styles.scss` (global)

**CSS Custom Properties (design tokens):**

```scss
:root {
  // Colors
  --color-primary: #4f46e5;
  --color-bg: #0f141b;
  --color-surface: #1a2230;
  --color-text: #e2e8f0;
  --color-muted: #94a3b8;

  // Spacing
  --space-xs: 4px;
  --space-sm: 8px;
  --space-md: 16px;
  --space-lg: 24px;
  --space-xl: 32px;

  // Border radius
  --radius-sm: 6px;
  --radius-md: 12px;
  --radius-lg: 20px;
}

// Dark mode: đổi CSS variables (không đổi class)
.dark { ... }
```

---

## 9. PWA (Progressive Web App)

Cấu hình trong `angular.json`:

- Service worker: `@angular/service-worker`
- Manifest: `public/manifest.webmanifest`
- Offline: cache assets + API responses

---

## 10. Rich Text Editor (Journal)

Component custom trong `features/journal/`:

- **Không dùng thư viện ngoài** — editor tự xây dựng dùng `contenteditable` + `execCommand`.
- Hỗ trợ: **đậm**, *nghiêng*, tiêu đề (H1/H2/H3), danh sách (ol/ul), trích dẫn, link.
- Output: HTML string lưu vào `JournalEntry.Content`.
- Test: 5 unit tests (Karma).

---

## 11. Build & Output

```bash
npm run build
# Output: dist/frontend/browser/
# Files: main-*.js (~505KB, ~119KB gzip), polyfills, styles
```

**Bundle size (approximate):**

| File | Size | Gzip |
|------|------|------|
| `main-*.js` | 505 KB | 119 KB |
| `polyfills-*.js` | 35 KB | 11 KB |
| `styles-*.css` | 6 KB | 2 KB |

---

## 12. Test Coverage

```bash
npm run test:ci  # Karma + ChromeHeadless (không cần user input)
```

**29 tests:**
- `AuthService` — login, logout, token management
- `FinanceService` — CRUD operations mock
- `SearchService` — search query
- `ThemeService` — dark mode toggle
- `RichEditor` — formatting commands
- `Login` component — form validation
- `Today` component — render
