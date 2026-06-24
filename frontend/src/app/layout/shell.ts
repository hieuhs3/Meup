import { AfterViewInit, Component, ElementRef, HostListener, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { API_ORIGIN } from '../core/api.config';
import { AuthService } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';
import { ConfirmDialog } from '../core/components/confirm-dialog';

interface GlassPos { x: number; y: number; w: number; h: number; }

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialog],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">◆ MeUp</div>
        <nav #navRef (mouseover)="onHover($event)" (mouseleave)="snapActive()">
          <!-- Viên kính trượt theo mục chọn/hover (Liquid Glass, kiểu iOS 26) -->
          <span class="nav-glass" [class.show]="show()"
                [style.transform]="'translate(' + pos().x + 'px,' + pos().y + 'px)'"
                [style.width.px]="pos().w" [style.height.px]="pos().h"></span>
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
export class Shell implements OnInit, AfterViewInit {
  readonly auth = inject(AuthService);
  readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  private readonly navRef = viewChild<ElementRef<HTMLElement>>('navRef');

  readonly pos = signal<GlassPos>({ x: 0, y: 0, w: 0, h: 0 });
  readonly show = signal(false);

  readonly avatarUrl = computed(() => {
    const u = this.auth.user()?.avatarUrl;
    return u ? `${API_ORIGIN}${u}` : null;
  });
  readonly initial = computed(() => (this.auth.user()?.displayName ?? '?').charAt(0).toUpperCase());

  constructor() {
    // Mỗi lần đổi route → viên kính trượt sang mục active mới.
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(() => queueMicrotask(() => this.snapActive()));
  }

  ngOnInit(): void {
    this.notify.refreshUnread();
  }

  ngAfterViewInit(): void {
    queueMicrotask(() => this.snapActive());
  }

  /** Hover lên một mục → kính trượt theo. */
  onHover(e: Event): void {
    const a = (e.target as HTMLElement).closest('a');
    if (a) this.place(a as HTMLElement);
  }

  /** Rời chuột khỏi menu → kính quay về mục đang active. */
  snapActive(): void {
    const nav = this.navRef()?.nativeElement;
    const active = nav?.querySelector('a.active') as HTMLElement | null;
    if (active) this.place(active);
    else this.show.set(false);
  }

  @HostListener('window:resize')
  onResize(): void {
    this.snapActive();
  }

  private place(el: HTMLElement): void {
    this.pos.set({ x: el.offsetLeft, y: el.offsetTop, w: el.offsetWidth, h: el.offsetHeight });
    this.show.set(true);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
