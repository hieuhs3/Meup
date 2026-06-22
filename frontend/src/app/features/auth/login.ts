import { AfterViewInit, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { GOOGLE_CLIENT_ID } from '../../core/api.config';
import { AuthService } from '../../core/services/auth.service';

declare const google: any;

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card">
      <h1>Đăng nhập</h1>
      <p class="muted">Chào mừng quay lại MeUp.</p>

      @if (!twoFactorToken()) {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Email
            <input type="email" formControlName="email" autocomplete="email" />
          </label>
          <label>Mật khẩu
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          @if (error()) { <p class="error">{{ error() }}</p> }

          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
          </button>
        </form>

        @if (googleEnabled) {
          <div class="divider">hoặc</div>
          <div #googleBtn></div>
        }

        <p class="muted"><a routerLink="/forgot-password">Quên mật khẩu?</a></p>
        <p class="muted">Chưa có tài khoản? <a routerLink="/register">Đăng ký</a></p>
      } @else {
        <!-- Bước 2: nhập mã 2FA -->
        <p class="muted">Tài khoản bật xác thực 2 lớp. Nhập mã từ app Authenticator (hoặc mã khôi phục).</p>
        <label>Mã xác thực
          <input type="text" inputmode="numeric" autocomplete="one-time-code"
                 [value]="code()" (input)="code.set($any($event.target).value)" />
        </label>
        @if (error()) { <p class="error">{{ error() }}</p> }
        <button type="button" [disabled]="loading()" (click)="submit2fa()">
          {{ loading() ? 'Đang xác thực…' : 'Xác nhận' }}
        </button>
        <button type="button" class="ghost" (click)="cancel2fa()">Hủy</button>
      }
    </div>
  `,
  styles: [`
    .divider { text-align: center; color: #94a3b8; margin: 1rem 0; }
  `],
})
export class Login implements AfterViewInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly googleBtn = viewChild<ElementRef<HTMLElement>>('googleBtn');
  readonly googleEnabled = !!GOOGLE_CLIENT_ID;

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly twoFactorToken = signal<string | null>(null);
  readonly code = signal('');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  ngAfterViewInit(): void {
    if (!this.googleEnabled || typeof google === 'undefined') return;
    google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (resp: { credential: string }) => this.onGoogle(resp.credential),
    });
    const el = this.googleBtn()?.nativeElement;
    if (el) google.accounts.id.renderButton(el, { theme: 'outline', size: 'large', width: 280 });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (res) => {
        if (res.requiresTwoFactor) {
          this.twoFactorToken.set(res.twoFactorToken);
          this.loading.set(false);
        } else {
          this.router.navigate(['/app']);
        }
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Đăng nhập thất bại. Vui lòng thử lại.');
        this.loading.set(false);
      },
    });
  }

  submit2fa(): void {
    const token = this.twoFactorToken();
    if (!token || !this.code()) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.loginTwoFactor(token, this.code()).subscribe({
      next: () => this.router.navigate(['/app']),
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Mã xác thực không đúng.');
        this.loading.set(false);
      },
    });
  }

  cancel2fa(): void {
    this.twoFactorToken.set(null);
    this.code.set('');
    this.error.set(null);
  }

  private onGoogle(idToken: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.auth.googleLogin(idToken).subscribe({
      next: () => this.router.navigate(['/app']),
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Đăng nhập Google thất bại.');
        this.loading.set(false);
      },
    });
  }
}
