import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { DocumentItem } from '../models/document.models';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/documents`;

  list(category?: string): Observable<DocumentItem[]> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<DocumentItem[]>(this.base, { params });
  }

  upload(file: File, category: string): Observable<DocumentItem> {
    const form = new FormData();
    form.append('file', file);
    form.append('category', category);
    return this.http.post<DocumentItem>(this.base, form);
  }

  download(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/download`, { responseType: 'blob' });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
