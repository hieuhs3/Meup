import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkService } from '../../core/services/work.service';
import { ConfirmService } from '../../core/services/confirm.service';
import {
  GOAL_LEVEL_LABELS,
  GOAL_LEVELS,
  GOAL_STATUS_LABELS,
  GOAL_STATUSES,
  Goal,
  GoalLevel,
  GoalStatus,
  Habit,
  HabitFrequency,
  Recurrence,
  SaveGoalRequest,
  SaveHabitRequest,
  TaskItem,
  WORK_TASK_STATUS_LABELS,
  WORK_TASK_STATUSES,
  WorkTaskStatus,
  goalLevelOrdinal,
} from '../../core/models/work.models';

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
    .habit-block { padding: .55rem 0; border-bottom: 1px solid var(--border); }
    .habit-stats { display: flex; gap: 1rem; font-size: .76rem; color: var(--muted); margin: .25rem 0 .35rem; flex-wrap: wrap; }
    .heat { display: grid; grid-template-rows: repeat(7, 11px); grid-auto-flow: column; grid-auto-columns: 11px; gap: 2px; }
    .heat .c { width: 11px; height: 11px; border-radius: 2px; background: #eef1fb; }
    .heat .c.on { background: var(--success); }
    .view-toggle { display: flex; gap: .4rem; margin-bottom: 1rem; }
    .view-toggle button.on { background: var(--primary); color: #fff; }
    .kanban { display: flex; gap: .7rem; overflow-x: auto; padding-bottom: .5rem; }
    .kcol { flex: 0 0 220px; background: #f6f7fb; border: 1px solid var(--border); border-radius: 10px; padding: .6rem; }
    .kcol h4 { margin: 0 0 .5rem; font-size: .85rem; display: flex; justify-content: space-between; }
    .kcard { background: #fff; border: 1px solid var(--border); border-radius: 8px; padding: .5rem .6rem; margin-bottom: .5rem; }
    .kcard .t { font-size: .9rem; }
    .kcard .due { font-size: .72rem; }
    .kcard select { width: 100%; margin: .35rem 0 0; padding: .15rem; font-size: .75rem; }
    .goal-node.dim { opacity: .6; }
    .goal-children { margin-left: 1.2rem; border-left: 2px dashed var(--border); padding-left: .9rem; margin-top: .6rem; }
    .desc { margin: .15rem 0 .35rem; font-size: .88rem; }
    .target { font-size: .8rem; font-weight: 400; margin-left: .3rem; }
    .status-sel { width: auto; margin: 0; padding: .15rem .4rem; font-size: .8rem; }
    .badge { display: inline-block; font-size: .72rem; font-weight: 600; padding: .08rem .45rem; border-radius: 999px; margin-left: .35rem; vertical-align: middle; }
    .badge.lvl { background: #eef1fb; color: var(--primary); }
    .badge.st-draft { background: #f1f1f4; color: #555; }
    .badge.st-active { background: #e3f3ff; color: #0b6bcb; }
    .badge.st-completed { background: #e7f7ec; color: #1a7f37; }
    .badge.st-cancelled { background: #fdeaea; color: #b42318; }
    .badge.st-archived { background: #f0eef9; color: #6b4eaa; }
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

  // --- Mục tiêu đa cấp (G1) ---
  readonly levels = GOAL_LEVELS;
  readonly statuses = GOAL_STATUSES;
  levelLabel(l: string): string { return GOAL_LEVEL_LABELS[l as GoalLevel] ?? l; }
  statusLabel(s: string): string { return GOAL_STATUS_LABELS[s as GoalStatus] ?? s; }

  readonly goalForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    level: ['year' as GoalLevel, [Validators.required]],
    status: ['active' as GoalStatus, [Validators.required]],
    parentGoalId: ['' as string],
    targetDate: ['' as string],
    description: ['' as string, [Validators.maxLength(1000)]],
  });
  readonly habitForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    frequency: ['daily' as HabitFrequency],
    targetPerWeek: [null as number | null],
  });

  /** Lưới heatmap 12 tuần gần nhất (84 ô, xếp theo cột tuần). */
  heatCells(h: Habit): { date: string; on: boolean }[] {
    const set = new Set(h.recentChecks);
    const cells: { date: string; on: boolean }[] = [];
    const today = new Date();
    for (let i = 83; i >= 0; i--) {
      const d = new Date(today);
      d.setDate(today.getDate() - i);
      const iso = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      cells.push({ date: iso, on: set.has(iso) });
    }
    return cells;
  }

  /** Mục tiêu hợp lệ làm cha cho cấp đang chọn ở form (cấp cao hơn). */
  parentOptions(): Goal[] {
    const lvl = this.goalForm.controls.level.value;
    return this.goals().filter((g) => goalLevelOrdinal(g.level) < goalLevelOrdinal(lvl));
  }

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

  // --- Kanban (G11) ---
  readonly view = signal<'tree' | 'kanban'>('tree');
  readonly kanbanStatuses = WORK_TASK_STATUSES;
  statusLabelTask(s: string): string { return WORK_TASK_STATUS_LABELS[s as WorkTaskStatus] ?? s; }
  tasksByStatus(status: WorkTaskStatus): TaskItem[] {
    return this.tasks().filter((t) => t.status === status);
  }
  setTaskStatus(t: TaskItem, status: string): void {
    if (status === t.status) return;
    this.work.setTaskStatus(t.id, status).subscribe({ next: () => this.reload() });
  }

  // Cây mục tiêu (dựng từ danh sách phẳng)
  rootGoals(): Goal[] {
    return this.goals().filter((g) => !g.parentGoalId);
  }
  childGoals(parentId: string): Goal[] {
    return this.goals().filter((g) => g.parentGoalId === parentId);
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

  /** Thêm sub-task dưới một task (kèm hạn). */
  addSub(parent: TaskItem, titleEl: HTMLInputElement, dueEl: HTMLInputElement): void {
    const title = titleEl.value.trim();
    if (!title) return;
    if (!dueEl.value) { this.error.set('Task con cần có ngày hạn (deadline).'); return; }
    this.error.set(null);
    this.work.createTask({ title, parentTaskId: parent.id, dueDate: dueEl.value }).subscribe({
      next: () => {
        titleEl.value = '';
        dueEl.value = '';
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
    const v = this.goalForm.getRawValue();
    const body: SaveGoalRequest = {
      name: v.name,
      level: v.level,
      status: v.status,
      parentGoalId: v.parentGoalId || null,
      targetDate: v.targetDate || null,
      description: v.description || null,
    };
    this.work.createGoal(body).subscribe({
      next: () => {
        this.goalForm.reset({ name: '', level: 'year', status: 'active', parentGoalId: '', targetDate: '', description: '' });
        this.loadGoals();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Thêm mục tiêu thất bại.'),
    });
  }

  /** Mở form thêm mục tiêu con: gắn cha + chọn cấp ngay dưới cha. */
  startAddChild(parent: Goal): void {
    const idx = goalLevelOrdinal(parent.level);
    const childLevel = this.levels[Math.min(idx + 1, this.levels.length - 1)];
    this.goalForm.patchValue({ parentGoalId: parent.id, level: childLevel });
    document.getElementById('goal-form')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  /** Đổi nhanh trạng thái mục tiêu (giữ các trường còn lại). */
  changeStatus(g: Goal, status: GoalStatus): void {
    this.work.updateGoal(g.id, {
      name: g.name, level: g.level, status,
      description: g.description, targetDate: g.targetDate, parentGoalId: g.parentGoalId,
    }).subscribe({
      next: () => this.reload(),
      error: (err) => this.error.set(err?.error?.error ?? 'Cập nhật trạng thái thất bại.'),
    });
  }

  async deleteGoal(g: Goal): Promise<void> {
    const msg = g.childCount
      ? `Xóa mục tiêu "${g.name}", toàn bộ mục tiêu con và task bên trong?`
      : `Xóa mục tiêu "${g.name}" và toàn bộ task bên trong?`;
    if (!(await this.confirm.ask(msg))) return;
    this.work.deleteGoal(g.id).subscribe({ next: () => this.reload() });
  }

  // --- Habit ---

  private loadHabits(): void {
    this.work.getHabits(this.todayIso()).subscribe({ next: (h) => this.habits.set(h) });
  }

  addHabit(): void {
    if (this.habitForm.invalid) return;
    const v = this.habitForm.getRawValue();
    const body: SaveHabitRequest = {
      name: v.name,
      frequency: v.frequency,
      targetPerWeek: v.frequency === 'weekly' ? v.targetPerWeek : null,
    };
    this.work.createHabit(body).subscribe({
      next: () => {
        this.habitForm.reset({ name: '', frequency: 'daily', targetPerWeek: null });
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
