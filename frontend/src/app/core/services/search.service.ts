import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import { SearchResult } from '../models/search.models';

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly http = inject(HttpClient);

  search(q: string): Observable<SearchResult> {
    return this.http.get<SearchResult>(`${API_BASE}/search`, { params: new HttpParams().set('q', q) });
  }
}
