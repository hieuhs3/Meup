import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-confirm-email',
  imports: [RouterLink],
  template: `
    <div class="auth-card">
      <h1>Xác thực email</h1>
      @switch (state()) {
        @case ('loading') { <p class="muted">Đang xác thực…</p> }
        @case ('ok') { <p class="success">Email đã được xác thực. Cảm ơn bạn!</p> }
        @case ('error') { <p class="error">Xác thực thất bại hoặc liên kết không hợp lệ.</p> }
      }
      <p class="muted"><a routerLink="/login">Về đăng nhập</a></p>
    </div>
  `,
})
export class ConfirmEmail implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly state = signal<'loading' | 'ok' | 'error'>('loading');

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!email || !token) {
      this.state.set('error');
      return;
    }
    this.auth.confirmEmail(email, token).subscribe({
      next: () => this.state.set('ok'),
      error: () => this.state.set('error'),
    });
  }
}
