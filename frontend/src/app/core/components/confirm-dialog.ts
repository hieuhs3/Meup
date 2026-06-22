import { Component, inject } from '@angular/core';
import { ConfirmService } from '../services/confirm.service';

/**
 * Hiển thị hộp thoại xác nhận khi ConfirmService có yêu cầu. Đặt MỘT lần ở shell.
 */
@Component({
  selector: 'app-confirm-dialog',
  template: `
    @if (svc.state(); as s) {
      <div class="cd-backdrop" (click)="svc.answer(false)">
        <div class="cd-box" role="dialog" aria-modal="true" aria-labelledby="cd-msg"
             (click)="$event.stopPropagation()">
          <p class="cd-msg" id="cd-msg">{{ s.message }}</p>
          <div class="cd-actions">
            <button class="ghost" (click)="svc.answer(false)">Hủy</button>
            <button class="cd-confirm" (click)="svc.answer(true)" autofocus>{{ s.confirmText }}</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .cd-backdrop {
      position: fixed; inset: 0; background: rgba(15, 20, 27, .45);
      display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 1rem;
    }
    .cd-box {
      background: var(--surface); border: 1px solid var(--border); border-radius: 12px;
      padding: 1.5rem; max-width: 420px; width: 100%;
      box-shadow: 0 12px 40px rgba(15, 20, 27, .25);
    }
    .cd-msg { margin: 0 0 1.25rem; color: var(--text); line-height: 1.5; }
    .cd-actions { display: flex; justify-content: flex-end; gap: .6rem; }
    .cd-confirm { background: var(--danger); }
    .cd-confirm:hover:not(:disabled) { background: #c42f42; }
  `],
})
export class ConfirmDialog {
  readonly svc = inject(ConfirmService);
}
