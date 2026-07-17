import { Injectable, signal, effect } from '@angular/core';
import { Capacitor } from '@capacitor/core';

/**
 * PlatformService — phát hiện môi trường chạy (web / mobile native).
 * Dùng Capacitor.getPlatform() thay vì UserAgent sniffing để chắc chắn.
 */
@Injectable({ providedIn: 'root' })
export class PlatformService {
  /** 'android' | 'ios' | 'web' */
  readonly platform = signal<string>(Capacitor.getPlatform());

  /** true khi chạy trong Capacitor native (Android hoặc iOS) */
  readonly isNative = signal<boolean>(Capacitor.isNativePlatform());

  /** true khi chạy trên Android */
  readonly isAndroid = signal<boolean>(Capacitor.getPlatform() === 'android');

  /** true khi chạy trên iOS */
  readonly isIOS = signal<boolean>(Capacitor.getPlatform() === 'ios');

  /** true khi chạy trên trình duyệt web thông thường */
  readonly isWeb = signal<boolean>(Capacitor.getPlatform() === 'web');

  /**
   * true khi cần hiển thị Bottom Tab Bar thay vì sidebar.
   * Gồm cả trường hợp web trên màn hình nhỏ (mobile browser / PWA).
   */
  readonly useMobileLayout = signal<boolean>(this._detectMobileLayout());

  constructor() {
    // Lắng nghe resize window (khi bật responsive DevTools hoặc tablet xoay)
    if (typeof window !== 'undefined') {
      window.addEventListener('resize', () => {
        this.useMobileLayout.set(this._detectMobileLayout());
      });
    }
  }

  private _detectMobileLayout(): boolean {
    // Native Capacitor → luôn dùng mobile layout
    if (Capacitor.isNativePlatform()) return true;
    // Web → dùng mobile layout khi màn hình nhỏ hơn 768px
    if (typeof window !== 'undefined') {
      return window.innerWidth < 768;
    }
    return false;
  }
}
