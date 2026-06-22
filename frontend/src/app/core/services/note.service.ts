import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { Note } from '../models/note.models';

@Injectable({ providedIn: 'root' })
export class NoteService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/notes`;

  list(): Observable<Note[]> {
    return this.http.get<Note[]>(this.base);
  }

  create(content: string): Observable<Note> {
    return this.http.post<Note>(this.base, { content });
  }

  update(id: string, content: string): Observable<Note> {
    return this.http.put<Note>(`${this.base}/${id}`, { content });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
