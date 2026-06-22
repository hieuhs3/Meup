import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Search } from './search';
import { API_BASE } from '../../core/api.config';

describe('Search (component)', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Search],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('run() rỗng thì không gọi API', () => {
    const fixture = TestBed.createComponent(Search);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;
    cmp.q = '   ';
    cmp.run();
    http.expectNone(() => true);
    expect(cmp.result()).toBeNull();
  });

  it('run() gọi search và lưu kết quả', () => {
    const fixture = TestBed.createComponent(Search);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.q = 'cà phê';
    cmp.run();
    const req = http.expectOne((r) => r.url === `${API_BASE}/search`);
    expect(req.request.params.get('q')).toBe('cà phê');
    req.flush({ items: [{ type: 'task', id: 't1', title: 'Mua cà phê', snippet: null, date: null }], total: 1 });

    expect(cmp.result()?.total).toBe(1);
    expect(cmp.lastQuery()).toBe('cà phê');
  });

  it('link() và label() ánh xạ đúng theo loại', () => {
    const fixture = TestBed.createComponent(Search);
    const cmp = fixture.componentInstance;
    expect(cmp.link({ type: 'transaction', id: 'x', title: '', snippet: null, date: null })).toBe('/app/finance');
    expect(cmp.link({ type: 'event', id: 'x', title: '', snippet: null, date: null })).toBe('/app/calendar');
    expect(cmp.label('journal')).toBe('Nhật ký');
  });
});
