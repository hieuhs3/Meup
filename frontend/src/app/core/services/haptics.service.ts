import { Injectable } from '@angular/core';
import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';
import { Capacitor } from '@capacitor/core';

/**
 * HapticsService — Wrapper quanh Capacitor Haptics.
 * Chỉ gọi API khi chạy trên native (iOS/Android).
 * Trên web: no-op (không báo lỗi).
 *
 * Cách dùng:
 *   constructor(private haptics: HapticsService) {}
 *   this.haptics.taskDone();    // hoàn thành task
 *   this.haptics.save();        // lưu thành công
 *   this.haptics.error();       // lỗi
 */
@Injectable({ providedIn: 'root' })
export class HapticsService {
  private readonly isNative = Capacitor.isNativePlatform();

  /**
   * Light tap — dùng khi check/uncheck habit, toggle checkbox
   */
  async light(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.impact({ style: ImpactStyle.Light });
    } catch {
      // ignore nếu thiết bị không hỗ trợ
    }
  }

  /**
   * Medium impact — dùng khi mở bottom sheet, swipe confirm
   */
  async medium(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.impact({ style: ImpactStyle.Medium });
    } catch {
      // ignore
    }
  }

  /**
   * Heavy impact — dùng khi hoàn thành task, xóa item
   */
  async heavy(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.impact({ style: ImpactStyle.Heavy });
    } catch {
      // ignore
    }
  }

  /**
   * Lưu thành công — notification success (double tap nhẹ)
   */
  async save(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.notification({ type: NotificationType.Success });
    } catch {
      // ignore
    }
  }

  /**
   * Lỗi — notification error (triple tap nặng)
   */
  async error(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.notification({ type: NotificationType.Error });
    } catch {
      // ignore
    }
  }

  /**
   * Cảnh báo — notification warning
   */
  async warning(): Promise<void> {
    if (!this.isNative) return;
    try {
      await Haptics.notification({ type: NotificationType.Warning });
    } catch {
      // ignore
    }
  }

  // --- Shorthand cho các action phổ biến ---

  /** Hoàn thành task / habit check-in */
  taskDone(): Promise<void> { return this.save(); }

  /** Xóa item */
  deleteItem(): Promise<void> { return this.heavy(); }

  /** Mở sheet / dialog */
  openSheet(): Promise<void> { return this.medium(); }

  /** Tick checkbox */
  tick(): Promise<void> { return this.light(); }
}
