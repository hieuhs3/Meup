export interface CalendarEvent {
  id: string;
  date: string; // yyyy-MM-dd
  startTime: string | null; // HH:mm:ss
  endTime: string | null;
  title: string;
  location: string | null;
  note: string | null;
}

export interface UpsertEventRequest {
  date: string;
  startTime?: string | null;
  endTime?: string | null;
  title: string;
  location?: string | null;
  note?: string | null;
}
