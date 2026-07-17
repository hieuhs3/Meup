import { HttpInterceptorFn, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { of, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { SyncService } from '../services/sync.service';
import { API_BASE } from '../api.config';

/**
 * OfflineInterceptor — Chặn mọi request để hỗ trợ chạy offline.
 * - GET: Lưu cache khi online, lấy cache trả về khi offline.
 * - POST/PUT/DELETE: Cho đi thẳng khi online, đưa vào hàng đợi khi offline.
 */
export const offlineInterceptor: HttpInterceptorFn = (req, next) => {
  const sync = inject(SyncService);

  // Chỉ can thiệp các request gửi tới backend API
  const isApi = req.url.startsWith(API_BASE);
  if (!isApi) return next(req);

  // Tránh cache các request auth liên quan đến đăng nhập/token
  const isAuth = req.url.includes('/auth/');
  if (isAuth) return next(req);

  // ── Xử lý GET Requests (Caching) ───────────────────
  if (req.method === 'GET') {
    if (sync.isOnline()) {
      return next(req).pipe(
        tap(event => {
          if (event instanceof HttpResponse) {
            sync.setCache(req.urlWithParams, event.body);
          }
        })
      );
    } else {
      // Đang offline: Thử lấy dữ liệu từ cache
      const cached = sync.getCache(req.urlWithParams);
      if (cached) {
        console.info(`[Offline Cache] Responding cached data for GET ${req.urlWithParams}`);
        return of(new HttpResponse({
          status: 200,
          body: cached,
          url: req.urlWithParams
        }));
      }

      // Không có cache: Trả về lỗi 503 Service Unavailable
      return throwError(() => new HttpErrorResponse({
        status: 503,
        statusText: 'Service Unavailable (Offline - No Cache)',
        url: req.urlWithParams
      }));
    }
  }

  // ── Xử lý POST/PUT/DELETE Requests (Queueing) ────────
  if (['POST', 'PUT', 'DELETE'].includes(req.method)) {
    if (sync.isOnline()) {
      return next(req);
    } else {
      // Đang offline: Thu thập các headers cần thiết để gửi lại
      const headers: { [key: string]: string } = {};
      req.headers.keys().forEach(key => {
        const val = req.headers.get(key);
        if (val) headers[key] = val;
      });

      // Đưa request vào hàng đợi
      sync.enqueue(req.url, req.method, req.body, headers);

      // Trả về response giả lập thành công (Accepted 202) để tránh crash/lỗi giao diện
      return of(new HttpResponse({
        status: 202,
        body: { offline: true, message: 'Được lưu để đồng bộ khi có mạng.' },
        url: req.url
      }));
    }
  }

  return next(req);
};
