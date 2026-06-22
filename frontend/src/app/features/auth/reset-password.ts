import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card">
      <h1>Đặt lại mật khẩu</h1>
      @if (!email || !token) {
        <p class="error">Liên kết không hợp lệ hoặc thiếu thông tin.</p>
        <p class="muted"><a routerLink="/forgot-password">Yêu cầu liên kết mới</a></p>
      } @else {
        <p class="muted">Đặt mật khẩu mới cho <strong>{{ email }}</strong>.</p>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Mật khẩu mới (tối thiểu 8 ký tự)
            <input type="password" formControlName="newPassword" autocomplete="new-password" />
          </label>
          @if (error()) { <p class="error">{{ error() }}</p> }
          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Đang đặt lại…' : 'Đặt lại mật khẩu' }}
          </button>
        </form>
      }
    </div>
  `,
})
export class ResetPassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly email = this.route.snapshot.queryParamMap.get('email') ?? '';
  readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid || !this.email || !this.token) return;
    this.loading.set(true);
    this.error.set(null);
    this.auth.resetPassword(this.email, this.token, this.form.getRawValue().newPassword).subscribe({
      next: () => this.router.navigate(['/login'], { queryParams: { reset: '1' } }),
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Đặt lại thất bại. Liên kết có thể đã hết hạn.');
        this.loading.set(false);
      },
    });
  }
}
