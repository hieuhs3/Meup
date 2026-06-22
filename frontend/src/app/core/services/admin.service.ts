import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { AdminUser } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  listUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${API_BASE}/admin/users`);
  }

  toggleLock(id: string): Observable<{ id: string; isLocked: boolean }> {
    return this.http.post<{ id: string; isLocked: boolean }>(`${API_BASE}/admin/users/${id}/lock`, {});
  }
}
