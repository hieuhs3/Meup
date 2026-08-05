# MeUp — Configuration Reference

> Tất cả cấu hình backend: `appsettings.json` + biến môi trường

---

## 1. Cấu trúc cấu hình

ASP.NET Core đọc cấu hình theo thứ tự ưu tiên (sau ghi đè trước):

```
1. appsettings.json          (cơ sở, commit vào git)
2. appsettings.{env}.json    (theo môi trường: Development/Production)
3. Environment variables     (prod: từ docker-compose --env-file)
4. User Secrets              (dev: dotnet user-secrets)
```

**Quy tắc mapping env var → JSON**: `Section__SubSection__Key` = `Section:SubSection:Key`

---

## 2. Backend — `appsettings.json` (đầy đủ)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=meup;Username=meup;Password=meup_dev_password"
  },

  "Jwt": {
    "Issuer": "MeUp.Api",
    "Audience": "MeUp.Client",
    "Key": "CHANGE_ME_dev_only_super_secret_key_at_least_32_chars_long_123456",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },

  "Cors": {
    "Origins": [
      "http://localhost:4200",
      "capacitor://localhost",
      "http://localhost",
      "http://10.0.2.2:4200",
      "http://10.0.2.2"
    ]
  },

  "Authentication": {
    "Google": {
      "ClientId": ""
    }
  },

  "Ai": {
    "ApiKey": ""
  },

  "Email": {
    "Host": "",
    "Port": 587,
    "User": "",
    "Password": "",
    "UseSsl": true,
    "FromEmail": "no-reply@meup.local",
    "FromName": "MeUp",
    "WebBaseUrl": "http://localhost:4200"
  },

  "Seed": {
    "Admin": {
      "Email": "admin@meup.local",
      "Password": "Admin@12345"
    }
  }
}
```

---

## 3. Tham chiếu từng section

### ConnectionStrings

| Key | Mô tả | Dev | Prod |
|-----|-------|-----|------|
| `Default` | PostgreSQL connection string | Postgres port 5433 (tránh xung đột) | `Host=db;Port=5432;Database=...` |

### Jwt

| Key | Mô tả | Dev | Prod |
|-----|-------|-----|------|
| `Issuer` | JWT issuer claim | MeUp.Api | MeUp.Api |
| `Audience` | JWT audience claim | MeUp.Client | MeUp.Client |
| `Key` | HMAC-SHA256 signing key | Dev-only key | **THAY BẰNG RANDOM ≥32 CHARS** |
| `AccessTokenMinutes` | Hạn access token | 15 | 15 |
| `RefreshTokenDays` | Hạn refresh token | 7 | 7 |

> **Bảo mật**: `Key` phải ≥ 32 ký tự ngẫu nhiên. Tạo bằng: `openssl rand -base64 48`

### Cors

| Key | Mô tả |
|-----|-------|
| `Origins` | Mảng domain được phép gọi API |

Prod: Đặt `Cors__Origins__0` = `https://app.yourdomain.com`

### Authentication

| Key | Mô tả | Bắt buộc |
|-----|-------|---------|
| `Authentication:Google:ClientId` | Google OAuth2 Client ID | Không (để trống = ẩn nút Google) |

### Ai

| Key | Mô tả | Bắt buộc |
|-----|-------|---------|
| `Ai:ApiKey` | Anthropic Claude API key (server-wide) | Không (để trống = AI dùng BYO key user) |

### Email

| Key | Mô tả | Default |
|-----|-------|---------|
| `Email:Host` | SMTP host | "" (để trống → ghi log ra file) |
| `Email:Port` | SMTP port | 587 |
| `Email:User` | SMTP username | "" |
| `Email:Password` | SMTP password | "" |
| `Email:UseSsl` | Dùng SSL/TLS | true |
| `Email:FromEmail` | Địa chỉ gửi | no-reply@meup.local |
| `Email:FromName` | Tên gửi | MeUp |
| `Email:WebBaseUrl` | URL frontend (dùng trong link email) | http://localhost:4200 |

**Hành vi Email:**
- Nếu `Email:Host` **rỗng** → `LogEmailSender` (ghi email ra `backend/MeUp.Api/sent-emails/`).
- Nếu `Email:Host` **có giá trị** → `SmtpEmailSender` (gửi thật qua SMTP).

### Seed

| Key | Mô tả | Dev | Prod |
|-----|-------|-----|------|
| `Seed:Admin:Email` | Email tài khoản admin đầu tiên | admin@meup.local | **Đặt qua env var** |
| `Seed:Admin:Password` | Mật khẩu admin đầu tiên | Admin@12345 | **Đặt qua env var** |

---

## 4. Environment Variables (Production)

Mapping từ `docker-compose.prod.yml`:

```bash
# Database
ConnectionStrings__Default=Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}

# JWT
Jwt__Key=${JWT_KEY}
Jwt__Issuer=${JWT_ISSUER:-MeUp.Api}
Jwt__Audience=${JWT_AUDIENCE:-MeUp.Client}

# CORS (FE origin)
Cors__Origins__0=${FRONTEND_ORIGIN}

# Email
Email__WebBaseUrl=${FRONTEND_ORIGIN}
Email__Host=${EMAIL_HOST:-}
Email__Port=${EMAIL_PORT:-587}
Email__User=${EMAIL_USER:-}
Email__Password=${EMAIL_PASSWORD:-}
Email__UseSsl=${EMAIL_USESSL:-true}
Email__FromEmail=${EMAIL_FROM:-no-reply@meup.local}

# Optional features
Ai__ApiKey=${AI_API_KEY:-}
Authentication__Google__ClientId=${GOOGLE_CLIENT_ID:-}

# Admin seed
Seed__Admin__Email=${ADMIN_EMAIL}
Seed__Admin__Password=${ADMIN_PASSWORD}

# ASP.NET
ASPNETCORE_ENVIRONMENT=Production
```

---

## 5. `.env.prod.example`

Template file (commit vào git, không có giá trị thật):

```bash
# === BẮT BUỘC ===
POSTGRES_DB=meup
POSTGRES_USER=meup
POSTGRES_PASSWORD=CHANGE_ME_strong_db_password

JWT_KEY=CHANGE_ME_run_openssl_rand_base64_48
JWT_ISSUER=MeUp.Api
JWT_AUDIENCE=MeUp.Client

FRONTEND_ORIGIN=https://app.yourdomain.com
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=CHANGE_ME_Admin@12345

CLOUDFLARE_TUNNEL_TOKEN=CHANGE_ME_tunnel_token_from_cloudflare

# === TÙY CHỌN ===
# AI (Claude API)
AI_API_KEY=

# Google OAuth2
GOOGLE_CLIENT_ID=

# SMTP email thật
EMAIL_HOST=
EMAIL_PORT=587
EMAIL_USER=
EMAIL_PASSWORD=
EMAIL_USESSL=true
EMAIL_FROM=no-reply@yourdomain.com
```

> **Quan trọng**: File `.env.prod` thật KHÔNG bao giờ commit vào git (đã có trong `.gitignore`).

---

## 6. Dev — User Secrets (thay thế cho env var)

Thay vì đặt giá trị nhạy cảm trong `appsettings.Development.json`, dùng User Secrets:

```bash
cd backend/MeUp.Api

# Đặt Google Client ID (dev)
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"

# Đặt AI API key (dev)
dotnet user-secrets set "Ai:ApiKey" "sk-ant-..."

# Xem tất cả secrets
dotnet user-secrets list
```

Secrets lưu tại `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json` — không ảnh hưởng đến production.

---

## 7. Frontend Environment

File: `frontend/src/environments/environment.ts` (dev):

```typescript
export const environment = {
  production: false,
  apiOrigin: 'http://localhost:5149',  // URL backend dev
  googleClientId: '',
};
```

File: `frontend/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiOrigin: '',  // Same origin: nginx proxy /api → api container
  googleClientId: '',  // Điền nếu muốn bật Google login
};
```

> Với kiến trúc full-docker (nginx proxy), `apiOrigin: ''` là đúng — không cần URL đầy đủ.

---

## 8. Docker Compose Dev

File: `docker-compose.yml` (chỉ DB + Redis cho dev):

```yaml
services:
  db:
    image: postgres:16
    ports:
      - "5433:5432"   # 5433 để tránh xung đột Postgres local
    environment:
      POSTGRES_DB: meup
      POSTGRES_USER: meup
      POSTGRES_PASSWORD: meup_dev_password

  redis:
    image: redis:7
    ports:
      - "6379:6379"
```

Chạy dev:
```bash
docker compose up -d         # khởi động Postgres + Redis
cd backend/MeUp.Api && dotnet run --launch-profile http   # API port 5149
cd frontend && npx ng serve  # FE port 4200
```
