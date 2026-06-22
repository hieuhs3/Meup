import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Login } from './login';
import { API_BASE } from '../../core/api.config';

const successResponse = {
  requiresTwoFactor: false,
  twoFactorToken: null,
  auth: {
    accessToken: 'acc', refreshToken: 'ref', accessTokenExpiresAt: '2026-01-01',
    user: { id: 'u1', email: 'a@b.com', displayName: 'A', role: 'user', createdAt: '2026-01-01',
      twoFactorEnabled: false, hasPassword: true, authProviders: [] },
  },
};

describe('Login (component)', () => {
  let http: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });
  afterEach(() => http.verify());

  it('đăng nhập thường (không 2FA) → điều hướng /app', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.form.setValue({ email: 'a@b.com', password: 'x' });
    cmp.submit();

    http.expectOne(`${API_BASE}/auth/login`).flush(successResponse);
    expect(router.navigate).toHaveBeenCalledWith(['/app']);
    expect(cmp.twoFactorToken()).toBeNull();
  });

  it('tài khoản bật 2FA → hiện bước nhập mã, chưa điều hướng', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.form.setValue({ email: 'a@b.com', password: 'x' });
    cmp.submit();
    http.expectOne(`${API_BASE}/auth/login`).flush(
      { requiresTwoFactor: true, twoFactorToken: 'tft', auth: null });

    expect(cmp.twoFactorToken()).toBe('tft');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('hoàn tất bước 2FA → điều hướng /app', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.twoFactorToken.set('tft');
    cmp.code.set('123456');
    cmp.submit2fa();

    const req = http.expectOne(`${API_BASE}/auth/login/2fa`);
    expect(req.request.body).toEqual({ twoFactorToken: 'tft', code: '123456' });
    req.flush(successResponse);
    expect(router.navigate).toHaveBeenCalledWith(['/app']);
  });

  it('hiển thị lỗi khi đăng nhập sai', () => {
    const fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;

    cmp.form.setValue({ email: 'a@b.com', password: 'sai' });
    cmp.submit();
    http.expectOne(`${API_BASE}/auth/login`).flush(
      { error: 'Email hoặc mật khẩu không đúng.' }, { status: 401, statusText: 'Unauthorized' });

    expect(cmp.error()).toBe('Email hoặc mật khẩu không đúng.');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
