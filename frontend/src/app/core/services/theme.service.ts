import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

/** Quản lý giao diện sáng/tối; lưu vào localStorage, áp vào <html data-theme>. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly KEY = 'meup.theme';
  readonly theme = signal<Theme>('light');

  constructor() {
    const saved = localStorage.getItem(this.KEY);
    this.apply(saved === 'dark' ? 'dark' : 'light');
  }

  apply(t: Theme): void {
    this.theme.set(t);
    document.documentElement.setAttribute('data-theme', t);
    localStorage.setItem(this.KEY, t);
  }

  toggle(): void {
    this.apply(this.theme() === 'dark' ? 'light' : 'dark');
  }
}
