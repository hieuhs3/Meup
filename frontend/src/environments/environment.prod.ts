/** Môi trường production — dùng khi build cho Cloudflare Pages. */
export const environment = {
  production: true,
  /**
   * Origin backend thật (Cloudflare Tunnel → API trên Oracle).
   * ĐỔI thành domain API của bạn, ví dụ: https://api.yourdomain.com
   */
  apiOrigin: 'https://api.YOUR_DOMAIN',
  /** Điền Google OAuth Client ID nếu bật đăng nhập Google. */
  googleClientId: '',
};
