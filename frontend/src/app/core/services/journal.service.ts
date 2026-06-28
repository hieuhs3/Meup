import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { JournalEntry, MoodTrendPoint, UpsertJournalRequest } from '../models/journal.models';

@Injectable({ providedIn: 'root' })
export class JournalService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/journal`;

  list(from?: string | null, to?: string | null, q?: string | null): Observable<JournalEntry[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (q) params = params.set('q', q);
    return this.http.get<JournalEntry[]>(this.base, { params });
  }

  get(id: string): Observable<JournalEntry> {
    return this.http.get<JournalEntry>(`${this.base}/${id}`);
  }

  create(body: UpsertJournalRequest): Observable<JournalEntry> {
    return this.http.post<JournalEntry>(this.base, body);
  }

  update(id: string, body: UpsertJournalRequest): Observable<JournalEntry> {
    return this.http.put<JournalEntry>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  moodTrend(from?: string | null, to?: string | null): Observable<MoodTrendPoint[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<MoodTrendPoint[]>(`${this.base}/mood-trend`, { params });
  }
}
