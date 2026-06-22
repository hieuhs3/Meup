import { Injectable, signal } from '@angular/core';

interface ConfirmState {
  message: string;
  confirmText: string;
  resolve: (ok: boolean) => void;
}

/**
 * Hộp thoại xác nhận dùng chung (thay cho window.confirm).
 * Gọi: `if (await confirm.ask('Xóa?')) { ... }` hoặc `confirm.ask(msg).then(ok => ...)`.
 * Component <app-confirm-dialog/> (đặt 1 lần ở shell) sẽ hiển thị và phản hồi.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly state = signal<ConfirmState | null>(null);

  ask(message: string, confirmText = 'Đồng ý'): Promise<boolean> {
    return new Promise<boolean>((resolve) => this.state.set({ message, confirmText, resolve }));
  }

  answer(ok: boolean): void {
    this.state()?.resolve(ok);
    this.state.set(null);
  }
}
