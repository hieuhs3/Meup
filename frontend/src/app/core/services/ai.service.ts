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

export interface AiStatus {
  /** AI dùng được (user có key riêng hoặc server có key). */
  enabled: boolean;
  /** User đã tự đặt API key của mình. */
  hasUserKey: boolean;
  /** Đang dùng key chung của server (user chưa đặt key). */
  usingServerKey: boolean;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/ai`;

  status(): Observable<AiStatus> {
    return this.http.get<AiStatus>(`${this.base}/status`);
  }

  /** Lưu API key Claude của riêng người dùng (mã hóa phía server). */
  setKey(apiKey: string): Observable<AiStatus> {
    return this.http.put<AiStatus>(`${this.base}/key`, { apiKey });
  }

  /** Xóa API key của người dùng. */
  clearKey(): Observable<AiStatus> {
    return this.http.delete<AiStatus>(`${this.base}/key`);
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
