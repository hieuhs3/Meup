/** Môi trường mặc định (dev). Build production sẽ thay bằng environment.prod.ts. */
export const environment = {
  production: false,
  /** Origin backend khi chạy local. */
  apiOrigin: 'http://localhost:5149',
  /** Google OAuth Client ID (để trống = ẩn nút đăng nhập Google). */
  googleClientId: '',
};
