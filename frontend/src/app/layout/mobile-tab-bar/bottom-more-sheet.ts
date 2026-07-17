import { Component, output, input } from '@angular/core';
import { RouterLink } from '@angular/router';

interface MenuItem {
  icon: string;
  label: string;
  route: string;
}

const MORE_ITEMS: MenuItem[] = [
  { icon: '📅', label: 'Lịch trình', route: '/app/calendar' },
  { icon: '📓', label: 'Nhật ký', route: '/app/journal' },
  { icon: '🧠', label: 'Kiến thức', route: '/app/knowledge' },
  { icon: '💼', label: 'Sự nghiệp', route: '/app/career' },
  { icon: '📁', label: 'Tài liệu', route: '/app/documents' },
  { icon: '📊', label: 'Thống kê', route: '/app/stats' },
  { icon: '✨', label: 'Gợi ý AI', route: '/app/insights' },
  { icon: '🔍', label: 'Tìm kiếm', route: '/app/search' },
  { icon: '🔔', label: 'Thông báo', route: '/app/notifications' },
  { icon: '👤', label: 'Hồ sơ', route: '/app/profile' },
  { icon: '🎛', label: 'Cài đặt', route: '/app/settings' },
];

/**
 * BottomMoreSheet — Sheet trượt từ dưới lên, hiển thị các menu
 * không nằm trên tab bar chính (calendar, journal, knowledge, …).
 */
@Component({
  selector: 'app-bottom-more-sheet',
  imports: [RouterLink],
  template: `
    <!-- Backdrop: tap để đóng -->
    <div class="backdrop" (click)="closed.emit()" aria-hidden="true"></div>

    <div class="sheet" role="dialog" aria-modal="true" aria-label="Menu điều hướng">
      <!-- Drag handle -->
      <div class="handle" aria-hidden="true"></div>

      <h2 class="sheet-title">Menu</h2>

      <div class="menu-grid">
        @for (item of items; track item.route) {
          <a
            class="menu-item"
            [routerLink]="item.route"
            (click)="closed.emit()"
          >
            <span class="menu-icon">{{ item.icon }}</span>
            <span class="menu-label">{{ item.label }}</span>
          </a>
        }

        @if (isAdmin()) {
          <a class="menu-item admin" routerLink="/app/admin" (click)="closed.emit()">
            <span class="menu-icon">⚙</span>
            <span class="menu-label">Quản trị</span>
          </a>
        }
      </div>
    </div>
  `,
  styles: [`
    .backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.5);
      backdrop-filter: blur(4px);
      -webkit-backdrop-filter: blur(4px);
      z-index: 110;
      animation: fadeIn 0.2s ease;
    }

    .sheet {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 120;
      background: var(--surface);
      border-radius: 20px 20px 0 0;
      padding: 12px 16px calc(16px + env(safe-area-inset-bottom, 0px));
      box-shadow: 0 -8px 40px rgba(0, 0, 0, 0.3);
      animation: slideUp 0.3s cubic-bezier(0.32, 0.72, 0, 1);
      border-top: 1px solid var(--border);
    }

    .handle {
      width: 40px;
      height: 4px;
      background: var(--border);
      border-radius: 2px;
      margin: 0 auto 16px;
    }

    .sheet-title {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--muted);
      text-transform: uppercase;
      letter-spacing: 0.08em;
      margin: 0 4px 12px;
    }

    .menu-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 8px;
    }

    .menu-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 6px;
      padding: 12px 8px;
      border-radius: 12px;
      text-decoration: none;
      color: var(--text);
      background: var(--bg);
      transition: background 0.15s, transform 0.1s;
      min-height: 72px;
      justify-content: center;
    }

    .menu-item:active {
      transform: scale(0.95);
      background: var(--border);
    }

    .menu-icon {
      font-size: 1.5rem;
      line-height: 1;
    }

    .menu-label {
      font-size: 0.7rem;
      font-weight: 500;
      text-align: center;
      line-height: 1.2;
    }

    .menu-item.admin {
      color: var(--danger);
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to   { opacity: 1; }
    }

    @keyframes slideUp {
      from { transform: translateY(100%); }
      to   { transform: translateY(0); }
    }
  `],
})
export class BottomMoreSheet {
  readonly isAdmin = input<boolean>(false);
  readonly closed = output<void>();
  readonly items = MORE_ITEMS;
}
