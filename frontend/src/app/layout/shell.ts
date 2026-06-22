import { Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { API_ORIGIN } from '../core/api.config';
import { AuthService } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';
import { ConfirmDialog } from '../core/components/confirm-dialog';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialog],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">◆ MeUp</div>
        <nav>
          <a routerLink="/app/today" routerLinkActive="active">▣ Hôm nay</a>
          <a routerLink="/app/finance" routerLinkActive="active">💰 Tài chính</a>
          <a routerLink="/app/health" routerLinkActive="active">♥ Sức khỏe</a>
          <a routerLink="/app/work" routerLinkActive="active">✓ Công việc</a>
          <a routerLink="/app/calendar" routerLinkActive="active">📅 Lịch trình</a>
          <a routerLink="/app/journal" routerLinkActive="active">📓 Nhật ký</a>
          <a routerLink="/app/stats" routerLinkActive="active">📊 Thống kê</a>
          <a routerLink="/app/insights" routerLinkActive="active">✨ Gợi ý AI</a>
          <a routerLink="/app/search" routerLinkActive="active">🔍 Tìm kiếm</a>
          <a routerLink="/app/notifications" routerLinkActive="active">🔔 Thông báo
            @if (notify.unread() > 0) { <span class="badge-count">{{ notify.unread() }}</span> }
          </a>
          <a routerLink="/app/profile" routerLinkActive="active">👤 Hồ sơ</a>
          <a routerLink="/app/settings" routerLinkActive="active">🎛 Cài đặt</a>
          @if (auth.isAdmin()) {
            <a routerLink="/app/admin" routerLinkActive="active">⚙ Quản trị</a>
          }
        </nav>
        <div class="sidebar-footer">
          <div class="who">
            @if (avatarUrl()) {
              <img class="avatar-sm" [src]="avatarUrl()" alt="Ảnh đại diện" />
            } @else {
              <span class="avatar-sm placeholder">{{ initial() }}</span>
            }
            <div class="who-text">
              <strong>{{ auth.user()?.displayName }}</strong>
              <span class="muted">{{ auth.user()?.email }}</span>
            </div>
          </div>
          <button class="ghost" (click)="logout()">Đăng xuất</button>
        </div>
      </aside>
      <main class="content">
        <router-outlet />
      </main>
    </div>
    <app-confirm-dialog />
  `,
  styles: [`
    .who { display: flex; align-items: center; gap: .5rem; }
    .who-text { display: flex; flex-direction: column; overflow: hidden; }
    .who-text strong, .who-text span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .avatar-sm { width: 36px; height: 36px; border-radius: 50%; object-fit: cover; flex: 0 0 auto; }
    .avatar-sm.placeholder { display: inline-flex; align-items: center; justify-content: center;
      background: #475569; color: #fff; font-weight: 700; }
    .badge-count { background: var(--danger); color: #fff; border-radius: 999px; font-size: .72rem;
      padding: 0 .4rem; margin-left: .25rem; }
  `],
})
export class Shell implements OnInit {
  readonly auth = inject(AuthService);
  readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  readonly avatarUrl = computed(() => {
    const u = this.auth.user()?.avatarUrl;
    return u ? `${API_ORIGIN}${u}` : null;
  });
  readonly initial = computed(() => (this.auth.user()?.displayName ?? '?').charAt(0).toUpperCase());

  ngOnInit(): void {
    this.notify.refreshUnread();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
