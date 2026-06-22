import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { CalendarEvent, UpsertEventRequest } from '../models/event.models';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/events`;

  list(from?: string | null, to?: string | null): Observable<CalendarEvent[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<CalendarEvent[]>(this.base, { params });
  }

  create(body: UpsertEventRequest): Observable<CalendarEvent> {
    return this.http.post<CalendarEvent>(this.base, body);
  }

  update(id: string, body: UpsertEventRequest): Observable<CalendarEvent> {
    return this.http.put<CalendarEvent>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
