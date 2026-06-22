import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { AppNotification } from '../../core/models/notification.models';

@Component({
  selector: 'app-notifications',
  imports: [DatePipe],
  template: `
    <header class="page-head">
      <h1>Thông báo</h1>
      <p class="muted">Nhắc nhở và cập nhật của bạn.</p>
    </header>

    <div class="bar">
      <button (click)="runReminders()" [disabled]="busy()">🔔 Nhắc ngay</button>
      <button class="ghost" (click)="markAll()">Đánh dấu đã đọc hết</button>
      @if (msg()) { <span class="muted">{{ msg() }}</span> }
    </div>

    <section class="card">
      @for (n of items(); track n.id) {
        <div class="item" [class.unread]="!n.isRead">
          <div class="body" (click)="open(n)">
            <div class="title">{{ n.title }} @if (!n.isRead) { <span class="dot"></span> }</div>
            <div class="msg">{{ n.message }}</div>
            <div class="time">{{ n.createdAt | date: 'short' }}</div>
          </div>
          <button class="ghost" (click)="remove(n)">✕</button>
        </div>
      } @empty {
        <p class="muted">Chưa có thông báo nào. Bấm "Nhắc ngay" để tạo nhắc cho hôm nay.</p>
      }
    </section>
  `,
  styles: [`
    .bar { display: flex; gap: .6rem; align-items: center; margin-bottom: 1rem; }
    .item { display: flex; gap: .6rem; padding: .7rem 0; border-bottom: 1px solid var(--border); }
    .item .body { flex: 1; cursor: pointer; }
    .item.unread .title { font-weight: 700; }
    .title { display: flex; align-items: center; gap: .4rem; }
    .msg { color: var(--muted); font-size: .9rem; margin: .15rem 0; }
    .time { color: var(--muted); font-size: .78rem; }
    .dot { width: 8px; height: 8px; border-radius: 50%; background: var(--primary); display: inline-block; }
  `],
})
export class Notifications implements OnInit {
  private readonly svc = inject(NotificationService);
  private readonly router = inject(Router);

  readonly items = signal<AppNotification[]>([]);
  readonly busy = signal(false);
  readonly msg = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.svc.list().subscribe({ next: (n) => this.items.set(n) });
    this.svc.refreshUnread();
  }

  open(n: AppNotification): void {
    if (!n.isRead) this.svc.markRead(n.id).subscribe({ next: () => this.load() });
    if (n.link) this.router.navigateByUrl(n.link);
  }

  remove(n: AppNotification): void {
    this.svc.delete(n.id).subscribe({ next: () => this.load() });
  }

  markAll(): void {
    this.svc.markAllRead().subscribe({ next: () => this.load() });
  }

  runReminders(): void {
    this.busy.set(true);
    this.svc.runReminders().subscribe({
      next: (r) => {
        this.busy.set(false);
        this.msg.set(r.created ? 'Đã tạo nhắc mới.' : 'Không có nhắc mới (đã nhắc hôm nay hoặc không có gì để nhắc).');
        this.load();
      },
      error: () => this.busy.set(false),
    });
  }
}
