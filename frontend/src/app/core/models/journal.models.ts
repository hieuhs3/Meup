export type Mood = 'excellent' | 'good' | 'normal' | 'bad' | 'terrible';

export const MOODS: Mood[] = ['excellent', 'good', 'normal', 'bad', 'terrible'];

export const MOOD_LABELS: Record<Mood, string> = {
  excellent: 'Tuyệt vời', good: 'Tốt', normal: 'Bình thường', bad: 'Tệ', terrible: 'Rất tệ',
};
export const MOOD_EMOJIS: Record<Mood, string> = {
  excellent: '😄', good: '🙂', normal: '😐', bad: '🙁', terrible: '😢',
};

export interface JournalEntry {
  id: string;
  date: string; // yyyy-MM-dd
  title: string | null;
  contentHtml: string;
  mood: Mood | null;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertJournalRequest {
  date: string;
  title?: string | null;
  contentHtml: string;
  mood?: Mood | null;
}

export interface MoodTrendPoint {
  date: string;
  mood: Mood;
  score: number; // 1–5
}
