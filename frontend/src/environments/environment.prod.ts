/** Môi trường production — dùng khi build cho Cloudflare Pages. */
export const environment = {
  production: true,
  /**
   * Để TRỐNG = cùng origin: FE và API chung domain, nginx (container web) proxy
   * /api và /uploads sang backend. Không cần domain API riêng, không CORS.
   * (Nếu deploy tách rời — vd FE trên Cloudflare Pages — đặt URL API đầy đủ ở đây.)
   */
  apiOrigin: '',
  /** Điền Google OAuth Client ID nếu bật đăng nhập Google. */
  googleClientId: '',
};
