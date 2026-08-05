# MeUp — Technical Documentation

> **Phiên bản:** Phase 3 hoàn tất (G1–G8 + G11) · Cập nhật: 2026-08-05  
> **Backend:** 146 test pass · **Frontend:** build sạch

Thư mục này chứa **toàn bộ tài liệu kỹ thuật** của dự án MeUp — một personal life management app full-stack, đa người dùng.

---

## Danh mục tài liệu

| File | Nội dung |
|------|----------|
| [01-system-overview.md](./01-system-overview.md) | Tổng quan kiến trúc hệ thống, tech stack, luồng dữ liệu |
| [02-backend-architecture.md](./02-backend-architecture.md) | Kiến trúc backend .NET 9: Controllers, Services, Entities, DI |
| [03-database-schema.md](./03-database-schema.md) | Schema PostgreSQL: tất cả bảng, quan hệ, index |
| [04-api-reference.md](./04-api-reference.md) | Tham chiếu đầy đủ tất cả REST API endpoints |
| [05-auth-security.md](./05-auth-security.md) | Xác thực & bảo mật: JWT, OAuth2 Google, 2FA TOTP |
| [06-frontend-architecture.md](./06-frontend-architecture.md) | Kiến trúc Angular 20: modules, routing, services, signals |
| [07-mobile-guide.md](./07-mobile-guide.md) | Hướng dẫn mobile Android/iOS với Capacitor |
| [08-deployment.md](./08-deployment.md) | Triển khai production: Docker, Cloudflare Tunnel, GCP |
| [09-configuration.md](./09-configuration.md) | Cấu hình đầy đủ: appsettings, env vars, secrets |
| [10-testing.md](./10-testing.md) | Chiến lược test: xUnit backend, Karma frontend |

---

## Nhanh: Chạy local

```bash
# 1. Hạ tầng (Postgres + Redis)
docker compose up -d

# 2. Backend API → http://localhost:5149
cd backend/MeUp.Api && dotnet run --launch-profile http

# 3. Frontend Angular → http://localhost:4200
cd frontend && npm install && npx ng serve
```

**Admin mặc định:** `admin@meup.local` / `Admin@12345`
