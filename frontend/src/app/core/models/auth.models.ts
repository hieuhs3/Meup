export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  role: string;
  createdAt: string;
  phoneNumber?: string | null;
  dateOfBirth?: string | null; // "yyyy-MM-dd"
  gender?: string | null; // "male" | "female" | "other"
  bio?: string | null;
  avatarUrl?: string | null;
  timeZone?: string | null;
  locale?: string | null;
  twoFactorEnabled: boolean;
  hasPassword: boolean;
  authProviders: string[] | null;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: UserProfile;
}

/** Bao bọc kết quả login: có thể yêu cầu bước 2FA thay vì trả token ngay. */
export interface LoginResponse {
  requiresTwoFactor: boolean;
  twoFactorToken: string | null;
  auth: AuthResponse | null;
}

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isLocked: boolean;
  createdAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** Payload cập nhật hồ sơ mở rộng. */
export interface UpdateProfileRequest {
  displayName: string;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  bio?: string | null;
  timeZone?: string | null;
  locale?: string | null;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface EnableTwoFactorResult {
  recoveryCodes: string[];
}
