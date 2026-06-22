import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { Stats } from '../models/stats.models';

@Injectable({ providedIn: 'root' })
export class StatsService {
  private readonly http = inject(HttpClient);

  get(from: string, to: string): Observable<Stats> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<Stats>(`${API_BASE}/stats`, { params });
  }
}
