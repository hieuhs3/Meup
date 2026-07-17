import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Capacitor } from '@capacitor/core';
import { BehaviorSubject, Observable, from, firstValueFrom } from 'rxjs';

export interface OfflineRequest {
  id: string;
  url: string;
  method: string;
  body: any;
  headers: { [key: string]: string };
  timestamp: number;
}

/**
 * SyncService — Quản lý trạng thái mạng và lưu trữ / gửi lại các request offline.
 */
@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly http = inject(HttpClient);
  private readonly QUEUE_KEY = 'meup.offline.queue';
  private readonly CACHE_PREFIX = 'meup.cache.';

  readonly isOnline = signal<boolean>(true);
  private isSyncing = false;

  constructor() {
    this.initNetworkDetection();
  }

  private initNetworkDetection(): void {
    if (typeof window !== 'undefined') {
      this.isOnline.set(window.navigator.onLine);

      window.addEventListener('online', () => {
        this.isOnline.set(true);
        this.syncQueue();
      });

      window.addEventListener('offline', () => {
        this.isOnline.set(false);
      });
    }
  }

  // --- Caching cho GET requests ---

  setCache(url: string, data: any): void {
    try {
      localStorage.setItem(`${this.CACHE_PREFIX}${url}`, JSON.stringify(data));
    } catch (e) {
      console.warn('Cache write failed:', e);
    }
  }

  getCache(url: string): any | null {
    try {
      const cached = localStorage.getItem(`${this.CACHE_PREFIX}${url}`);
      return cached ? JSON.parse(cached) : null;
    } catch {
      return null;
    }
  }

  // --- Offline Request Queue (POST/PUT/DELETE) ---

  /** Thêm request vào queue để đồng bộ sau */
  enqueue(url: string, method: string, body: any, headers: { [key: string]: string }): void {
    const queue = this.getQueue();
    const offlineReq: OfflineRequest = {
      id: Math.random().toString(36).substring(2, 9),
      url,
      method,
      body,
      headers,
      timestamp: Date.now()
    };
    queue.push(offlineReq);
    this.saveQueue(queue);
    console.info(`Request enqueued offline: ${method} ${url}`);
  }

  /** Đồng bộ các request trong queue lên server khi online */
  async syncQueue(): Promise<void> {
    if (this.isSyncing || !this.isOnline() || this.getQueue().length === 0) return;

    this.isSyncing = true;
    console.info('Starting sync of offline request queue...');
    const queue = this.getQueue();

    while (queue.length > 0) {
      const req = queue[0];
      try {
        await this.executeRequest(req);
        // Thành công: Xóa request đầu tiên ra khỏi queue
        queue.shift();
        this.saveQueue(queue);
      } catch (err) {
        console.error(`Sync failed for request ${req.id}:`, err);
        // Nếu lỗi do mạng bị mất lại, dừng đồng bộ
        if (!window.navigator.onLine) {
          this.isOnline.set(false);
          break;
        }
        // Nếu là lỗi validate/logic (400, 403, 500) mà không phải lỗi mạng,
        // bỏ qua để tránh nghẽn hàng đợi (hoặc log lại)
        queue.shift();
        this.saveQueue(queue);
      }
    }

    this.isSyncing = false;
    console.info('Offline request queue sync process finished.');
  }

  private executeRequest(req: OfflineRequest): Promise<any> {
    const options = {
      headers: req.headers,
      body: req.body
    };
    return firstValueFrom(this.http.request(req.method, req.url, options));
  }

  private getQueue(): OfflineRequest[] {
    try {
      const raw = localStorage.getItem(this.QUEUE_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  private saveQueue(queue: OfflineRequest[]): void {
    try {
      localStorage.setItem(this.QUEUE_KEY, JSON.stringify(queue));
    } catch (e) {
      console.error('Failed to save offline queue:', e);
    }
  }
}
