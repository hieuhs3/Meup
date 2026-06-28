import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../core/services/document.service';
import { ConfirmService } from '../../core/services/confirm.service';
import {
  DOCUMENT_CATEGORIES,
  DOCUMENT_CATEGORY_LABELS,
  DocumentCategory,
  DocumentItem,
} from '../../core/models/document.models';

@Component({
  selector: 'app-documents',
  imports: [FormsModule],
  template: `
    <header class="page-head">
      <h1>Tài liệu</h1>
      <p class="muted">Lưu CV, chứng chỉ, hợp đồng, hóa đơn… (tối đa 10MB/file).</p>
    </header>
    @if (error()) { <p class="error">{{ error() }}</p> }
    @if (msg()) { <p class="success">{{ msg() }}</p> }

    <section class="card">
      <h3>Tải lên</h3>
      <div class="row">
        <label>Phân loại
          <select [(ngModel)]="uploadCategory">
            @for (c of categories; track c) { <option [value]="c">{{ label(c) }}</option> }
          </select>
        </label>
        <input #fileInput type="file" (change)="onFile($event)" />
        @if (uploading()) { <span class="muted">Đang tải lên…</span> }
      </div>
    </section>

    <section class="card">
      <div class="row" style="justify-content:space-between">
        <h3 style="margin:0">Tài liệu của bạn</h3>
        <label>Lọc
          <select [(ngModel)]="filterCategory" (ngModelChange)="load()">
            <option value="">Tất cả</option>
            @for (c of categories; track c) { <option [value]="c">{{ label(c) }}</option> }
          </select>
        </label>
      </div>

      @for (d of docs(); track d.id) {
        <div class="item">
          <span class="cat">{{ label(d.category) }}</span>
          <span class="grow">{{ d.fileName }}</span>
          <small class="muted">{{ sizeKb(d.size) }} · {{ d.uploadedAt.slice(0, 10) }}</small>
          <button class="ghost" (click)="download(d)">Tải</button>
          <button class="icon-btn danger" (click)="del(d)" aria-label="Xóa">✕</button>
        </div>
      } @empty {
        <p class="muted">Chưa có tài liệu nào.</p>
      }
    </section>
  `,
  styles: [`
    .row { display: flex; gap: .8rem; align-items: flex-end; flex-wrap: wrap; }
    .row label { margin-bottom: 0; }
    .item { display: flex; align-items: center; gap: .6rem; padding: .55rem 0; border-bottom: 1px solid var(--border); }
    .item .grow { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .cat { font-size: .75rem; background: #eef1fb; color: var(--primary); border-radius: 999px; padding: .1rem .55rem; }
  `],
})
export class Documents implements OnInit {
  private readonly docSvc = inject(DocumentService);
  private readonly confirm = inject(ConfirmService);

  readonly docs = signal<DocumentItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly msg = signal<string | null>(null);
  readonly uploading = signal(false);

  readonly categories = DOCUMENT_CATEGORIES;
  uploadCategory: DocumentCategory = 'other';
  filterCategory = '';

  label(c: string): string { return DOCUMENT_CATEGORY_LABELS[c as DocumentCategory] ?? c; }
  sizeKb(bytes: number): string {
    return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)} MB` : `${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.docSvc.list(this.filterCategory || undefined).subscribe({
      next: (d) => this.docs.set(d),
      error: () => this.error.set('Không tải được tài liệu.'),
    });
  }

  onFile(e: Event): void {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.error.set(null);
    this.msg.set(null);
    this.uploading.set(true);
    this.docSvc.upload(file, this.uploadCategory).subscribe({
      next: () => {
        this.uploading.set(false);
        this.msg.set('Đã tải lên.');
        input.value = '';
        this.load();
      },
      error: (err) => {
        this.uploading.set(false);
        input.value = '';
        this.error.set(err?.error?.error ?? 'Tải lên thất bại.');
      },
    });
  }

  download(d: DocumentItem): void {
    this.docSvc.download(d.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = d.fileName;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.error.set('Tải xuống thất bại.'),
    });
  }

  async del(d: DocumentItem): Promise<void> {
    if (!(await this.confirm.ask(`Xóa tài liệu "${d.fileName}"?`))) return;
    this.docSvc.delete(d.id).subscribe({ next: () => this.load() });
  }
}
