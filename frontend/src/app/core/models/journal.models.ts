export interface JournalEntry {
  id: string;
  date: string; // yyyy-MM-dd
  title: string | null;
  contentHtml: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpsertJournalRequest {
  date: string;
  title?: string | null;
  contentHtml: string;
}
