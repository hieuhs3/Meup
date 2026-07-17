import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { FinanceService } from '../../core/services/finance.service';
import { HealthService } from '../../core/services/health.service';
import { WorkService } from '../../core/services/work.service';
import { EventService } from '../../core/services/event.service';
import { PlatformService } from '../../core/services/platform.service';
import { Summary } from '../../core/models/finance.models';
import { HealthLog } from '../../core/models/health.models';
import { WorkSummary } from '../../core/models/work.models';
import { CalendarEvent } from '../../core/models/event.models';
import { MoneyPipe } from '../../core/pipes/money.pipe';
import { DateNavigator } from '../../core/components/date-navigator';
import { QuickAddFab } from '../../core/components/quick-add-fab';

/**
 * F6 — Màn hình trung tâm "Hôm nay": gom Tài chính / Sức khỏe / Công việc
 * theo một ngày, cho phép đi lùi/tới ngày.
 */
@Component({
  selector: 'app-today',
  imports: [RouterLink, MoneyPipe, DateNavigator, QuickAddFab],
  templateUrl: './today.html',
  styles: [`
    /* Desktop: date-bar ngang */
    .date-bar { display: flex; align-items: center; gap: .6rem; margin-bottom: 1.25rem; flex-wrap: wrap; }
    .date-bar input { width: auto; margin: 0; }
    .date-bar .nav { padding: .45rem .7rem; }
    .date-bar .today-btn { background: transparent; color: var(--primary); border: 1px solid var(--border); }
    .day-label { font-weight: 600; }

    /* Cards dùng chung desktop + mobile */
    .card.tile { display: block; text-decoration: none; color: inherit; transition: box-shadow .15s, transform .15s; }
    .card.tile:hover { box-shadow: 0 6px 20px rgba(31,39,51,.08); transform: translateY(-1px); }
    .big { font-size: 1.4rem; font-weight: 700; margin: .2rem 0; }
    .income { color: var(--success); }
    .expense { color: var(--danger); }
    .hint { color: var(--muted); font-size: .88rem; }
    .hint a { color: var(--primary); }
    .pill { display:inline-block; background:#eef1fb; color:var(--primary); border-radius:999px; padding:.1rem .5rem; font-size:.78rem; margin-left:.3rem; }

    /* Mobile: summary row 3 pill cards */
    .summary-row {
      display: flex;
      gap: 10px;
      overflow-x: auto;
      padding-bottom: 4px;
      margin-bottom: 16px;
      scrollbar-width: none;
    }
    .summary-row::-webkit-scrollbar { display: none; }
    .sum-pill {
      flex: 0 0 auto;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 14px;
      padding: 10px 14px;
      min-width: 110px;
    }
    .sum-pill .s-label { font-size: .7rem; color: var(--muted); }
    .sum-pill .s-value { font-size: 1rem; font-weight: 700; margin-top: 2px; }

    /* Mobile: metric pills cho Health */
    .metric-row {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .metric-chip {
      display: flex;
      align-items: center;
      gap: 5px;
      background: var(--bg);
      border-radius: 999px;
      padding: 5px 10px;
      font-size: .82rem;
    }
    .metric-chip .mc-icon { font-size: .9rem; }
  `],
})
export class Today implements OnInit {
  readonly auth = inject(AuthService);
  readonly platform = inject(PlatformService);
  private readonly financeSvc = inject(FinanceService);
  private readonly healthSvc = inject(HealthService);
  private readonly workSvc = inject(WorkService);
  private readonly eventSvc = inject(EventService);

  readonly date = signal(this.todayIso());
  readonly finance = signal<Summary | null>(null);
  readonly health = signal<HealthLog | null>(null);
  readonly work = signal<WorkSummary | null>(null);
  readonly events = signal<CalendarEvent[] | null>(null);

  readonly isToday = computed(() => this.date() === this.todayIso());
  readonly dateLabel = computed(() => this.formatLabel(this.date()));

  ngOnInit(): void {
    this.reload();
  }

  // --- Điều hướng ngày ---

  shiftDay(days: number): void {
    const d = this.parse(this.date());
    d.setDate(d.getDate() + days);
    this.date.set(this.iso(d));
    this.reload();
  }

  pickDate(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.reload();
  }

  goToday(): void {
    this.date.set(this.todayIso());
    this.reload();
  }

  private reload(): void {
    const d = this.date();
    this.financeSvc.getSummary(d).subscribe({ next: (s) => this.finance.set(s), error: () => this.finance.set(null) });
    this.healthSvc.getSummary(d).subscribe({ next: (s) => this.health.set(s.today), error: () => this.health.set(null) });
    this.workSvc.getSummary(d).subscribe({ next: (w) => this.work.set(w), error: () => this.work.set(null) });
    this.eventSvc.list(d, d).subscribe({ next: (e) => this.events.set(e), error: () => this.events.set(null) });
  }

  // --- Tiện ích ---


  hm(t: string | null): string {
    return t ? t.slice(0, 5) : '';
  }

  private parse(iso: string): Date {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private iso(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private todayIso(): string {
    return this.iso(new Date());
  }

  private formatLabel(iso: string): string {
    const days = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
    const d = this.parse(iso);
    return `${days[d.getDay()]}, ${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
  }
}
