export interface CategoryStat {
  name: string;
  color: string | null;
  type: string; // income | expense
  amount: number;
}

export interface DailyNet {
  date: string;
  income: number;
  expense: number;
}

export interface FinanceStats {
  totalIncome: number;
  totalExpense: number;
  byCategory: CategoryStat[];
  daily: DailyNet[];
}

export interface WeightPoint {
  date: string;
  weight: number;
}

export interface HealthStats {
  avgWeight: number | null;
  avgSleep: number | null;
  avgWater: number | null;
  avgWorkout: number | null;
  days: number;
  weightSeries: WeightPoint[];
}

export interface WorkStats {
  tasksTotal: number;
  tasksDone: number;
  goalsCount: number;
  goalsAvgProgress: number;
  habitsTotal: number;
  habitChecks: number;
}

export interface Stats {
  from: string;
  to: string;
  finance: FinanceStats;
  health: HealthStats;
  work: WorkStats;
}
