import { Component, inject, signal, output } from '@angular/core';
import { Router } from '@angular/router';

interface QuickAction {
  icon: string;
  label: string;
  action: () => void;
  color: string;
}

/**
 * QuickAddFab — Floating Action Button mở mini-menu thêm nhanh:
 * - Giao dịch (→ Finance)
 * - Nhật ký sức khỏe (→ Health)
 * - Task mới (→ Work)
 * - Nhật ký (→ Journal)
 */
@Component({
  selector: 'app-quick-add-fab',
  template: `
    <!-- Backdrop -->
    @if (open()) {
      <div class="fab-backdrop" (click)="open.set(false)" aria-hidden="true"></div>
    }

    <!-- Mini action buttons (hiện khi open) -->
    @if (open()) {
      <div class="fab-actions" role="menu">
        @for (action of actions; track action.label) {
          <button
            class="fab-action"
            [style.--action-color]="action.color"
            (click)="trigger(action)"
            role="menuitem"
          >
            <span class="fab-action-icon">{{ action.icon }}</span>
            <span class="fab-action-label">{{ action.label }}</span>
          </button>
        }
      </div>
    }

    <!-- FAB chính -->
    <button
      class="fab"
      [class.open]="open()"
      (click)="toggleOpen()"
      [attr.aria-expanded]="open()"
      aria-label="Thêm nhanh"
    >
      <span class="fab-icon">{{ open() ? '✕' : '+' }}</span>
    </button>
  `,
  styles: [`
    :host {
      position: fixed;
      bottom: calc(56px + env(safe-area-inset-bottom, 0px) + 16px);
      right: 16px;
      z-index: 90;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 12px;
    }

    .fab-backdrop {
      position: fixed;
      inset: 0;
      z-index: -1;
    }

    .fab {
      width: 56px;
      height: 56px;
      border-radius: 50%;
      background: linear-gradient(135deg, #5b7cfa, #7c5bfa);
      color: #fff;
      border: none;
      box-shadow: 0 6px 20px rgba(91, 124, 250, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      transition: transform 0.25s cubic-bezier(0.34, 1.56, 0.64, 1), box-shadow 0.2s;
      flex: 0 0 auto;
    }

    .fab:active { transform: scale(0.92); }
    .fab.open { transform: rotate(45deg); box-shadow: 0 4px 12px rgba(91, 124, 250, 0.4); }

    .fab-icon {
      font-size: 1.5rem;
      line-height: 1;
      transition: transform 0.2s;
    }

    /* Action buttons */
    .fab-actions {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 8px;
      animation: fabActionsIn 0.2s ease;
    }

    .fab-action {
      display: flex;
      align-items: center;
      gap: 10px;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 28px;
      padding: 10px 16px 10px 12px;
      color: var(--text);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
      white-space: nowrap;
      transition: transform 0.15s, background 0.15s;
    }

    .fab-action:hover { background: var(--bg); transform: translateX(-2px); }
    .fab-action:active { transform: scale(0.96); }

    .fab-action-icon {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: var(--action-color, #eef1fb);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
      flex: 0 0 auto;
    }

    .fab-action-label {
      font-size: 0.9rem;
      font-weight: 500;
    }

    @keyframes fabActionsIn {
      from { opacity: 0; transform: translateY(10px) scale(0.95); }
      to   { opacity: 1; transform: translateY(0) scale(1); }
    }
  `],
})
export class QuickAddFab {
  private readonly router = inject(Router);
  readonly open = signal(false);

  /** Emit để parent component biết → có thể mở form inline */
  readonly addTransaction = output<void>();

  readonly actions: QuickAction[] = [
    {
      icon: '💰',
      label: 'Giao dịch',
      color: 'rgba(52, 196, 111, 0.2)',
      action: () => { this.open.set(false); this.router.navigate(['/app/finance']); },
    },
    {
      icon: '♥',
      label: 'Sức khỏe',
      color: 'rgba(239, 90, 110, 0.15)',
      action: () => { this.open.set(false); this.router.navigate(['/app/health']); },
    },
    {
      icon: '✓',
      label: 'Task mới',
      color: 'rgba(91, 124, 250, 0.15)',
      action: () => { this.open.set(false); this.router.navigate(['/app/work']); },
    },
    {
      icon: '📓',
      label: 'Nhật ký',
      color: 'rgba(250, 188, 91, 0.2)',
      action: () => { this.open.set(false); this.router.navigate(['/app/journal']); },
    },
  ];

  toggleOpen(): void {
    this.open.set(!this.open());
  }

  trigger(action: QuickAction): void {
    action.action();
  }
}
