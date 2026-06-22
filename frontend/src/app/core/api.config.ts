import { environment } from '../../environments/environment';

/** Origin gốc của backend (dùng để ghép đường dẫn avatar tĩnh). */
export const API_ORIGIN = environment.apiOrigin;

/** URL gốc của backend API. */
export const API_BASE = `${API_ORIGIN}/api`;

/**
 * Google OAuth 2.0 Client ID cho đăng nhập Google.
 * Để trống = ẩn nút "Đăng nhập với Google". Điền Client ID thật để bật.
 */
export const GOOGLE_CLIENT_ID = environment.googleClientId;
