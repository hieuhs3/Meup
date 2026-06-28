import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { JournalService } from '../../core/services/journal.service';
import {
  JournalEntry,
  MOOD_EMOJIS,
  MOOD_LABELS,
  MOODS,
  Mood,
  MoodTrendPoint,
} from '../../core/models/journal.models';
import { NoteService } from '../../core/services/note.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { Note } from '../../core/models/note.models';
import { FormsModule } from '@angular/forms';
import { RichEditor } from '../../core/components/rich-editor';

@Component({
  selector: 'app-journal',
  imports: [ReactiveFormsModule, FormsModule, RichEditor],
  templateUrl: './journal.html',
  styles: [`
    .bar { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: 1rem; }
    .bar label { margin-bottom: 0; }
    .entry { padding: .9rem 0; border-bottom: 1px solid var(--border); }
    .entry-head { display: flex; justify-content: space-between; align-items: baseline; gap: .5rem; }
    .entry h4 { margin: 0; font-size: 1.05rem; }
    .entry .meta { color: var(--muted); font-size: .82rem; white-space: nowrap; }
    .excerpt { color: var(--muted); font-size: .9rem; margin: .35rem 0 .5rem; }
    .full { border-left: 3px solid var(--border); padding-left: .9rem; margin: .4rem 0; }
    .full h1 { font-size: 1.4rem; } .full h2 { font-size: 1.15rem; }
    .full blockquote { border-left: 3px solid var(--primary); margin:.4rem 0; padding:.2rem .8rem; color: var(--muted); }
    .full ul, .full ol { padding-left: 1.4rem; }
    .actions button { padding: .25rem .6rem; font-size: .82rem; margin-left: .35rem; }
    .danger-btn { background: var(--danger); }
    .mood-pick { display: flex; gap: .4rem; flex-wrap: wrap; margin: .3rem 0 .2rem; }
    .mood-pick button { font-size: 1.4rem; line-height: 1; padding: .25rem .45rem; background: #eef1fb; border: 1px solid var(--border); border-radius: 10px; cursor: pointer; }
    .mood-pick button.on { background: var(--primary); border-color: var(--primary); transform: scale(1.08); }
    .mood-pick button .lbl { display: block; font-size: .6rem; color: var(--muted); margin-top: .1rem; }
    .mood-pick button.on .lbl { color: #fff; }
    .trend { display: flex; align-items: flex-end; gap: 3px; height: 90px; padding-top: .4rem; overflow-x: auto; }
    .trend .col { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; min-width: 18px; }
    .trend .col .b { width: 12px; border-radius: 3px 3px 0 0; background: var(--primary); }
    .trend .col .e { font-size: .8rem; }
    .trend .col .d { font-size: .55rem; color: var(--muted); white-space: nowrap; }
    .entry-mood { font-size: 1.1rem; margin-right: .25rem; }
  `],
})
export class Journal implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly journal = inject(JournalService);
  private readonly noteSvc = inject(NoteService);
  private readonly confirm = inject(ConfirmService);

  readonly entries = signal<JournalEntry[]>([]);
  readonly editingId = signal<string | null>(null); // null = không mở; '' = bài mới
  readonly expandedId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly msg = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    date: [this.todayIso(), [Validators.required]],
    title: ['', [Validators.maxLength(200)]],
    contentHtml: [''],
    mood: ['' as Mood | ''],
  });

  readonly filterForm = this.fb.nonNullable.group({ from: [''], to: [''], q: [''] });

  // --- Mood (G2) ---
  readonly moods = MOODS;
  readonly moodTrend = signal<MoodTrendPoint[]>([]);
  moodLabel(m: string | null): string { return m ? MOOD_LABELS[m as Mood] ?? '' : ''; }
  moodEmoji(m: string | null): string { return m ? MOOD_EMOJIS[m as Mood] ?? '' : ''; }
  /** Chọn mood trong form (bấm lại để bỏ chọn). */
  pickMood(m: Mood): void {
    this.form.controls.mood.setValue(this.form.controls.mood.value === m ? '' : m);
  }

  get editorOpen(): boolean {
    return this.editingId() !== null;
  }

  // --- Ghi chú nhanh (A5) ---
  readonly notes = signal<Note[]>([]);
  newNote = '';
  readonly editingNoteId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
    this.loadNotes();
    this.loadMoodTrend();
  }

  private loadMoodTrend(): void {
    this.journal.moodTrend().subscribe({ next: (t) => this.moodTrend.set(t) });
  }

  private loadNotes(): void {
    this.noteSvc.list().subscribe({ next: (n) => this.notes.set(n) });
  }

  addNote(): void {
    const content = this.newNote.trim();
    if (!content) return;
    this.noteSvc.create(content).subscribe({
      next: () => { this.newNote = ''; this.loadNotes(); },
    });
  }

  saveNote(n: Note, content: string): void {
    if (!content.trim()) return;
    this.noteSvc.update(n.id, content.trim()).subscribe({
      next: () => { this.editingNoteId.set(null); this.loadNotes(); },
    });
  }

  async deleteNote(n: Note): Promise<void> {
    if (!(await this.confirm.ask('Xóa ghi chú này?'))) return;
    this.noteSvc.delete(n.id).subscribe({ next: () => this.loadNotes() });
  }

  load(): void {
    const f = this.filterForm.getRawValue();
    this.journal.list(f.from || null, f.to || null, f.q || null).subscribe({
      next: (e) => this.entries.set(e),
      error: () => this.error.set('Không tải được nhật ký.'),
    });
  }

  newEntry(): void {
    this.form.reset({ date: this.todayIso(), title: '', contentHtml: '', mood: '' });
    this.editingId.set('');
    this.msg.set(null);
  }

  edit(e: JournalEntry): void {
    this.form.reset({ date: e.date, title: e.title ?? '', contentHtml: e.contentHtml, mood: e.mood ?? '' });
    this.editingId.set(e.id);
    this.msg.set(null);
  }

  cancel(): void {
    this.editingId.set(null);
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const body = { date: v.date, title: v.title || null, contentHtml: v.contentHtml, mood: v.mood || null };
    const id = this.editingId();
    const req = id ? this.journal.update(id, body) : this.journal.create(body);

    req.subscribe({
      next: () => {
        this.editingId.set(null);
        this.msg.set('Đã lưu nhật ký.');
        this.load();
        this.loadMoodTrend();
      },
      error: (err) => this.error.set(err?.error?.error ?? 'Lưu thất bại.'),
    });
  }

  async remove(e: JournalEntry): Promise<void> {
    if (!(await this.confirm.ask('Xóa bài nhật ký này?'))) return;
    this.journal.delete(e.id).subscribe({
      next: () => { this.load(); this.loadMoodTrend(); },
      error: () => this.error.set('Xóa thất bại.'),
    });
  }

  toggleExpand(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  /** Trích đoạn không định dạng cho danh sách. */
  excerpt(html: string): string {
    const text = html.replace(/<[^>]*>/g, ' ').replace(/&nbsp;/g, ' ').replace(/\s+/g, ' ').trim();
    return text.length > 160 ? text.slice(0, 160) + '…' : text || '(trống)';
  }

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
