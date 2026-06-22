import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE } from '../api.config';
import { AppNotification } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/notifications`;

  /** Số chưa đọc dùng chung (chuông ở shell). */
  readonly unread = signal(0);

  list(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(this.base);
  }

  refreshUnread(): void {
    this.http.get<{ count: number }>(`${this.base}/unread-count`).subscribe({
      next: (r) => this.unread.set(r.count),
    });
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/read`, {}).pipe(tap(() => this.refreshUnread()));
  }

  markAllRead(): Observable<void> {
    return this.http.post<void>(`${this.base}/read-all`, {}).pipe(tap(() => this.unread.set(0)));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(tap(() => this.refreshUnread()));
  }

  runReminders(): Observable<{ created: boolean; notification: AppNotification | null }> {
    return this.http
      .post<{ created: boolean; notification: AppNotification | null }>(`${this.base}/run-reminders`, {})
      .pipe(tap(() => this.refreshUnread()));
  }
}
