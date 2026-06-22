import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { API_BASE } from '../api.config';
import { AuthResponse, LoginResponse, UserProfile } from '../models/auth.models';

function user(): UserProfile {
  return {
    id: 'u1', email: 'a@b.com', displayName: 'A', role: 'user', createdAt: '2026-01-01',
    twoFactorEnabled: false, hasPassword: true, authProviders: [],
  };
}
function auth(): AuthResponse {
  return { accessToken: 'acc', refreshToken: 'ref', accessTokenExpiresAt: '2026-01-01', user: user() };
}

describe('AuthService', () => {
  let svc: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    svc = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('login đặt phiên khi có auth', () => {
    svc.login({ email: 'a@b.com', password: 'x' }).subscribe();
    const req = http.expectOne(`${API_BASE}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ requiresTwoFactor: false, twoFactorToken: null, auth: auth() } as LoginResponse);

    expect(svc.isLoggedIn()).toBeTrue();
    expect(svc.getAccessToken()).toBe('acc');
    expect(svc.user()?.email).toBe('a@b.com');
  });

  it('login yêu cầu 2FA thì KHÔNG đặt phiên', () => {
    svc.login({ email: 'a@b.com', password: 'x' }).subscribe();
    http.expectOne(`${API_BASE}/auth/login`).flush(
      { requiresTwoFactor: true, twoFactorToken: 'tft', auth: null } as LoginResponse);

    expect(svc.isLoggedIn()).toBeFalse();
    expect(svc.getAccessToken()).toBeNull();
  });

  it('loginTwoFactor gọi đúng endpoint và đặt phiên', () => {
    svc.loginTwoFactor('tft', '123456').subscribe();
    const req = http.expectOne(`${API_BASE}/auth/login/2fa`);
    expect(req.request.body).toEqual({ twoFactorToken: 'tft', code: '123456' });
    req.flush({ requiresTwoFactor: false, twoFactorToken: null, auth: auth() } as LoginResponse);
    expect(svc.isLoggedIn()).toBeTrue();
  });

  it('googleLogin POST /auth/google', () => {
    svc.googleLogin('idtok').subscribe();
    const req = http.expectOne(`${API_BASE}/auth/google`);
    expect(req.request.body).toEqual({ idToken: 'idtok' });
    req.flush({ requiresTwoFactor: false, twoFactorToken: null, auth: auth() } as LoginResponse);
    expect(svc.isAdmin()).toBeFalse();
  });

  it('logout thu hồi và xóa phiên', () => {
    svc.login({ email: 'a@b.com', password: 'x' }).subscribe();
    http.expectOne(`${API_BASE}/auth/login`).flush(
      { requiresTwoFactor: false, twoFactorToken: null, auth: auth() } as LoginResponse);

    svc.logout();
    http.expectOne(`${API_BASE}/auth/logout`).flush({});
    expect(svc.isLoggedIn()).toBeFalse();
    expect(localStorage.getItem('meup.auth')).toBeNull();
  });
});
