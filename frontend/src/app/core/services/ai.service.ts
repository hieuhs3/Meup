import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';

export interface WeeklyInsight {
  enabled: boolean;
  summary: string | null;
  from: string;
  to: string;
}

export interface CategorySuggestion {
  enabled: boolean;
  categoryId: string | null;
  categoryName: string | null;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/ai`;

  status(): Observable<{ enabled: boolean }> {
    return this.http.get<{ enabled: boolean }>(`${this.base}/status`);
  }

  weeklyInsight(date?: string, refresh = false): Observable<WeeklyInsight> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    if (refresh) params = params.set('refresh', 'true');
    return this.http.get<WeeklyInsight>(`${this.base}/weekly-insight`, { params });
  }

  categorize(note: string, type: string): Observable<CategorySuggestion> {
    return this.http.post<CategorySuggestion>(`${this.base}/categorize`, { note, type });
  }
}
