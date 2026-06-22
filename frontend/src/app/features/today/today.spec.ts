import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Today } from './today';
import { API_BASE } from '../../core/api.config';

describe('Today (component)', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Today],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  // Trả lời 4 request mà reload() phát ra.
  function flushDay(): void {
    http.expectOne((r) => r.url === `${API_BASE}/finance/summary`)
      .flush({ date: '2026-06-18', balance: 0, dayIncome: 0, dayExpense: 0, monthIncome: 0, monthExpense: 0 });
    http.expectOne((r) => r.url === `${API_BASE}/health/summary`).flush({ today: null, previous: null });
    http.expectOne((r) => r.url === `${API_BASE}/work/summary`)
      .flush({ tasksTotal: 0, tasksDone: 0, tasksOverdue: 0, goalsCount: 0, goalsAvgProgress: 0, habitsTotal: 0, habitsCheckedToday: 0 });
    http.expectOne((r) => r.url === `${API_BASE}/events`).flush([]);
  }

  it('ngOnInit tải tổng quan của 4 mảng', () => {
    const fixture = TestBed.createComponent(Today);
    fixture.detectChanges(); // ngOnInit → reload()
    flushDay();
    const cmp = fixture.componentInstance;
    expect(cmp.finance()).not.toBeNull();
    expect(cmp.work()).not.toBeNull();
    expect(cmp.events()).toEqual([]);
  });

  it('shiftDay đổi ngày và tải lại', () => {
    const fixture = TestBed.createComponent(Today);
    fixture.detectChanges();
    flushDay();
    const cmp = fixture.componentInstance;
    const before = cmp.date();

    cmp.shiftDay(-1);
    flushDay(); // reload phát 4 request mới
    expect(cmp.date()).not.toBe(before);
    expect(cmp.isToday()).toBeFalse();
  });
});
