import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { NoteService } from '../../core/services/note.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { Note } from '../../core/models/note.models';

@Component({
  selector: 'app-knowledge',
  imports: [ReactiveFormsModule, FormsModule],
  template: `
    <header class="page-head">
      <h1>Kiến thức</h1>
      <p class="muted">Ghi chú có tiêu đề, thẻ và liên kết <code>[[tiêu đề]]</code> (backlinks kiểu Obsidian).</p>
    </header>
    @if (error()) { <p class="error">{{ error() }}</p> }
    @if (msg()) { <p class="success">{{ msg() }}</p> }

    <section class="card">
      <h3>{{ editingId() ? 'Sửa ghi chú' : 'Ghi chú mới' }}</h3>
      <form [formGroup]="form" (ngSubmit)="save()">
        <div class="row">
          <label class="grow">Tiêu đề <input type="text" formControlName="title" maxlength="200" placeholder="vd Docker Compose" /></label>
          <label>Nhóm <input type="text" formControlName="category" maxlength="50" placeholder="vd DevOps" /></label>
        </div>
        <label>Nội dung (dùng <code>[[tiêu đề]]</code> để liên kết)
          <textarea formControlName="content" rows="4" maxlength="5000"
                    style="width:100%; padding:.6rem; border:1px solid var(--border); border-radius:8px; font:inherit; resize:vertical"></textarea>
        </label>
        <label>Thẻ (cách nhau bởi dấu phẩy) <input type="text" formControlName="tags" placeholder="docker, devops" /></label>
        <div style="margin-top:.6rem">
          <button type="submit" [disabled]="form.invalid">{{ editingId() ? 'Lưu' : 'Thêm' }}</button>
          @if (editingId()) { <button type="button" class="ghost" (click)="cancel()">Hủy</button> }
        </div>
      </form>
    </section>

    <section class="card">
      <div class="row" style="justify-content:space-between; align-items:center">
        <h3 style="margin:0">Ghi chú</h3>
        <input type="text" [(ngModel)]="q" (ngModelChange)="load()" placeholder="🔍 tìm…" style="width:auto; margin:0" />
      </div>
      @if (activeTag()) {
        <p class="muted">Lọc theo thẻ: <b>#{{ activeTag() }}</b>
          <button class="ghost" (click)="clearTag()">bỏ lọc</button></p>
      }

      @for (n of notes(); track n.id) {
        <div class="note">
          <div class="note-head">
            <b>{{ n.title || '(không tiêu đề)' }}</b>
            @if (n.category) { <span class="cat">{{ n.category }}</span> }
            <span class="sp"></span>
            <button class="ghost" (click)="edit(n)">Sửa</button>
            <button class="ghost" (click)="del(n)">Xóa</button>
          </div>
          <div class="content">{{ n.content }}</div>
          @if (n.tags.length) {
            <div class="tags">
              @for (t of n.tags; track t) { <button class="tag" (click)="filterTag(t)">#{{ t }}</button> }
            </div>
          }
          @if (n.backlinks.length) {
            <div class="links">↩ Được nhắc bởi:
              @for (b of n.backlinks; track b.id) { <span class="ref">{{ b.title }}</span> }
            </div>
          }
        </div>
      } @empty {
        <p class="muted">Chưa có ghi chú nào.</p>
      }
    </section>
  `,
  styles: [`
    .row { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; }
    .row label { margin-bottom: 0; }
    .row .grow { flex: 1; min-width: 180px; }
    .note { padding: .7rem 0; border-bottom: 1px solid var(--border); }
    .note-head { display: flex; align-items: center; gap: .5rem; }
    .note-head .sp { flex: 1; }
    .note-head .ghost { padding: .2rem .5rem; font-size: .8rem; }
    .cat { font-size: .72rem; background: #eef1fb; color: var(--primary); border-radius: 999px; padding: .1rem .5rem; }
    .content { white-space: pre-wrap; margin: .35rem 0; color: var(--text); }
    .tags { display: flex; gap: .35rem; flex-wrap: wrap; }
    .tag { background: #f0eef9; color: #6b4eaa; border: none; border-radius: 999px; padding: .1rem .5rem; font-size: .75rem; cursor: pointer; }
    .links { font-size: .8rem; color: var(--muted); margin-top: .3rem; }
    .ref { background: #e7f7ec; color: #1a7f37; border-radius: 6px; padding: .05rem .4rem; margin-left: .25rem; }
  `],
})
export class Knowledge implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly noteSvc = inject(NoteService);
  private readonly confirm = inject(ConfirmService);

  readonly notes = signal<Note[]>([]);
  readonly error = signal<string | null>(null);
  readonly msg = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly activeTag = signal<string>('');
  q = '';

  readonly form = this.fb.nonNullable.group({
    title: [''],
    content: ['', [Validators.required, Validators.maxLength(5000)]],
    category: [''],
    tags: [''],
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.noteSvc.list(this.activeTag() || undefined, undefined, this.q || undefined).subscribe({
      next: (n) => this.notes.set(n),
      error: () => this.error.set('Không tải được ghi chú.'),
    });
  }

  filterTag(t: string): void { this.activeTag.set(t); this.load(); }
  clearTag(): void { this.activeTag.set(''); this.load(); }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const body = {
      content: v.content,
      title: v.title || null,
      category: v.category || null,
      tags: v.tags ? v.tags.split(',').map((s) => s.trim()).filter(Boolean) : [],
    };
    const id = this.editingId();
    const req = id ? this.noteSvc.updateFull(id, body) : this.noteSvc.createFull(body);
    req.subscribe({
      next: () => { this.cancel(); this.msg.set('Đã lưu ghi chú.'); this.load(); },
      error: (e) => this.error.set(e?.error?.error ?? 'Lưu thất bại.'),
    });
  }

  edit(n: Note): void {
    this.editingId.set(n.id);
    this.form.setValue({
      title: n.title ?? '',
      content: n.content,
      category: n.category ?? '',
      tags: n.tags.join(', '),
    });
    this.msg.set(null);
  }

  cancel(): void {
    this.editingId.set(null);
    this.form.reset({ title: '', content: '', category: '', tags: '' });
  }

  async del(n: Note): Promise<void> {
    if (!(await this.confirm.ask('Xóa ghi chú này?'))) return;
    this.noteSvc.delete(n.id).subscribe({ next: () => this.load() });
  }
}
