export type TaskStatus = 'all' | 'active' | 'done';

export type Recurrence = 'none' | 'daily' | 'weekly' | 'monthly';

export type WorkTaskStatus = 'todo' | 'in_progress' | 'review' | 'done' | 'cancelled';

export const WORK_TASK_STATUSES: WorkTaskStatus[] = ['todo', 'in_progress', 'review', 'done', 'cancelled'];

export const WORK_TASK_STATUS_LABELS: Record<WorkTaskStatus, string> = {
  todo: 'Cần làm', in_progress: 'Đang làm', review: 'Soát lại', done: 'Xong', cancelled: 'Đã hủy',
};

export interface TaskItem {
  id: string;
  title: string;
  isDone: boolean;
  dueDate: string | null;
  isOverdue: boolean;
  completedAt: string | null;
  createdAt: string;
  recurrence: Recurrence;
  goalId: string | null;
  parentTaskId: string | null;
  status: WorkTaskStatus;
}

export interface CreateTaskRequest {
  title: string;
  dueDate?: string | null;
  recurrence?: Recurrence;
  goalId?: string | null;
  parentTaskId?: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  dueDate?: string | null;
  isDone: boolean;
  recurrence?: Recurrence;
}

export type GoalLevel = 'life' | 'year' | 'quarter' | 'month' | 'week';
export type GoalStatus = 'draft' | 'active' | 'completed' | 'cancelled' | 'archived';

export const GOAL_LEVELS: GoalLevel[] = ['life', 'year', 'quarter', 'month', 'week'];
export const GOAL_STATUSES: GoalStatus[] = ['draft', 'active', 'completed', 'cancelled', 'archived'];

export const GOAL_LEVEL_LABELS: Record<GoalLevel, string> = {
  life: 'Đời', year: 'Năm', quarter: 'Quý', month: 'Tháng', week: 'Tuần',
};
export const GOAL_STATUS_LABELS: Record<GoalStatus, string> = {
  draft: 'Nháp', active: 'Đang chạy', completed: 'Hoàn thành', cancelled: 'Đã hủy', archived: 'Lưu trữ',
};

/** Thứ hạng cấp (nhỏ = cao hơn trong cây) — khớp GoalLevel.Ordinal ở backend. */
export function goalLevelOrdinal(level: GoalLevel): number {
  return GOAL_LEVELS.indexOf(level);
}

export interface Goal {
  id: string;
  name: string;
  progress: number; // tính tự động (rollup từ goal con + task con)
  createdAt: string;
  taskCount: number;
  doneCount: number;
  level: GoalLevel;
  status: GoalStatus;
  description: string | null;
  targetDate: string | null;
  parentGoalId: string | null;
  childCount: number;
}

export interface SaveGoalRequest {
  name: string;
  level?: GoalLevel;
  status?: GoalStatus;
  description?: string | null;
  targetDate?: string | null;
  parentGoalId?: string | null;
}

/** Nút cây mục tiêu (lồng nhau) cho dashboard. */
export interface GoalTreeNode {
  id: string;
  name: string;
  progress: number;
  level: GoalLevel;
  status: GoalStatus;
  description: string | null;
  targetDate: string | null;
  parentGoalId: string | null;
  taskCount: number;
  doneCount: number;
  createdAt: string;
  children: GoalTreeNode[];
}

export type HabitFrequency = 'daily' | 'weekly';

export interface Habit {
  id: string;
  name: string;
  date: string;
  checked: boolean;
  streak: number;
  createdAt: string;
  frequency: HabitFrequency;
  targetPerWeek: number | null;
  bestStreak: number;
  completionRate: number; // % trong 30 ngày gần nhất
  recentChecks: string[]; // các ngày đã check trong 12 tuần gần nhất (yyyy-MM-dd)
}

export interface SaveHabitRequest {
  name: string;
  frequency?: HabitFrequency;
  targetPerWeek?: number | null;
}

export interface WorkSummary {
  tasksTotal: number;
  tasksDone: number;
  tasksOverdue: number;
  goalsCount: number;
  goalsAvgProgress: number;
  habitsTotal: number;
  habitsCheckedToday: number;
}
