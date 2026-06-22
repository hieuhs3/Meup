import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SearchService } from '../../core/services/search.service';
import { SearchHit, SearchResult } from '../../core/models/search.models';

@Component({
  selector: 'app-search',
  imports: [FormsModule, RouterLink],
  template: `
    <header class="page-head">
      <h1>Tìm kiếm</h1>
      <p class="muted">Tìm xuyên giao dịch, nhật ký, công việc và sự kiện.</p>
    </header>

    <form class="search-bar" (ngSubmit)="run()">
      <input type="search" [(ngModel)]="q" name="q" placeholder="Nhập từ khóa…" autofocus />
      <button type="submit">Tìm</button>
    </form>

    @if (result(); as r) {
      <p class="muted">{{ r.total }} kết quả cho "{{ lastQuery() }}"</p>
      <section class="card">
        @for (h of r.items; track h.type + h.id) {
          <a class="hit" [routerLink]="link(h)">
            <span class="badge" [class]="h.type">{{ label(h.type) }}</span>
            <span class="body">
              <span class="title">{{ h.title }}</span>
              @if (h.snippet) { <span class="snip">{{ h.snippet }}</span> }
            </span>
            @if (h.date) { <span class="date">{{ h.date }}</span> }
          </a>
        } @empty {
          <p class="muted">Không tìm thấy kết quả.</p>
        }
      </section>
    }
  `,
  styles: [`
    .search-bar { display: flex; gap: .6rem; margin-bottom: 1rem; }
    .search-bar input { margin: 0; }
    .hit { display: flex; align-items: center; gap: .7rem; padding: .6rem 0; border-bottom: 1px solid var(--border);
      text-decoration: none; color: inherit; }
    .hit:hover { background: #f8fafc; }
    .hit .body { flex: 1; display: flex; flex-direction: column; }
    .hit .title { font-weight: 600; }
    .hit .snip { color: var(--muted); font-size: .85rem; }
    .hit .date { color: var(--muted); font-size: .82rem; white-space: nowrap; }
    .badge { min-width: 78px; text-align: center; }
    .badge.transaction { background: #fde7ea; color: var(--danger); }
    .badge.journal { background: #eef1fb; color: var(--primary); }
    .badge.task { background: #e4f6ec; color: var(--success); }
    .badge.event { background: #fff4e0; color: #b7791f; }
  `],
})
export class Search {
  private readonly search = inject(SearchService);

  q = '';
  readonly result = signal<SearchResult | null>(null);
  readonly lastQuery = signal('');

  run(): void {
    const q = this.q.trim();
    if (!q) return;
    this.lastQuery.set(q);
    this.search.search(q).subscribe({ next: (r) => this.result.set(r) });
  }

  label(type: SearchHit['type']): string {
    return { transaction: 'Giao dịch', journal: 'Nhật ký', task: 'Công việc', event: 'Sự kiện' }[type];
  }

  link(h: SearchHit): string {
    return {
      transaction: '/app/finance',
      journal: '/app/journal',
      task: '/app/work',
      event: '/app/calendar',
    }[h.type];
  }
}
