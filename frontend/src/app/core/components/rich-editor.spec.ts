import { TestBed } from '@angular/core/testing';
import { RichEditor } from './rich-editor';

describe('RichEditor', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [RichEditor] }));

  it('writeValue đổ HTML vào vùng soạn thảo', () => {
    const fixture = TestBed.createComponent(RichEditor);
    fixture.detectChanges();
    fixture.componentInstance.writeValue('<b>xin chào</b>');
    const area = fixture.nativeElement.querySelector('.area') as HTMLElement;
    expect(area.innerHTML).toBe('<b>xin chào</b>');
  });

  it('onInput phát nội dung qua registerOnChange', () => {
    const fixture = TestBed.createComponent(RichEditor);
    fixture.detectChanges();
    const cmp = fixture.componentInstance;
    let captured = '';
    cmp.registerOnChange((v) => (captured = v));

    const area = fixture.nativeElement.querySelector('.area') as HTMLElement;
    area.innerHTML = '<p>nội dung mới</p>';
    cmp.onInput();
    expect(captured).toBe('<p>nội dung mới</p>');
  });

  it('setDisabledState đổi contenteditable', () => {
    const fixture = TestBed.createComponent(RichEditor);
    fixture.detectChanges();
    fixture.componentInstance.setDisabledState(true);
    const area = fixture.nativeElement.querySelector('.area') as HTMLElement;
    expect(area.getAttribute('contenteditable')).toBe('false');
  });
});
