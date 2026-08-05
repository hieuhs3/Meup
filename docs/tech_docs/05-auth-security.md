# MeUp — Xác thực & Bảo mật

> JWT · Google OAuth2 · 2FA TOTP · ASP.NET Core Identity

---

## 1. Tổng quan bảo mật

MeUp sử dụng chiến lược xác thực đa lớp:

1. **JWT (JSON Web Token)** — access token ngắn hạn (15 phút) cho mọi API request.
2. **Refresh Token** — token dài hạn (7 ngày) lưu hash trong DB, xoay vòng khi dùng.
3. **Google OAuth2** — đăng nhập/đăng ký qua Google, không cần mật khẩu.
4. **2FA TOTP** — xác thực hai lớp dùng Google Authenticator / Authy.
5. **Lockout** — khóa tài khoản 15 phút sau 5 lần đăng nhập sai.
6. **Data Isolation** — mọi query đều lọc theo `UserId`.

---

## 2. JWT (Access Token)

### Cấu hình

```json
// appsettings.json
{
  "Jwt": {
    "Issuer": "MeUp.Api",
    "Audience": "MeUp.Client",
    "Key": "CHANGE_ME_secret_key_at_least_32_chars",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

### JWT Claims

| Claim | Giá trị |
|-------|---------|
| `sub` | UserId (Guid) |
| `email` | Email người dùng |
| `name` | DisplayName |
| `role` | "User" hoặc "Admin" |
| `jti` | JWT ID (unique per token) |
| `iat` | Issued at (Unix timestamp) |
| `exp` | Expiry (15 phút từ lúc tạo) |

### Validation parameters

```csharp
new TokenValidationParameters {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwt.Issuer,
    ValidAudience = jwt.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ClockSkew = TimeSpan.FromSeconds(30)  // cho phép 30s sai lệch đồng hồ
}
```

### Token 2FA (purpose-specific JWT)

Khi đăng nhập tài khoản có bật 2FA, server trả `twoFactorToken` là JWT đặc biệt:

| Claim | Giá trị |
|-------|---------|
| `sub` | UserId |
| `purpose` | "twofa" |
| `exp` | 5 phút |

Token này **không** hoạt động cho API nghiệp vụ — chỉ dùng ở endpoint `/api/auth/login/2fa`.

---

## 3. Refresh Token

### Chiến lược

- **Rotation**: Mỗi lần refresh, token cũ bị xóa, token mới được tạo.
- **Lưu hash**: Không lưu token gốc, chỉ lưu SHA-256 hash vào DB → giảm thiệt hại nếu DB bị rò rỉ.
- **Revoke on logout**: Gọi `/api/auth/logout` → thu hồi token ngay.

### Bảng RefreshTokens

```sql
TokenHash    VARCHAR(128)  -- SHA-256 của token gốc
ExpiresAt    TIMESTAMPTZ   -- 7 ngày
RevokedAt    TIMESTAMPTZ   -- null nếu còn hiệu lực
UserId       UUID FK
```

### Luồng xoay vòng (Rotation)

```
1. Client gửi refreshToken + userId
2. Server: SHA-256(refreshToken) → tìm trong DB
3. Kiểm tra: ExpiresAt > now AND RevokedAt IS NULL
4. Xóa bản ghi cũ (hoặc đặt RevokedAt = now)
5. Tạo refresh token mới → lưu hash mới vào DB
6. Tạo access token mới
7. Trả về access + refresh token mới
```

---

## 4. Google OAuth2 (Đăng nhập Google)

### Cấu hình

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com"
    }
  }
}
```

> Mặc định nút "Đăng nhập Google" bị ẩn nếu `ClientId` trống.

### Luồng xác thực

```
[Frontend]                    [Backend]                   [Google]
    │                             │                           │
    ├── Gọi Google Identity ──────────────────────────────────►
    │   Services (JS SDK)         │                           │
    │◄── idToken ─────────────────────────────────────────────┤
    │                             │                           │
    ├── POST /api/auth/google ──►│                           │
    │   { idToken }               │                           │
    │                             ├── GET tokeninfo ──────────►
    │                             │   ?id_token={idToken}     │
    │                             │◄── { email, sub, aud } ───┤
    │                             │                           │
    │                             ├── Validate: aud == ClientId
    │                             ├── Validate: exp còn hạn
    │                             ├── Validate: email_verified
    │                             │                           │
    │                             ├── Tìm user theo email
    │                             │   → Không có: tạo mới (EmailConfirmed=true)
    │                             │   → Có: liên kết external login
    │                             │                           │
    │◄── AuthResponse ────────────┤                           │
```

**Lưu ý kỹ thuật:**
- Dùng `HttpClient` gọi `https://oauth2.googleapis.com/tokeninfo?id_token=...` thay vì NuGet `Google.Apis.Auth` (không cần package bổ sung, build offline an toàn).
- `IGoogleTokenValidator` là interface → mock được trong test.
- Tài khoản Google-only không có `PasswordHash` — khi đổi email/xóa không yêu cầu mật khẩu cũ.

---

## 5. 2FA TOTP (Two-Factor Authentication)

Sử dụng ASP.NET Core Identity built-in TOTP (RFC 6238), tương thích Google Authenticator, Authy, Microsoft Authenticator.

### Setup 2FA

```
1. POST /api/users/me/2fa/setup
   → Server: ResetAuthenticatorKeyAsync (nếu chưa có)
   → GetAuthenticatorKeyAsync
   → Tạo URI: otpauth://totp/MeUp:{email}?secret={key}&issuer=MeUp
   → Trả: { sharedKey, authenticatorUri }

2. User quét QR code bằng app authenticator

3. POST /api/users/me/2fa/enable { code: "123456" }
   → Server: VerifyTwoFactorTokenAsync(user, "Authenticator", code)
   → Đúng: SetTwoFactorEnabledAsync(true)
   → GenerateNewTwoFactorRecoveryCodesAsync(10)
   → Trả: { recoveryCodes: ["abc-def", ...] }  ← chỉ hiện 1 lần!
```

### Đăng nhập với 2FA

```
Bước 1: POST /api/auth/login { email, password }
→ Đúng mật khẩu + TwoFactorEnabled = true
→ Trả: { requiresTwoFactor: true, twoFactorToken: "JWT 5 phút" }

Bước 2: POST /api/auth/login/2fa { twoFactorToken, code }
→ Validate twoFactorToken (signature + exp + purpose="twofa")
→ VerifyTwoFactorTokenAsync(user, "Authenticator", code)
   HOẶC RedeemTwoFactorRecoveryCodeAsync(user, code)
→ Đúng: IssueTokensAsync → AuthResponse đầy đủ
```

### Recovery Codes

- 10 mã khôi phục khi bật 2FA, mỗi mã dùng một lần.
- Dùng `RedeemTwoFactorRecoveryCodeAsync` — Identity tự đánh dấu đã dùng.
- Nếu hết: disable rồi enable lại 2FA để sinh mã mới.

---

## 6. Account Lockout

Cấu hình trong `Program.cs`:

```csharp
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
```

**Hành vi:**
- Đăng nhập sai 5 lần liên tiếp → khóa 15 phút.
- Đăng nhập thành công → reset `AccessFailedCount` về 0.
- Admin có thể khóa vĩnh viễn qua `IsLocked = true` (bypass lockout timer).

---

## 7. Data Protection (API Key Encryption)

ASP.NET Core Data Protection dùng để mã hóa API key Claude cá nhân của user (`EncryptedAiApiKey`):

```csharp
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("keys"))
    .SetApplicationName("MeUp");
```

- Keys lưu tại `backend/MeUp.Api/keys/` (mount volume ở production).
- Đảm bảo key không mất sau khi redeploy → giá trị đã mã hóa vẫn giải mã được.

---

## 8. Password Requirements

```csharp
options.Password.RequiredLength = 8;
options.Password.RequireNonAlphanumeric = false;
// Yêu cầu mặc định: chữ hoa, chữ thường, số
options.User.RequireUniqueEmail = true;
```

---

## 9. CORS Policy

```csharp
.WithOrigins([
    "http://localhost:4200",        // Angular dev
    "capacitor://localhost",         // iOS native
    "http://localhost",              // iOS Simulator
    "http://10.0.2.2:4200",         // Android emulator → Angular dev
    "http://10.0.2.2"               // Android emulator
])
.AllowAnyHeader()
.AllowAnyMethod()
```

Ở production, `Cors:Origins` set từ env var `CORS__ORIGINS__0` = `FRONTEND_ORIGIN`.

---

## 10. Bảo mật khi triển khai

| Vấn đề | Biện pháp |
|--------|-----------|
| JWT key | Đặt `JWT_KEY` qua env var (≥ 32 chars, random) |
| DB password | Đặt `POSTGRES_PASSWORD` qua env var |
| Admin credentials | Đặt `ADMIN_EMAIL`, `ADMIN_PASSWORD` qua env var |
| Google ClientId | Đặt qua env var, KHÔNG commit |
| AI API key | Đặt qua env var hoặc user tự nhập BYO |
| HTTPS | Cloudflare Tunnel → TLS do Cloudflare xử lý |
| Avatar upload | Validate MIME + kích thước; lưu ngoài webroot |
| XSS | Server trả HTML thô từ rich-text (journal), client phải sanitize |

---

## 11. Checklist bảo mật trước go-live

- [ ] Đổi `JWT_KEY` thành chuỗi ngẫu nhiên (`openssl rand -base64 48`)
- [ ] Đổi `ADMIN_PASSWORD` thành mật khẩu mạnh
- [ ] Đổi `POSTGRES_PASSWORD` thành mật khẩu mạnh
- [ ] **Không commit** `.env.prod` vào git
- [ ] Kiểm tra HTTPS hoạt động qua Cloudflare Tunnel
- [ ] Xóa OpenAPI endpoint (`/openapi`) ở production (hiện đã tắt bởi `if (IsDevelopment)`)
- [ ] Kiểm tra `CORS__ORIGINS__0` chính xác domain production
