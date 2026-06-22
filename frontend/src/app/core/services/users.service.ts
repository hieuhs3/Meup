import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import {
  EnableTwoFactorResult,
  TwoFactorSetup,
  UpdateProfileRequest,
  UserProfile,
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  getMe(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${API_BASE}/users/me`);
  }

  updateProfile(body: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${API_BASE}/users/me`, body);
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${API_BASE}/users/me/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  changeEmail(newEmail: string, currentPassword: string | null): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${API_BASE}/users/me/change-email`, {
      newEmail,
      currentPassword,
    });
  }

  uploadAvatar(file: File): Observable<UserProfile> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<UserProfile>(`${API_BASE}/users/me/avatar`, form);
  }

  deleteAvatar(): Observable<UserProfile> {
    return this.http.delete<UserProfile>(`${API_BASE}/users/me/avatar`);
  }

  deleteAccount(currentPassword: string | null): Observable<void> {
    return this.http.delete<void>(`${API_BASE}/users/me`, {
      body: { currentPassword },
    });
  }

  // --- 2FA ---

  twoFactorSetup(): Observable<TwoFactorSetup> {
    return this.http.post<TwoFactorSetup>(`${API_BASE}/users/me/2fa/setup`, {});
  }

  twoFactorEnable(code: string): Observable<EnableTwoFactorResult> {
    return this.http.post<EnableTwoFactorResult>(`${API_BASE}/users/me/2fa/enable`, { code });
  }

  twoFactorDisable(currentPassword: string | null): Observable<void> {
    return this.http.post<void>(`${API_BASE}/users/me/2fa/disable`, { currentPassword });
  }
}
