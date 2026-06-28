import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Note, UpsertNoteRequest } from '../models/note.models';
import { API_BASE } from '../api.config';

@Injectable({ providedIn: 'root' })
export class NoteService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/notes`;

  list(tag?: string, category?: string, q?: string): Observable<Note[]> {
    let params = new HttpParams();
    if (tag) params = params.set('tag', tag);
    if (category) params = params.set('category', category);
    if (q) params = params.set('q', q);
    return this.http.get<Note[]>(this.base, { params });
  }

  // Ghi chú nhanh (chỉ nội dung) — dùng ở trang Nhật ký.
  create(content: string): Observable<Note> {
    return this.http.post<Note>(this.base, { content });
  }

  update(id: string, content: string): Observable<Note> {
    return this.http.put<Note>(`${this.base}/${id}`, { content });
  }

  // Ghi chú kiến thức đầy đủ (tiêu đề/nhóm/thẻ) — dùng ở trang Kiến thức.
  createFull(body: UpsertNoteRequest): Observable<Note> {
    return this.http.post<Note>(this.base, body);
  }

  updateFull(id: string, body: UpsertNoteRequest): Observable<Note> {
    return this.http.put<Note>(`${this.base}/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
