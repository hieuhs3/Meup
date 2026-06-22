import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card">
      <h1>Quên mật khẩu</h1>
      <p class="muted">Nhập email, chúng tôi sẽ gửi liên kết đặt lại mật khẩu.</p>

      @if (sent()) {
        <p class="success">Nếu email tồn tại, liên kết đặt lại đã được gửi. Vui lòng kiểm tra hộp thư.</p>
        <p class="muted"><a routerLink="/login">Về đăng nhập</a></p>
      } @else {
        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>Email <input type="email" formControlName="email" autocomplete="email" /></label>
          @if (error()) { <p class="error">{{ error() }}</p> }
          <button type="submit" [disabled]="form.invalid || loading()">
            {{ loading() ? 'Đang gửi…' : 'Gửi liên kết' }}
          </button>
        </form>
        <p class="muted"><a routerLink="/login">Về đăng nhập</a></p>
      }
    </div>
  `,
})
export class ForgotPassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    this.auth.forgotPassword(this.form.getRawValue().email).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: () => { this.loading.set(false); this.sent.set(true); }, // không lộ lỗi
    });
  }
}
