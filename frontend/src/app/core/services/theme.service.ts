import { Injectable, signal } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { StatusBar, Style } from '@capacitor/status-bar';

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
    this.updateNativeStatusBar(t);
  }

  toggle(): void {
    this.apply(this.theme() === 'dark' ? 'light' : 'dark');
  }

  private async updateNativeStatusBar(t: Theme): Promise<void> {
    if (!Capacitor.isNativePlatform()) return;
    try {
      if (t === 'dark') {
        await StatusBar.setStyle({ style: Style.Dark });
        if (Capacitor.getPlatform() === 'android') {
          await StatusBar.setBackgroundColor({ color: '#1e1e2e' }); // Màu tối phù hợp với thiết kế dark mode
        }
      } else {
        await StatusBar.setStyle({ style: Style.Light });
        if (Capacitor.getPlatform() === 'android') {
          await StatusBar.setBackgroundColor({ color: '#f4f6fb' }); // Màu sáng phù hợp với light mode
        }
      }
    } catch (e) {
      console.warn('StatusBar error:', e);
    }
  }
}
