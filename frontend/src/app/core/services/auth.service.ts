import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE } from '../api.config';
import { AuthResponse, LoginRequest, LoginResponse, RegisterRequest, UserProfile } from '../models/auth.models';

const STORAGE_KEY = 'meup.auth';

interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  user: UserProfile;
}

/**
 * Quản lý trạng thái đăng nhập: token + hồ sơ người dùng.
 * Lưu vào localStorage để giữ phiên khi tải lại trang.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _user = signal<UserProfile | null>(null);
  readonly user = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 'admin');

  private accessToken: string | null = null;
  private refreshToken: string | null = null;

  constructor() {
    this.restore();
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  register(body: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE}/auth/register`, body)
      .pipe(tap((res) => this.setSession(res)));
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE}/auth/login`, body)
      .pipe(tap((res) => res.auth && this.setSession(res.auth)));
  }

  /** Bước 2 của đăng nhập 2FA: gửi token thử thách + mã TOTP/khôi phục. */
  loginTwoFactor(twoFactorToken: string, code: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE}/auth/login/2fa`, { twoFactorToken, code })
      .pipe(tap((res) => res.auth && this.setSession(res.auth)));
  }

  /** Đăng nhập bằng Google ID token (lấy từ Google Identity Services). */
  googleLogin(idToken: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_BASE}/auth/google`, { idToken })
      .pipe(tap((res) => res.auth && this.setSession(res.auth)));
  }

  // --- C2: vòng đời tài khoản ---

  forgotPassword(email: string): Observable<unknown> {
    return this.http.post(`${API_BASE}/auth/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${API_BASE}/auth/reset-password`, { email, token, newPassword });
  }

  confirmEmail(email: string, token: string): Observable<unknown> {
    return this.http.post(`${API_BASE}/auth/confirm-email`, { email, token });
  }

  /** Làm mới access token bằng refresh token hiện có. */
  refresh(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE}/auth/refresh`, { refreshToken: this.refreshToken })
      .pipe(tap((res) => this.setSession(res)));
  }

  logout(): void {
    if (this.refreshToken) {
      // Thu hồi phía server; không chặn việc đăng xuất ở client nếu lỗi.
      this.http.post(`${API_BASE}/auth/logout`, { refreshToken: this.refreshToken }).subscribe({
        error: () => {},
      });
    }
    this.clearSession();
  }

  /** Cập nhật hồ sơ trong store (gọi sau khi sửa hồ sơ thành công). */
  updateUser(user: UserProfile): void {
    this._user.set(user);
    this.persist();
  }

  private setSession(res: AuthResponse): void {
    this.accessToken = res.accessToken;
    this.refreshToken = res.refreshToken;
    this._user.set(res.user);
    this.persist();
  }

  clearSession(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this._user.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private persist(): void {
    if (!this.accessToken || !this.refreshToken || !this._user()) return;
    const data: StoredAuth = {
      accessToken: this.accessToken,
      refreshToken: this.refreshToken,
      user: this._user()!,
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
  }

  private restore(): void {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    try {
      const data = JSON.parse(raw) as StoredAuth;
      this.accessToken = data.accessToken;
      this.refreshToken = data.refreshToken;
      this._user.set(data.user);
    } catch {
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
