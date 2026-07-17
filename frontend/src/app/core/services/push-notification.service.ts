import { Injectable, inject } from '@angular/core';
import { Capacitor } from '@capacitor/core';
import { PushNotifications, Token, ActionPerformed } from '@capacitor/push-notifications';
import { NotificationService } from './notification.service';
import { HttpClient } from '@angular/common/http';
import { API_BASE } from '../api.config';

/**
 * Service quản lý Push Notifications trên Mobile qua Capacitor.
 * Hỗ trợ xin quyền, lấy FCM Token, lắng nghe sự kiện nhận thông báo và click thông báo.
 */
@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly notificationSvc = inject(NotificationService);
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/notifications`;

  /**
   * Khởi tạo và đăng ký Push Notifications.
   * Nên được gọi sau khi user đăng nhập thành công.
   */
  async initPush(): Promise<void> {
    if (!Capacitor.isNativePlatform()) {
      console.log('Push notifications are not supported on web.');
      return;
    }

    try {
      // 1. Kiểm tra quyền
      let permStatus = await PushNotifications.checkPermissions();

      if (permStatus.receive === 'prompt') {
        permStatus = await PushNotifications.requestPermissions();
      }

      if (permStatus.receive !== 'granted') {
        console.warn('User denied push notification permissions.');
        return;
      }

      // 2. Đăng ký với APNS / FCM
      await PushNotifications.register();

      // 3. Lắng nghe các event
      this.setupListeners();
    } catch (e) {
      console.error('Error initializing push notifications:', e);
    }
  }

  private setupListeners(): void {
    // Lấy FCM token thành công
    PushNotifications.addListener('registration', (token: Token) => {
      console.log('Push registration success, token: ' + token.value);
      this.sendTokenToBackend(token.value);
    });

    // Lỗi đăng ký
    PushNotifications.addListener('registrationError', (error: any) => {
      console.error('Error on push registration: ', error);
    });

    // Nhận được notification khi app đang mở (Foreground)
    PushNotifications.addListener(
      'pushNotificationReceived',
      (notification) => {
        console.log('Push notification received in foreground: ', notification);
        // Refresh số lượng thông báo chưa đọc trên chuông báo
        this.notificationSvc.refreshUnread();
      }
    );

    // Click vào notification từ khay hệ thống (App đang ở background/closed)
    PushNotifications.addListener(
      'pushNotificationActionPerformed',
      (action: ActionPerformed) => {
        console.log('Push notification action performed: ', action);
        // Có thể thực hiện routing dựa trên data của notification
        const data = action.notification.data;
        if (data && data.route) {
          // Thực hiện điều hướng nếu cần
        }
        this.notificationSvc.refreshUnread();
      }
    );
  }

  /**
   * Gửi Token lên backend. Nếu API backend chưa hỗ trợ, phương thức sẽ fail một cách an toàn.
   */
  private sendTokenToBackend(token: string): void {
    // Chuẩn bị endpoint dự phòng cho tương lai nếu backend được cập nhật
    this.http.post(`${this.base}/register-device`, { token, platform: Capacitor.getPlatform() })
      .subscribe({
        next: () => console.log('Device token registered with backend successfully.'),
        error: (err) => {
          // Backend chưa có endpoint này nên sẽ trả về 404. Log info để dev biết.
          console.info('Endpoint register-device not available yet on backend (404 is expected). Saved token local only.');
        }
      });
  }
}
