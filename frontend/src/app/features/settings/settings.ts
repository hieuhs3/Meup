import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { API_BASE } from '../../core/api.config';
import { ThemeService } from '../../core/services/theme.service';
import { AiService, AiStatus } from '../../core/services/ai.service';

@Component({
  selector: 'app-settings',
  imports: [FormsModule],
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
      <h3>API key AI (Claude)</h3>
      <p class="muted">
        Nhập API key Anthropic của riêng bạn để dùng tính năng Gợi ý AI. Key được mã hóa và lưu an toàn,
        không hiển thị lại. Lấy key tại <code>console.anthropic.com</code>.
      </p>

      @if (status(); as s) {
        @if (s.hasUserKey) {
          <p class="success">✓ Bạn đã đặt API key riêng.</p>
        } @else if (s.usingServerKey) {
          <p class="muted">Đang dùng key chung của hệ thống. Bạn có thể đặt key riêng bên dưới.</p>
        } @else {
          <p class="muted">Chưa có API key — tính năng AI đang tắt.</p>
        }
      }

      <label for="ai-key">API key
        <input id="ai-key" type="password" [(ngModel)]="apiKey" name="aiKey"
               placeholder="sk-ant-..." autocomplete="off" />
      </label>
      @if (aiMsg()) { <p [class]="aiOk() ? 'success' : 'error'">{{ aiMsg() }}</p> }
      <div class="ai-actions">
        <button (click)="saveKey()" [disabled]="aiBusy() || !apiKey.trim()">
          {{ aiBusy() ? 'Đang lưu…' : 'Lưu key' }}
        </button>
        @if (status()?.hasUserKey) {
          <button class="ghost" (click)="clearKey()" [disabled]="aiBusy()">Xóa key</button>
        }
      </div>
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
  styles: [`
    .ai-actions { display: flex; gap: .6rem; flex-wrap: wrap; }
  `],
})
export class Settings implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly ai = inject(AiService);
  readonly theme = inject(ThemeService);

  readonly busy = signal(false);
  readonly msg = signal<string | null>(null);

  // --- API key AI ---
  apiKey = '';
  readonly status = signal<AiStatus | null>(null);
  readonly aiBusy = signal(false);
  readonly aiMsg = signal<string | null>(null);
  readonly aiOk = signal(false);

  ngOnInit(): void {
    this.ai.status().subscribe({ next: (s) => this.status.set(s) });
  }

  saveKey(): void {
    const key = this.apiKey.trim();
    if (!key) return;
    this.aiBusy.set(true);
    this.aiMsg.set(null);
    this.ai.setKey(key).subscribe({
      next: (s) => {
        this.status.set(s);
        this.apiKey = '';
        this.aiBusy.set(false);
        this.aiOk.set(true);
        this.aiMsg.set('Đã lưu API key. Tính năng AI đã sẵn sàng.');
      },
      error: () => {
        this.aiBusy.set(false);
        this.aiOk.set(false);
        this.aiMsg.set('Lưu key thất bại. Vui lòng thử lại.');
      },
    });
  }

  clearKey(): void {
    if (!confirm('Xóa API key của bạn? Tính năng AI sẽ tắt (trừ khi hệ thống có key chung).')) return;
    this.aiBusy.set(true);
    this.aiMsg.set(null);
    this.ai.clearKey().subscribe({
      next: (s) => {
        this.status.set(s);
        this.aiBusy.set(false);
        this.aiOk.set(true);
        this.aiMsg.set('Đã xóa API key.');
      },
      error: () => {
        this.aiBusy.set(false);
        this.aiOk.set(false);
        this.aiMsg.set('Xóa key thất bại.');
      },
    });
  }

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
