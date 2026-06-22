import { Component, ElementRef, forwardRef, viewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Bộ soạn thảo rich-text gọn ("mini Word") dựa trên contenteditable + execCommand.
 * Không phụ thuộc thư viện ngoài. Triển khai ControlValueAccessor để dùng với reactive forms.
 * Lưu/đọc nội dung dạng HTML.
 */
@Component({
  selector: 'app-rich-editor',
  standalone: true,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => RichEditor), multi: true },
  ],
  template: `
    <div class="rich-editor">
      <div class="toolbar">
        <button type="button" (mousedown)="cmd($event, 'bold')" title="Đậm"><b>B</b></button>
        <button type="button" (mousedown)="cmd($event, 'italic')" title="Nghiêng"><i>I</i></button>
        <button type="button" (mousedown)="cmd($event, 'underline')" title="Gạch chân"><u>U</u></button>
        <button type="button" (mousedown)="cmd($event, 'strikeThrough')" title="Gạch ngang"><s>S</s></button>
        <span class="sep"></span>
        <button type="button" (mousedown)="format($event, 'H1')" title="Tiêu đề 1">H1</button>
        <button type="button" (mousedown)="format($event, 'H2')" title="Tiêu đề 2">H2</button>
        <button type="button" (mousedown)="format($event, 'P')" title="Đoạn thường">¶</button>
        <span class="sep"></span>
        <button type="button" (mousedown)="cmd($event, 'insertUnorderedList')" title="Danh sách chấm">• ☰</button>
        <button type="button" (mousedown)="cmd($event, 'insertOrderedList')" title="Danh sách số">1. ☰</button>
        <button type="button" (mousedown)="format($event, 'BLOCKQUOTE')" title="Trích dẫn">❝</button>
        <span class="sep"></span>
        <button type="button" (mousedown)="addLink($event)" title="Chèn link">🔗</button>
        <button type="button" (mousedown)="cmd($event, 'removeFormat')" title="Xóa định dạng">✕</button>
      </div>
      <div
        #area
        class="area"
        contenteditable="true"
        (input)="onInput()"
        (blur)="onTouched()"
        [attr.data-placeholder]="placeholder"
      ></div>
    </div>
  `,
  styles: [`
    .rich-editor { border: 1px solid var(--border); border-radius: 10px; overflow: hidden; background: #fff; }
    .toolbar { display: flex; flex-wrap: wrap; gap: .15rem; padding: .4rem; border-bottom: 1px solid var(--border); background: #f8fafc; }
    .toolbar button { background: transparent; color: var(--text); border: 1px solid transparent; border-radius: 6px;
      padding: .25rem .5rem; font-size: .9rem; min-width: 32px; cursor: pointer; }
    .toolbar button:hover { background: #eef1fb; border-color: var(--border); }
    .toolbar .sep { width: 1px; background: var(--border); margin: .2rem .25rem; }
    .area { min-height: 220px; max-height: 460px; overflow-y: auto; padding: .8rem 1rem; font-size: 1rem; line-height: 1.55; outline: none; }
    .area:empty::before { content: attr(data-placeholder); color: var(--muted); }
    .area h1 { font-size: 1.4rem; margin: .4rem 0; }
    .area h2 { font-size: 1.15rem; margin: .4rem 0; }
    .area blockquote { border-left: 3px solid var(--primary); margin: .4rem 0; padding: .2rem .8rem; color: var(--muted); }
    .area ul, .area ol { padding-left: 1.4rem; margin: .4rem 0; }
    .area a { color: var(--primary); }
  `],
})
export class RichEditor implements ControlValueAccessor {
  readonly placeholder = 'Viết nhật ký của bạn…';
  private readonly area = viewChild.required<ElementRef<HTMLElement>>('area');

  private onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  // --- ControlValueAccessor ---

  writeValue(value: string | null): void {
    const el = this.area().nativeElement;
    el.innerHTML = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.area().nativeElement.contentEditable = isDisabled ? 'false' : 'true';
  }

  // --- Soạn thảo ---

  onInput(): void {
    this.onChange(this.area().nativeElement.innerHTML);
  }

  /** Giữ con trỏ trong vùng soạn thảo: mousedown + preventDefault để không mất selection. */
  cmd(event: Event, command: string): void {
    event.preventDefault();
    this.area().nativeElement.focus();
    document.execCommand(command, false);
    this.onInput();
  }

  format(event: Event, tag: string): void {
    event.preventDefault();
    this.area().nativeElement.focus();
    document.execCommand('formatBlock', false, tag);
    this.onInput();
  }

  addLink(event: Event): void {
    event.preventDefault();
    const url = prompt('Nhập đường dẫn (URL):', 'https://');
    if (!url) return;
    this.area().nativeElement.focus();
    document.execCommand('createLink', false, url);
    this.onInput();
  }
}
