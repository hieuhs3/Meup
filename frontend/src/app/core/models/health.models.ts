export interface HealthLog {
  date: string; // yyyy-MM-dd
  weight: number | null;
  heightCm: number | null;
  bmi: number | null;
  sleepHours: number | null;
  waterMl: number | null;
  workoutMinutes: number | null;
  note: string | null;
  updatedAt: string;
}

export interface UpsertHealthLogRequest {
  weight?: number | null;
  heightCm?: number | null;
  sleepHours?: number | null;
  waterMl?: number | null;
  workoutMinutes?: number | null;
  note?: string | null;
}

export type ActivityTypeKey = 'running' | 'walking' | 'gym' | 'swimming' | 'cycling' | 'other';

export const ACTIVITY_TYPES: ActivityTypeKey[] = ['running', 'walking', 'gym', 'swimming', 'cycling', 'other'];

export const ACTIVITY_TYPE_LABELS: Record<ActivityTypeKey, string> = {
  running: 'Chạy bộ', walking: 'Đi bộ', gym: 'Gym', swimming: 'Bơi', cycling: 'Đạp xe', other: 'Khác',
};

export interface Activity {
  id: string;
  date: string;
  type: ActivityTypeKey;
  durationMin: number;
  calories: number | null;
  note: string | null;
  createdAt: string;
}

export interface SaveActivityRequest {
  date: string;
  type: ActivityTypeKey;
  durationMin: number;
  calories?: number | null;
  note?: string | null;
}

export interface TrendPoint {
  date: string;
  value: number | null;
}

export interface HealthTrend {
  weight: TrendPoint[];
  bmi: TrendPoint[];
  calories: TrendPoint[];
}

export interface HealthSummary {
  date: string;
  today: HealthLog | null;
  previous: HealthLog | null;
}

export interface Medication {
  id: string;
  name: string;
  dosage: string | null;
  note: string | null;
  date: string;
  taken: boolean;
  createdAt: string;
}

