import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HealthService } from '../../core/services/health.service';
import { ConfirmService } from '../../core/services/confirm.service';
import {
  ACTIVITY_TYPE_LABELS,
  ACTIVITY_TYPES,
  Activity,
  ActivityTypeKey,
  HealthLog,
  HealthSummary,
  HealthTrend,
  Medication,
} from '../../core/models/health.models';

interface MetricDelta {
  label: string;
  unit: string;
  cur: number | null;
  prev: number | null;
  diff: number | null;
}

@Component({
  selector: 'app-health',
  imports: [ReactiveFormsModule],
  templateUrl: './health.html',
  styles: [`
    .date-row { display: flex; align-items: flex-end; gap: 1rem; margin-bottom: 1.25rem; }
    .date-row label { margin-bottom: 0; }
    .metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; }
    .metric { border: 1px solid var(--border); border-radius: 10px; padding: .9rem 1rem; }
    .metric .label { color: var(--muted); font-size: .8rem; }
    .metric .cur { font-size: 1.35rem; font-weight: 700; margin: .2rem 0; }
    .metric .delta { font-size: .82rem; }
    .delta.up { color: var(--danger); }
    .delta.down { color: var(--success); }
    .delta.flat { color: var(--muted); }
    .row { display: flex; gap: .75rem; flex-wrap: wrap; align-items: flex-end; }
    .row label { flex: 1; min-width: 130px; }
    .habit { display: flex; align-items: center; gap: .7rem; padding: .55rem 0; border-bottom: 1px solid var(--border); }
    .habit .name { flex: 1; }
    .check { width: 34px; height: 34px; border-radius: 50%; padding: 0; background: #eef1fb; color: var(--primary); border: 1px solid var(--border); }
    .check.on { background: var(--success); color: #fff; border-color: var(--success); }
    .icon-btn { background: transparent; color: var(--muted); padding: .2rem .4rem; }
    .trend-row { display: flex; align-items: flex-end; gap: 3px; height: 90px; overflow-x: auto; }
    .tcol { display: flex; align-items: flex-end; min-width: 8px; height: 100%; }
    .tb { width: 8px; border-radius: 2px 2px 0 0; display: block; }
    .tb.w { background: var(--primary); }
    .tb.c { background: var(--success); }
  `],
})
export class Health implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly health = inject(HealthService);
  private readonly confirm = inject(ConfirmService);

  readonly date = signal(this.todayIso());
  readonly summary = signal<HealthSummary | null>(null);
  readonly history = signal<HealthLog[]>([]);
  readonly msg = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  readonly medications = signal<Medication[]>([]);
  readonly medForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    dosage: [''],
    note: [''],
  });

  readonly hasToday = computed(() => !!this.summary()?.today);

  readonly deltas = computed<MetricDelta[]>(() => {
    const s = this.summary();
    const t = s?.today;
    const p = s?.previous;
    const make = (label: string, unit: string, key: keyof HealthLog): MetricDelta => {
      const cur = (t?.[key] as number | null) ?? null;
      const prev = (p?.[key] as number | null) ?? null;
      const diff = cur !== null && prev !== null ? +(cur - prev).toFixed(2) : null;
      return { label, unit, cur, prev, diff };
    };
    return [
      make('Cân nặng', 'kg', 'weight'),
      make('BMI', '', 'bmi'),
      make('Giờ ngủ', 'h', 'sleepHours'),
      make('Nước', 'ml', 'waterMl'),
      make('Tập', 'phút', 'workoutMinutes'),
    ];
  });

  readonly form = this.fb.nonNullable.group({
    weight: [null as number | null, [Validators.min(0), Validators.max(500)]],
    heightCm: [null as number | null, [Validators.min(0), Validators.max(300)]],
    sleepHours: [null as number | null, [Validators.min(0), Validators.max(24)]],
    waterMl: [null as number | null, [Validators.min(0), Validators.max(20000)]],
    workoutMinutes: [null as number | null, [Validators.min(0), Validators.max(1440)]],
    note: [''],
  });

  // --- Hoạt động & xu hướng (G5) ---
  readonly activities = signal<Activity[]>([]);
  readonly trend = signal<HealthTrend | null>(null);
  readonly activityTypes = ACTIVITY_TYPES;
  activityLabel(t: string): string { return ACTIVITY_TYPE_LABELS[t as ActivityTypeKey] ?? t; }
  readonly activityForm = this.fb.nonNullable.group({
    type: ['running' as ActivityTypeKey, [Validators.required]],
    durationMin: [null as number | null, [Validators.required, Validators.min(1), Validators.max(1440)]],
    calories: [null as number | null],
    note: [''],
  });

  ngOnInit(): void {
    this.load();
    this.loadHistory();
    this.loadMeds();
    this.loadActivities();
    this.loadTrends();
  }

  private loadActivities(): void {
    this.health.getActivities(this.date(), this.date()).subscribe({ next: (a) => this.activities.set(a) });
  }

  private loadTrends(): void {
    this.health.getTrends(this.isoDaysAgo(30), this.todayIso()).subscribe({ next: (t) => this.trend.set(t) });
  }

  addActivity(): void {
    if (this.activityForm.invalid) return;
    const v = this.activityForm.getRawValue();
    this.health.createActivity({
      date: this.date(), type: v.type, durationMin: Number(v.durationMin),
      calories: v.calories ?? null, note: v.note || null,
    }).subscribe({
      next: () => {
        this.activityForm.reset({ type: 'running', durationMin: null, calories: null, note: '' });
        this.loadActivities();
        this.loadTrends();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm hoạt động thất bại.'),
    });
  }

  async deleteActivity(a: Activity): Promise<void> {
    if (!(await this.confirm.ask(`Xóa hoạt động "${this.activityLabel(a.type)}"?`))) return;
    this.health.deleteActivity(a.id).subscribe({ next: () => { this.loadActivities(); this.loadTrends(); } });
  }

  changeDate(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.msg.set(null);
    this.load();
    this.loadMeds();
    this.loadActivities();
  }

  // --- Thuốc (A2) ---

  private loadMeds(): void {
    this.health.getMedications(this.date()).subscribe({ next: (m) => this.medications.set(m) });
  }

  addMed(): void {
    if (this.medForm.invalid) return;
    const v = this.medForm.getRawValue();
    this.health.createMedication(v.name, v.dosage || null, v.note || null).subscribe({
      next: () => {
        this.medForm.reset({ name: '', dosage: '', note: '' });
        this.loadMeds();
      },
    });
  }

  toggleMed(m: Medication): void {
    this.health.setMedicationTaken(m.id, this.date(), !m.taken).subscribe({
      next: (updated) => this.medications.update((list) => list.map((x) => (x.id === m.id ? updated : x))),
    });
  }

  async deleteMed(m: Medication): Promise<void> {
    if (!(await this.confirm.ask(`Xóa thuốc "${m.name}"?`))) return;
    this.health.deleteMedication(m.id).subscribe({ next: () => this.loadMeds() });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.health.getSummary(this.date()).subscribe({
      next: (s) => {
        this.summary.set(s);
        this.patchForm(s.today);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không tải được dữ liệu sức khỏe.');
        this.loading.set(false);
      },
    });
  }

  private loadHistory(): void {
    const from = this.isoDaysAgo(30);
    this.health.getLogs(from, this.todayIso()).subscribe({
      next: (l) => this.history.set(l),
      error: () => this.error.set('Không tải được lịch sử sức khỏe.'),
    });
  }

  private patchForm(log: HealthLog | null): void {
    this.form.reset({
      weight: log?.weight ?? null,
      heightCm: log?.heightCm ?? null,
      sleepHours: log?.sleepHours ?? null,
      waterMl: log?.waterMl ?? null,
      workoutMinutes: log?.workoutMinutes ?? null,
      note: log?.note ?? '',
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.health
      .upsert(this.date(), {
        weight: v.weight,
        heightCm: v.heightCm,
        sleepHours: v.sleepHours,
        waterMl: v.waterMl,
        workoutMinutes: v.workoutMinutes,
        note: v.note || null,
      })
      .subscribe({
        next: () => {
          this.msg.set('Đã lưu nhật ký.');
          this.load();
          this.loadHistory();
          this.loadTrends();
        },
        error: (err) => this.msg.set(err?.error?.error ?? 'Lưu thất bại.'),
      });
  }

  async remove(): Promise<void> {
    if (!(await this.confirm.ask('Xóa nhật ký ngày này?'))) return;
    this.health.deleteLog(this.date()).subscribe({
      next: () => {
        this.msg.set('Đã xóa nhật ký.');
        this.load();
        this.loadHistory();
      },
      error: (err) => this.msg.set(err?.error?.error ?? 'Xóa thất bại.'),
    });
  }

  // --- Tiện ích ---

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private isoDaysAgo(n: number): string {
    const d = new Date();
    d.setDate(d.getDate() - n);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
