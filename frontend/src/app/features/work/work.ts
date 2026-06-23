import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkService } from '../../core/services/work.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { Goal, Habit, Recurrence, TaskItem } from '../../core/models/work.models';

@Component({
  selector: 'app-work',
  imports: [ReactiveFormsModule, NgTemplateOutlet],
  templateUrl: './work.html',
  styles: [`
    .inline-form { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1rem; }
    .inline-form label { margin-bottom: 0; }
    .inline-form .grow { flex: 1; min-width: 160px; }
    .goal-head { display: flex; justify-content: space-between; align-items: center; gap: .5rem; }
    .goal-head strong { font-size: 1.05rem; }
    .bar { height: 8px; background: #eef1fb; border-radius: 999px; overflow: hidden; margin: .5rem 0 .75rem; }
    .bar > span { display: block; height: 100%; background: var(--success); transition: width .25s; }
    .quick-add { display: flex; gap: .5rem; margin: .5rem 0; flex-wrap: wrap; }
    .quick-add input[type=text] { flex: 1; min-width: 160px; margin: 0; }
    .quick-add input[type=date] { width: auto; margin: 0; }
    .item { display: flex; align-items: center; gap: .55rem; padding: .4rem 0; }
    .item .title { flex: 1; }
    .item .title.done { text-decoration: line-through; color: var(--muted); }
    .due { font-size: .8rem; color: var(--muted); white-space: nowrap; }
    .due.overdue { color: var(--danger); font-weight: 600; }
    .children { margin-left: 1.4rem; border-left: 2px solid var(--border); padding-left: .6rem; }
    .subadd { display: flex; gap: .5rem; margin: .3rem 0 .3rem 1.4rem; }
    .subadd input { margin: 0; }
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
  private readonly confirm = inject(ConfirmService);

  readonly tasks = signal<TaskItem[]>([]);
  readonly goals = signal<Goal[]>([]);
  readonly habits = signal<Habit[]>([]);
  readonly error = signal<string | null>(null);
  /** Task đang mở ô thêm sub-task (chỉ 1 ô mở mỗi lần). */
  readonly addingSubFor = signal<string | null>(null);

  private readonly recurrenceLabels: Record<Recurrence, string> = {
    none: '', daily: '🔁 hằng ngày', weekly: '🔁 hằng tuần', monthly: '🔁 hằng tháng',
  };
  recurrenceLabel(r: string): string {
    return this.recurrenceLabels[r as Recurrence] ?? '';
  }

  readonly goalForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
  });
  readonly habitForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
  });

  ngOnInit(): void {
    this.loadTasks();
    this.loadGoals();
    this.loadHabits();
  }

  // --- Dựng cây từ danh sách phẳng ---

  topTasks(goalId: string): TaskItem[] {
    return this.tasks().filter((t) => t.goalId === goalId && !t.parentTaskId);
  }
  childrenOf(taskId: string): TaskItem[] {
    return this.tasks().filter((t) => t.parentTaskId === taskId);
  }
  standaloneTasks(): TaskItem[] {
    return this.tasks().filter((t) => !t.goalId && !t.parentTaskId);
  }

  // --- Task ---

  private loadTasks(): void {
    this.work.getTasks('all').subscribe({
      next: (t) => this.tasks.set(t),
      error: () => this.error.set('Không tải được công việc.'),
    });
  }

  /** Thêm task cấp 1 (vào mục tiêu nếu có goalId, ngược lại là task tự do). */
  addTask(goalId: string | null, titleEl: HTMLInputElement, dueEl?: HTMLInputElement): void {
    const title = titleEl.value.trim();
    if (!title) return;
    this.work.createTask({ title, goalId, dueDate: dueEl?.value || null }).subscribe({
      next: () => {
        titleEl.value = '';
        if (dueEl) dueEl.value = '';
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm task thất bại.'),
    });
  }

  /** Thêm sub-task dưới một task. */
  addSub(parent: TaskItem, titleEl: HTMLInputElement): void {
    const title = titleEl.value.trim();
    if (!title) return;
    this.work.createTask({ title, parentTaskId: parent.id }).subscribe({
      next: () => {
        titleEl.value = '';
        this.addingSubFor.set(null);
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm task con thất bại.'),
    });
  }

  toggleAddSub(taskId: string): void {
    this.addingSubFor.set(this.addingSubFor() === taskId ? null : taskId);
  }

  toggleTask(t: TaskItem): void {
    this.work.toggleTask(t.id).subscribe({ next: () => this.reload() });
  }

  async deleteTask(t: TaskItem): Promise<void> {
    const msg = this.childrenOf(t.id).length
      ? 'Xóa task này và toàn bộ task con?'
      : 'Xóa task này?';
    if (!(await this.confirm.ask(msg))) return;
    this.work.deleteTask(t.id).subscribe({ next: () => this.reload() });
  }

  /** Tải lại task + goal (để tiến độ goal cập nhật theo). */
  private reload(): void {
    this.loadTasks();
    this.loadGoals();
  }

  // --- Goal ---

  private loadGoals(): void {
    this.work.getGoals().subscribe({ next: (g) => this.goals.set(g) });
  }

  addGoal(): void {
    if (this.goalForm.invalid) return;
    this.work.createGoal(this.goalForm.getRawValue().name).subscribe({
      next: () => {
        this.goalForm.reset({ name: '' });
        this.loadGoals();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm mục tiêu thất bại.'),
    });
  }

  async deleteGoal(g: Goal): Promise<void> {
    if (!(await this.confirm.ask(`Xóa mục tiêu "${g.name}" và toàn bộ task bên trong?`))) return;
    this.work.deleteGoal(g.id).subscribe({ next: () => this.reload() });
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

  async deleteHabit(h: Habit): Promise<void> {
    if (!(await this.confirm.ask(`Xóa thói quen "${h.name}"?`))) return;
    this.work.deleteHabit(h.id).subscribe({ next: () => this.loadHabits() });
  }

  // --- Tiện ích ---

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
