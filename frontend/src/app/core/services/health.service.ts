import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { HealthLog, HealthSummary, Medication, UpsertHealthLogRequest } from '../models/health.models';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/health`;

  getLog(date: string): Observable<HealthLog | null> {
    return this.http.get<HealthLog | null>(`${this.base}/logs/${date}`);
  }

  getLogs(from?: string, to?: string): Observable<HealthLog[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<HealthLog[]>(`${this.base}/logs`, { params });
  }

  upsert(date: string, body: UpsertHealthLogRequest): Observable<HealthLog> {
    return this.http.put<HealthLog>(`${this.base}/logs/${date}`, body);
  }

  deleteLog(date: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/logs/${date}`);
  }

  getSummary(date?: string): Observable<HealthSummary> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<HealthSummary>(`${this.base}/summary`, { params });
  }

  // --- Thuốc (A2) ---

  private readonly medBase = `${API_BASE}/medications`;

  getMedications(date: string): Observable<Medication[]> {
    return this.http.get<Medication[]>(this.medBase, { params: new HttpParams().set('date', date) });
  }

  createMedication(name: string, dosage: string | null, note: string | null): Observable<Medication> {
    return this.http.post<Medication>(this.medBase, { name, dosage, note });
  }

  deleteMedication(id: string): Observable<void> {
    return this.http.delete<void>(`${this.medBase}/${id}`);
  }

  setMedicationTaken(id: string, date: string, taken: boolean): Observable<Medication> {
    const params = new HttpParams().set('date', date);
    return taken
      ? this.http.post<Medication>(`${this.medBase}/${id}/take`, {}, { params })
      : this.http.delete<Medication>(`${this.medBase}/${id}/take`, { params });
  }
}
