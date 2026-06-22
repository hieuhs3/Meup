import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { FinanceService } from './finance.service';
import { API_BASE } from '../api.config';

describe('FinanceService', () => {
  let svc: FinanceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    svc = TestBed.inject(FinanceService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('getTransactions gắn bộ lọc + phân trang vào query', () => {
    svc.getTransactions({ type: 'expense', q: 'cafe', page: 2, pageSize: 20 }).subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE}/finance/transactions`);
    expect(req.request.params.get('type')).toBe('expense');
    expect(req.request.params.get('q')).toBe('cafe');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ items: [], total: 0, page: 2, pageSize: 20 });
  });

  it('createBudget POST đúng body', () => {
    svc.createBudget('cat1', 2000000).subscribe();
    const req = http.expectOne(`${API_BASE}/finance/budgets`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ categoryId: 'cat1', amount: 2000000 });
    req.flush({});
  });

  it('getBudgets truyền month', () => {
    svc.getBudgets('2026-06-15').subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE}/finance/budgets`);
    expect(req.request.params.get('month')).toBe('2026-06-15');
    req.flush([]);
  });

  it('getSummary truyền date', () => {
    svc.getSummary('2026-06-18').subscribe();
    const req = http.expectOne((r) => r.url === `${API_BASE}/finance/summary`);
    expect(req.request.params.get('date')).toBe('2026-06-18');
    req.flush({});
  });
});
