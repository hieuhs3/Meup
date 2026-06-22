import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SearchService } from './search.service';
import { JournalService } from './journal.service';
import { NoteService } from './note.service';
import { API_BASE } from '../api.config';

describe('SearchService', () => {
  let svc: SearchService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    svc = TestBed.inject(SearchService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('search gắn q vào query', () => {
    svc.search('cà phê').subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE}/search`);
    expect(req.request.params.get('q')).toBe('cà phê');
    req.flush({ items: [], total: 0 });
  });
});

describe('JournalService', () => {
  let svc: JournalService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    svc = TestBed.inject(JournalService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('create POST /journal', () => {
    svc.create({ date: '2026-06-18', title: 'T', contentHtml: '<p>x</p>' }).subscribe();
    const req = http.expectOne(`${API_BASE}/journal`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('delete DELETE /journal/{id}', () => {
    svc.delete('j1').subscribe();
    const req = http.expectOne(`${API_BASE}/journal/j1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});

describe('NoteService', () => {
  let svc: NoteService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    svc = TestBed.inject(NoteService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('create POST /notes với content', () => {
    svc.create('ghi chú').subscribe();
    const req = http.expectOne(`${API_BASE}/notes`);
    expect(req.request.body).toEqual({ content: 'ghi chú' });
    req.flush({});
  });
});
