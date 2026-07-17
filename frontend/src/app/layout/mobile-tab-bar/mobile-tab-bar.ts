import { Component, inject, signal, effect } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { BottomMoreSheet } from './bottom-more-sheet';

interface Tab {
  icon: string;
  label: string;
  route: string;
}

/** 5 tab chính hiển thị trên Bottom Tab Bar */
const MAIN_TABS: Tab[] = [
  { icon: '⊞', label: 'Hôm nay', route: '/app/today' },
  { icon: '💰', label: 'Tài chính', route: '/app/finance' },
  { icon: '♥', label: 'Sức khỏe', route: '/app/health' },
  { icon: '✓', label: 'Việc làm', route: '/app/work' },
];

/**
 * MobileTabBar — Bottom Navigation Bar cho mobile (Capacitor + web nhỏ).
 * Hiển thị 4 tab chính + nút "Thêm" mở BottomMoreSheet.
 */
@Component({
  selector: 'app-mobile-tab-bar',
  imports: [RouterLink, RouterLinkActive, BottomMoreSheet],
  template: `
    <nav class="tab-bar" role="navigation" aria-label="Điều hướng chính">
      @for (tab of tabs; track tab.route) {
        <a
          class="tab-item"
          [routerLink]="tab.route"
          routerLinkActive="active"
          [attr.aria-label]="tab.label"
        >
          <span class="tab-icon">{{ tab.icon }}</span>
          <span class="tab-label">{{ tab.label }}</span>
        </a>
      }

      <!-- Nút "Thêm" → mở sheet danh sách menu còn lại -->
      <button
        class="tab-item more-btn"
        [class.active]="moreActive()"
        (click)="toggleMore()"
        aria-label="Thêm"
        aria-haspopup="dialog"
        [attr.aria-expanded]="showMore()"
      >
        <span class="tab-icon">⋯</span>
        <span class="tab-label">Thêm</span>
        @if (notify.unread() > 0) {
          <span class="notif-dot" aria-label="{{ notify.unread() }} thông báo chưa đọc"></span>
        }
      </button>
    </nav>

    <!-- Sheet "Thêm" -->
    @if (showMore()) {
      <app-bottom-more-sheet
        [isAdmin]="auth.isAdmin()"
        (closed)="showMore.set(false)"
      />
    }
  `,
  styles: [`
    :host {
      display: block;
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 100;
    }

    .tab-bar {
      display: flex;
      align-items: stretch;
      background: var(--surface);
      border-top: 1px solid var(--border);
      /* Safe area padding (notch / home indicator) */
      padding-bottom: env(safe-area-inset-bottom, 0px);
      height: calc(56px + env(safe-area-inset-bottom, 0px));
      box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.15);
    }

    .tab-item {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 2px;
      text-decoration: none;
      color: var(--muted);
      background: transparent;
      border: none;
      padding: 6px 4px;
      cursor: pointer;
      font-family: inherit;
      position: relative;
      transition: color 0.2s ease;
      /* Đủ lớn để chạm: min 44px */
      min-width: 44px;
      min-height: 44px;
    }

    .tab-item:hover {
      color: var(--text);
    }

    .tab-item.active {
      color: var(--primary);
    }

    /* Glow indicator trên tab active */
    .tab-item.active::before {
      content: '';
      position: absolute;
      top: 0;
      left: 50%;
      transform: translateX(-50%);
      width: 32px;
      height: 3px;
      background: var(--primary);
      border-radius: 0 0 4px 4px;
    }

    .tab-icon {
      font-size: 1.3rem;
      line-height: 1;
      transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
    }

    .tab-item.active .tab-icon {
      transform: scale(1.15) translateY(-1px);
    }

    .tab-label {
      font-size: 0.65rem;
      font-weight: 500;
      letter-spacing: 0.01em;
      white-space: nowrap;
    }

    .notif-dot {
      position: absolute;
      top: 6px;
      right: calc(50% - 14px);
      width: 8px;
      height: 8px;
      background: var(--danger);
      border-radius: 50%;
      border: 2px solid var(--surface);
    }

    .more-btn {
      /* override button styles từ global */
      background: transparent !important;
      color: var(--muted) !important;
      border-radius: 0 !important;
      padding: 6px 4px !important;
      font-size: inherit !important;
    }

    .more-btn.active {
      color: var(--primary) !important;
    }

    .more-btn.active::before {
      content: '';
      position: absolute;
      top: 0;
      left: 50%;
      transform: translateX(-50%);
      width: 32px;
      height: 3px;
      background: var(--primary);
      border-radius: 0 0 4px 4px;
    }
  `],
})
export class MobileTabBar {
  readonly tabs = MAIN_TABS;
  readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly showMore = signal(false);

  /** "Thêm" được coi là active khi route hiện tại không thuộc 4 tab chính */
  readonly moreActive = signal(false);

  constructor() {
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => {
        const url = this.router.url;
        const isMainTab = MAIN_TABS.some(t => url.startsWith(t.route));
        this.moreActive.set(!isMainTab);
        // Đóng sheet khi navigate
        this.showMore.set(false);
      });
  }

  toggleMore(): void {
    this.showMore.update(v => !v);
  }
}
