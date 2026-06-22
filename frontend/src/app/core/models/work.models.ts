export type TaskStatus = 'all' | 'active' | 'done';

export type Recurrence = 'none' | 'daily' | 'weekly' | 'monthly';

export interface TaskItem {
  id: string;
  title: string;
  isDone: boolean;
  dueDate: string | null;
  isOverdue: boolean;
  completedAt: string | null;
  createdAt: string;
  recurrence: Recurrence;
}

export interface CreateTaskRequest {
  title: string;
  dueDate?: string | null;
  recurrence?: Recurrence;
}

export interface UpdateTaskRequest {
  title: string;
  dueDate?: string | null;
  isDone: boolean;
  recurrence?: Recurrence;
}

export interface Goal {
  id: string;
  name: string;
  progress: number;
  createdAt: string;
}

export interface Habit {
  id: string;
  name: string;
  date: string;
  checked: boolean;
  streak: number;
  createdAt: string;
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
