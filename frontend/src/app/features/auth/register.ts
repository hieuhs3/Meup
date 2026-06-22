import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-card">
      <h1>Đăng ký</h1>
      <p class="muted">Tạo tài khoản MeUp của bạn.</p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>Tên hiển thị
          <input type="text" formControlName="displayName" autocomplete="name" />
        </label>
        <label>Email
          <input type="email" formControlName="email" autocomplete="email" />
        </label>
        <label>Mật khẩu (tối thiểu 8 ký tự)
          <input type="password" formControlName="password" autocomplete="new-password" />
        </label>

        @if (error()) { <p class="error">{{ error() }}</p> }

        <button type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Đang tạo…' : 'Đăng ký' }}
        </button>
      </form>

      <p class="muted">Đã có tài khoản? <a routerLink="/login">Đăng nhập</a></p>
    </div>
  `,
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => this.router.navigate(['/app']),
      error: (err) => {
        this.error.set(err?.error?.error ?? 'Đăng ký thất bại. Vui lòng thử lại.');
        this.loading.set(false);
      },
    });
  }
}
