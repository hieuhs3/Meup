export interface NoteRef {
  id: string;
  title: string;
}

export interface Note {
  id: string;
  title: string | null;
  content: string;
  category: string | null;
  tags: string[];
  outLinks: string[];
  backlinks: NoteRef[];
  createdAt: string;
  updatedAt: string;
}

export interface UpsertNoteRequest {
  content: string;
  title?: string | null;
  category?: string | null;
  tags?: string[];
}
