import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { API_BASE } from '../../core/api.config';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-settings',
  template: `
    <header class="page-head"><h1>Cài đặt</h1></header>

    <section class="card">
      <h3>Giao diện</h3>
      <p class="muted">Chế độ hiện tại: <strong>{{ theme.theme() === 'dark' ? 'Tối' : 'Sáng' }}</strong></p>
      <button (click)="theme.toggle()">
        {{ theme.theme() === 'dark' ? '☀ Chuyển sang Sáng' : '🌙 Chuyển sang Tối' }}
      </button>
    </section>

    <section class="card">
      <h3>Dữ liệu</h3>
      <p class="muted">Tải về toàn bộ dữ liệu của bạn (giao dịch, sức khỏe, công việc, lịch, nhật ký, ghi chú…) dưới dạng JSON.</p>
      @if (msg()) { <p class="success">{{ msg() }}</p> }
      <button (click)="exportData()" [disabled]="busy()">{{ busy() ? 'Đang xuất…' : '⬇ Xuất dữ liệu (JSON)' }}</button>
    </section>

    <section class="card">
      <h3>Ngôn ngữ</h3>
      <p class="muted">Tiếng Việt (đa ngôn ngữ vi/en sẽ bổ sung sau).</p>
    </section>
  `,
})
export class Settings {
  private readonly http = inject(HttpClient);
  readonly theme = inject(ThemeService);

  readonly busy = signal(false);
  readonly msg = signal<string | null>(null);

  exportData(): void {
    this.busy.set(true);
    this.msg.set(null);
    this.http.get(`${API_BASE}/export`).subscribe({
      next: (data) => {
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'meup-export.json';
        a.click();
        URL.revokeObjectURL(url);
        this.busy.set(false);
        this.msg.set('Đã tải file meup-export.json.');
      },
      error: () => {
        this.busy.set(false);
        this.msg.set('Xuất dữ liệu thất bại.');
      },
    });
  }
}
