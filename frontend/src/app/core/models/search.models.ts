export interface SearchHit {
  type: 'transaction' | 'journal' | 'task' | 'event';
  id: string;
  title: string;
  snippet: string | null;
  date: string | null;
}

export interface SearchResult {
  items: SearchHit[];
  total: number;
}
