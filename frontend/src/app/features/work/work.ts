import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkService } from '../../core/services/work.service';
import { Goal, Habit, Recurrence, TaskItem, TaskStatus } from '../../core/models/work.models';

@Component({
  selector: 'app-work',
  imports: [ReactiveFormsModule],
  templateUrl: './work.html',
  styles: [`
    .inline-form { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1rem; }
    .inline-form label { margin-bottom: 0; }
    .inline-form .grow { flex: 1; min-width: 160px; }
    .tabs { display: flex; gap: .4rem; margin-bottom: .75rem; }
    .tabs button { background: transparent; color: var(--primary); border: 1px solid var(--border); }
    .tabs button.active { background: var(--primary); color: #fff; }
    .item { display: flex; align-items: center; gap: .6rem; padding: .55rem 0; border-bottom: 1px solid var(--border); }
    .item .title { flex: 1; }
    .item .title.done { text-decoration: line-through; color: var(--muted); }
    .due { font-size: .8rem; color: var(--muted); }
    .due.overdue { color: var(--danger); font-weight: 600; }
    .icon-btn { background: transparent; color: var(--muted); padding: .2rem .4rem; }
    .goal { padding: .6rem 0; border-bottom: 1px solid var(--border); }
    .goal-head { display: flex; justify-content: space-between; align-items: center; }
    .bar { height: 8px; background: #eef1fb; border-radius: 999px; overflow: hidden; margin: .4rem 0; }
    .bar > span { display: block; height: 100%; background: var(--primary); }
    .habit { display: flex; align-items: center; gap: .7rem; padding: .55rem 0; border-bottom: 1px solid var(--border); }
    .habit .name { flex: 1; }
    .check { width: 34px; height: 34px; border-radius: 50%; padding: 0; background: #eef1fb; color: var(--primary); border: 1px solid var(--border); }
    .check.on { background: var(--success); color: #fff; border-color: var(--success); }
    .streak { font-size: .85rem; color: var(--muted); min-width: 54px; }
  `],
})
export class Work implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly work = inject(WorkService);

  readonly tasks = signal<TaskItem[]>([]);
  readonly goals = signal<Goal[]>([]);
  readonly habits = signal<Habit[]>([]);
  readonly status = signal<TaskStatus>('all');
  readonly error = signal<string | null>(null);

  readonly taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    dueDate: [''],
    recurrence: ['none' as Recurrence],
  });

  readonly recurrenceLabels: Record<Recurrence, string> = {
    none: '', daily: '🔁 hằng ngày', weekly: '🔁 hằng tuần', monthly: '🔁 hằng tháng',
  };
  readonly goalForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    progress: [0, [Validators.min(0), Validators.max(100)]],
  });
  readonly habitForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
  });

  ngOnInit(): void {
    this.loadTasks();
    this.loadGoals();
    this.loadHabits();
  }

  // --- Task ---

  private loadTasks(): void {
    this.work.getTasks(this.status()).subscribe({
      next: (t) => this.tasks.set(t),
      error: () => this.error.set('Không tải được công việc.'),
    });
  }

  setStatus(s: TaskStatus): void {
    this.status.set(s);
    this.loadTasks();
  }

  addTask(): void {
    if (this.taskForm.invalid) return;
    const v = this.taskForm.getRawValue();
    this.work.createTask({ title: v.title, dueDate: v.dueDate || null, recurrence: v.recurrence }).subscribe({
      next: () => {
        this.taskForm.reset({ title: '', dueDate: '', recurrence: 'none' });
        this.loadTasks();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm công việc thất bại.'),
    });
  }

  toggleTask(t: TaskItem): void {
    this.work.toggleTask(t.id).subscribe({ next: () => this.loadTasks() });
  }

  deleteTask(t: TaskItem): void {
    if (!confirm('Xóa công việc này?')) return;
    this.work.deleteTask(t.id).subscribe({ next: () => this.loadTasks() });
  }

  // --- Goal ---

  private loadGoals(): void {
    this.work.getGoals().subscribe({ next: (g) => this.goals.set(g) });
  }

  addGoal(): void {
    if (this.goalForm.invalid) return;
    const v = this.goalForm.getRawValue();
    this.work.createGoal(v.name, Number(v.progress)).subscribe({
      next: () => {
        this.goalForm.reset({ name: '', progress: 0 });
        this.loadGoals();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm mục tiêu thất bại.'),
    });
  }

  saveProgress(g: Goal, value: string): void {
    const p = Math.max(0, Math.min(100, Number(value)));
    this.work.updateGoal(g.id, g.name, p).subscribe({
      next: (updated) => this.goals.update((list) => list.map((x) => (x.id === g.id ? updated : x))),
    });
  }

  deleteGoal(g: Goal): void {
    if (!confirm(`Xóa mục tiêu "${g.name}"?`)) return;
    this.work.deleteGoal(g.id).subscribe({ next: () => this.loadGoals() });
  }

  // --- Habit ---

  private loadHabits(): void {
    this.work.getHabits(this.todayIso()).subscribe({ next: (h) => this.habits.set(h) });
  }

  addHabit(): void {
    if (this.habitForm.invalid) return;
    this.work.createHabit(this.habitForm.getRawValue().name).subscribe({
      next: () => {
        this.habitForm.reset({ name: '' });
        this.loadHabits();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm thói quen thất bại.'),
    });
  }

  toggleHabit(h: Habit): void {
    this.work.setHabitCheck(h.id, this.todayIso(), !h.checked).subscribe({
      next: (updated) => this.habits.update((list) => list.map((x) => (x.id === h.id ? updated : x))),
    });
  }

  deleteHabit(h: Habit): void {
    if (!confirm(`Xóa thói quen "${h.name}"?`)) return;
    this.work.deleteHabit(h.id).subscribe({ next: () => this.loadHabits() });
  }

  // --- Tiện ích ---

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
