import { Component, OnInit, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { AdminService } from '../../core/services/admin.service';
import { AdminUser } from '../../core/models/auth.models';

@Component({
  selector: 'app-admin-users',
  template: `
    <header class="page-head"><h1>Quản trị người dùng</h1></header>

    @if (error()) { <p class="error">{{ error() }}</p> }

    <section class="card">
      <table class="table">
        <thead>
          <tr><th>Email</th><th>Tên</th><th>Vai trò</th><th>Trạng thái</th><th></th></tr>
        </thead>
        <tbody>
          @for (u of users(); track u.id) {
            <tr>
              <td>{{ u.email }}</td>
              <td>{{ u.displayName }}</td>
              <td>{{ u.role }}</td>
              <td>
                @if (u.isLocked) { <span class="badge locked">Đã khóa</span> }
                @else { <span class="badge active">Hoạt động</span> }
              </td>
              <td>
                @if (u.id !== auth.user()?.id) {
                  <button class="ghost" (click)="toggle(u)">
                    {{ u.isLocked ? 'Mở khóa' : 'Khóa' }}
                  </button>
                } @else {
                  <span class="muted">(bạn)</span>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
})
export class AdminUsers implements OnInit {
  private readonly admin = inject(AdminService);
  readonly auth = inject(AuthService);

  readonly users = signal<AdminUser[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.admin.listUsers().subscribe({
      next: (list) => this.users.set(list),
      error: (err) => this.error.set(err?.error?.error ?? 'Không tải được danh sách người dùng.'),
    });
  }

  toggle(u: AdminUser): void {
    this.admin.toggleLock(u.id).subscribe({
      next: (res) => this.users.update((list) =>
        list.map((x) => (x.id === res.id ? { ...x, isLocked: res.isLocked } : x)),
      ),
      error: (err) => this.error.set(err?.error?.error ?? 'Thao tác thất bại.'),
    });
  }
}
