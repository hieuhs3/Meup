import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EventService } from '../../core/services/event.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CalendarEvent } from '../../core/models/event.models';

@Component({
  selector: 'app-calendar',
  imports: [ReactiveFormsModule],
  templateUrl: './calendar.html',
  styles: [`
    .date-bar { display: flex; align-items: center; gap: .6rem; margin-bottom: 1.25rem; flex-wrap: wrap; }
    .date-bar input { width: auto; margin: 0; }
    .nav { padding: .45rem .7rem; }
    .ev { display: flex; gap: .8rem; padding: .6rem 0; border-bottom: 1px solid var(--border); }
    .ev .time { min-width: 110px; color: var(--primary); font-weight: 600; font-size: .9rem; }
    .ev .time.allday { color: var(--muted); font-weight: 500; }
    .ev .body { flex: 1; }
    .ev .title { font-weight: 600; }
    .ev .meta { color: var(--muted); font-size: .85rem; }
    .row { display: flex; gap: .75rem; flex-wrap: wrap; align-items: flex-end; }
    .row label { flex: 1; min-width: 130px; }
    .actions button { padding: .25rem .6rem; font-size: .82rem; margin-left: .35rem; }
  `],
})
export class Calendar implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly events = inject(EventService);
  private readonly confirm = inject(ConfirmService);

  readonly date = signal(this.todayIso());
  readonly list = signal<CalendarEvent[]>([]);
  readonly editingId = signal<string | null>(null); // null = đóng; '' = mới
  readonly error = signal<string | null>(null);

  readonly editorOpen = computed(() => this.editingId() !== null);

  readonly form = this.fb.nonNullable.group({
    startTime: [''],
    endTime: [''],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    location: [''],
    note: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  changeDate(value: string): void {
    if (!value) return;
    this.date.set(value);
    this.editingId.set(null);
    this.load();
  }

  shiftDay(days: number): void {
    const d = this.parse(this.date());
    d.setDate(d.getDate() + days);
    this.date.set(this.iso(d));
    this.editingId.set(null);
    this.load();
  }

  load(): void {
    const d = this.date();
    this.events.list(d, d).subscribe({
      next: (e) => this.list.set(e),
      error: () => this.error.set('Không tải được sự kiện.'),
    });
  }

  newEvent(): void {
    this.form.reset({ startTime: '', endTime: '', title: '', location: '', note: '' });
    this.editingId.set('');
  }

  edit(e: CalendarEvent): void {
    this.form.reset({
      startTime: this.hm(e.startTime),
      endTime: this.hm(e.endTime),
      title: e.title,
      location: e.location ?? '',
      note: e.note ?? '',
    });
    this.editingId.set(e.id);
  }

  cancel(): void {
    this.editingId.set(null);
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const body = {
      date: this.date(),
      startTime: this.toTime(v.startTime),
      endTime: this.toTime(v.endTime),
      title: v.title,
      location: v.location || null,
      note: v.note || null,
    };
    const id = this.editingId();
    const req = id ? this.events.update(id, body) : this.events.create(body);
    req.subscribe({
      next: () => {
        this.editingId.set(null);
        this.load();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Lưu sự kiện thất bại.'),
    });
  }

  async remove(e: CalendarEvent): Promise<void> {
    if (!(await this.confirm.ask('Xóa sự kiện này?'))) return;
    this.events.delete(e.id).subscribe({ next: () => this.load() });
  }

  // --- Tiện ích ---

  /** "HH:mm:ss" → "HH:mm" để hiển thị / đổ vào input. */
  hm(t: string | null): string {
    return t ? t.slice(0, 5) : '';
  }

  /** input "HH:mm" → "HH:mm:00" cho TimeOnly; rỗng → null. */
  private toTime(v: string): string | null {
    if (!v) return null;
    return v.length === 5 ? `${v}:00` : v;
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
}
