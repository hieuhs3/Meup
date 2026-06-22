export interface HealthLog {
  date: string; // yyyy-MM-dd
  weight: number | null;
  sleepHours: number | null;
  waterMl: number | null;
  workoutMinutes: number | null;
  note: string | null;
  updatedAt: string;
}

export interface UpsertHealthLogRequest {
  weight?: number | null;
  sleepHours?: number | null;
  waterMl?: number | null;
  workoutMinutes?: number | null;
  note?: string | null;
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

