import { Component, input, output, computed } from '@angular/core';

/**
 * DateNavigator — Thanh điều hướng ngày mobile-friendly.
 * Hiển thị: ← Thứ 3, 15/7/2026 → với nút "Hôm nay" khi không phải ngày hiện tại.
 */
@Component({
  selector: 'app-date-navigator',
  template: `
    <div class="date-nav">
      <button class="nav-btn" (click)="prev.emit()" aria-label="Ngày trước">◀</button>

      <div class="date-center">
        <span class="date-label">{{ label() }}</span>
        @if (!isToday()) {
          <button class="today-chip" (click)="goToday.emit()">Về hôm nay</button>
        }
      </div>

      <button class="nav-btn" (click)="next.emit()" aria-label="Ngày sau">▶</button>
    </div>
  `,
  styles: [`
    .date-nav {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding: 8px 0 12px;
    }

    .nav-btn {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: var(--surface);
      border: 1px solid var(--border);
      color: var(--text);
      font-size: 0.8rem;
      display: flex;
      align-items: center;
      justify-content: center;
      flex: 0 0 auto;
      padding: 0;
      transition: background 0.15s, transform 0.1s;
    }
    .nav-btn:active { transform: scale(0.92); }

    .date-center {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
    }

    .date-label {
      font-size: 1rem;
      font-weight: 600;
      color: var(--text);
    }

    .today-chip {
      background: var(--primary);
      color: #fff;
      border: none;
      border-radius: 999px;
      font-size: 0.7rem;
      padding: 2px 10px;
      height: auto;
      line-height: 1.6;
      opacity: 0.9;
    }
    .today-chip:hover { opacity: 1; }
  `],
})
export class DateNavigator {
  readonly label = input.required<string>();
  readonly isToday = input<boolean>(true);

  readonly prev = output<void>();
  readonly next = output<void>();
  readonly goToday = output<void>();
}
