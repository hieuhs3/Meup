import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { API_BASE } from '../api.config';
import { AuthService } from '../services/auth.service';

// Refresh đang chạy được chia sẻ cho mọi request gặp 401 cùng lúc (tránh refresh nhiều lần).
let refresh$: Observable<string> | null = null;

const SKIP_REFRESH = ['/auth/login', '/auth/register', '/auth/refresh'];

/**
 * Gắn Bearer token vào request tới API. Khi gặp 401, thử làm mới token một lần rồi gửi lại;
 * nếu vẫn lỗi thì đăng xuất và chuyển về trang đăng nhập.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApi = req.url.startsWith(API_BASE);
  const token = auth.getAccessToken();
  const authReq = isApi && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      const canRefresh =
        err.status === 401 && isApi && !SKIP_REFRESH.some((p) => req.url.includes(p)) && !!auth.getAccessToken();

      if (!canRefresh) return throwError(() => err);

      if (!refresh$) {
        refresh$ = auth.refresh().pipe(
          switchMap((res) => [res.accessToken]),
          shareReplay(1),
          finalize(() => (refresh$ = null)),
        );
      }

      return refresh$.pipe(
        switchMap((newToken) =>
          next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })),
        ),
        catchError((refreshErr) => {
          auth.clearSession();
          router.navigate(['/login']);
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};
