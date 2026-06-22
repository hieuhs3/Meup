import { Component, OnInit, inject, signal } from '@angular/core';
import { AiService, WeeklyInsight } from '../../core/services/ai.service';

@Component({
  selector: 'app-insights',
  template: `
    <header class="page-head">
      <h1>Gợi ý AI</h1>
      <p class="muted">Tổng kết & gợi ý từ dữ liệu của bạn (dùng Claude).</p>
    </header>

    @if (enabled() === false) {
      <section class="card">
        <p class="muted">Tính năng AI chưa được bật. Quản trị viên cần cấu hình <code>Ai:ApiKey</code>
          (ANTHROPIC_API_KEY) trong cấu hình máy chủ.</p>
      </section>
    } @else {
      <section class="card">
        <button (click)="generate(false)" [disabled]="busy()">
          {{ busy() ? 'Đang tạo…' : '✨ Tạo tổng kết tuần' }}
        </button>
        @if (insight(); as i) {
          <p class="muted" style="margin-top:1rem">
            Tuần {{ i.from }} – {{ i.to }}
            <button class="link" (click)="generate(true)" [disabled]="busy()">Tạo lại</button>
          </p>
          <div class="summary">{{ i.summary }}</div>
        }
      </section>
    }
  `,
  styles: [`
    .summary { white-space: pre-wrap; line-height: 1.6; margin-top: .5rem; }
  `],
})
export class Insights implements OnInit {
  private readonly ai = inject(AiService);

  readonly enabled = signal<boolean | null>(null);
  readonly busy = signal(false);
  readonly insight = signal<WeeklyInsight | null>(null);

  ngOnInit(): void {
    this.ai.status().subscribe({
      next: (s) => this.enabled.set(s.enabled),
      error: () => this.enabled.set(false),
    });
  }

  generate(refresh = false): void {
    this.busy.set(true);
    this.ai.weeklyInsight(undefined, refresh).subscribe({
      next: (i) => { this.insight.set(i); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }
}
