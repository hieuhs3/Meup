import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('meup.theme');
    document.documentElement.removeAttribute('data-theme');
  });

  function make(): ThemeService {
    TestBed.configureTestingModule({});
    return TestBed.inject(ThemeService);
  }

  it('mặc định là light và áp data-theme', () => {
    const t = make();
    expect(t.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('toggle chuyển dark và lưu localStorage', () => {
    const t = make();
    t.toggle();
    expect(t.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem('meup.theme')).toBe('dark');
  });

  it('đọc lại theme đã lưu khi khởi tạo', () => {
    localStorage.setItem('meup.theme', 'dark');
    const t = make();
    expect(t.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
