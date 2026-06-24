import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { StatsService } from '../../core/services/stats.service';
import { Stats } from '../../core/models/stats.models';
import { MoneyPipe } from '../../core/pipes/money.pipe';

@Component({
  selector: 'app-stats',
  imports: [ReactiveFormsModule, MoneyPipe],
  templateUrl: './stats.html',
  styles: [`
    .bar-row { display: flex; align-items: center; gap: .6rem; margin: .35rem 0; }
    .bar-row .label { width: 130px; font-size: .85rem; }
    .bar-row .track { flex: 1; background: #eef1fb; border-radius: 6px; height: 16px; overflow: hidden; }
    .bar-row .track > span { display: block; height: 100%; }
    .bar-row .val { width: 120px; text-align: right; font-size: .82rem; white-space: nowrap; }
    .sum-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: .8rem; margin-bottom: 1rem; }
    .sum { border: 1px solid var(--border); border-radius: 10px; padding: .8rem 1rem; }
    .sum .label { color: var(--muted); font-size: .8rem; }
    .sum .value { font-size: 1.25rem; font-weight: 700; margin-top: .2rem; }
    .income { color: var(--success); } .expense { color: var(--danger); }
    .spark { display: flex; align-items: flex-end; gap: 3px; height: 80px; margin-top: .5rem; }
    .spark > span { flex: 1; background: var(--primary); border-radius: 3px 3px 0 0; min-height: 2px; }
    .range { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1rem; }
    .range label { margin-bottom: 0; }
  `],
})
export class StatsPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly statsSvc = inject(StatsService);

  readonly data = signal<Stats | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly form = this.fb.nonNullable.group({
    from: [this.monthStart()],
    to: [this.todayIso()],
  });

  // Giá trị lớn nhất để chuẩn hóa độ rộng thanh danh mục.
  readonly maxCat = computed(() => {
    const cats = this.data()?.finance.byCategory ?? [];
    return cats.reduce((m, c) => Math.max(m, c.amount), 0) || 1;
  });
  readonly maxWeight = computed(() => {
    const s = this.data()?.health.weightSeries ?? [];
    return s.reduce((m, p) => Math.max(m, p.weight), 0) || 1;
  });
  readonly minWeight = computed(() => {
    const s = this.data()?.health.weightSeries ?? [];
    return s.length ? s.reduce((m, p) => Math.min(m, p.weight), Infinity) : 0;
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const { from, to } = this.form.getRawValue();
    if (!from || !to) return;
    this.loading.set(true);
    this.error.set(null);
    this.statsSvc.get(from, to).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không tải được thống kê.');
        this.loading.set(false);
      },
    });
  }

  catWidth(amount: number): number {
    return Math.round((amount / this.maxCat()) * 100);
  }

  /** Chiều cao cột cân nặng (chuẩn hóa theo min–max để thấy biến thiên). */
  weightHeight(w: number): number {
    const min = this.minWeight();
    const max = this.maxWeight();
    if (max === min) return 60;
    return Math.round(((w - min) / (max - min)) * 90 + 10);
  }


  private monthStart(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-01`;
  }
  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
