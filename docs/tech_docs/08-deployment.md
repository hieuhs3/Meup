# MeUp — Hướng dẫn Triển khai Production

> Docker · Cloudflare Tunnel · Google Cloud Always Free

---

## 1. Kiến trúc Production

```
Người dùng (HTTPS)
   │
   └──► Cloudflare Tunnel → web (nginx) ┬─ /        → Angular (static)
           (HTTPS, free)                 ├─ /api     → api (.NET)
                                         └─ /uploads → api (.NET)
                                                        │
                                            api ──► PostgreSQL + Redis
           Tất cả trong docker-compose.prod.yml trên Google Cloud VM
```

**Ưu điểm:**
- Một origin duy nhất → không cần CORS phức tạp.
- HTTPS miễn phí qua Cloudflare Tunnel (không mở port vào).
- Cụm chạy trên bất kỳ Linux VM nào.

---

## 2. Yêu cầu tiên quyết

- [ ] Tài khoản Google Cloud (Always Free — e2-micro, us-central1/us-west1/us-east1).
- [ ] Tài khoản Cloudflare + 1 domain (có nameserver → Cloudflare).
- [ ] Repo trên GitHub.

---

## 3. Tạo VM (Google Cloud Always Free)

**Specs Free:**
- Machine: `e2-micro` (2 vCPU shared, 1GB RAM)
- Region: `us-central1` hoặc `us-west1` hoặc `us-east1`
- Disk: 30GB Standard

```bash
# Sau khi SSH vào VM:

# Thêm 2GB swap (bắt buộc với 1GB RAM)
sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# Cài Docker + Compose
sudo apt-get update && sudo apt-get install -y ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" | sudo tee /etc/apt/sources.list.d/docker.list
sudo apt-get update && sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER && newgrp docker
```

---

## 4. Tạo Cloudflare Tunnel

1. **Zero Trust → Networks → Tunnels → Create tunnel** → loại Cloudflared → copy **token**.
2. Thêm **Public Hostname**:
   - Subdomain + Domain → `app.yourdomain.com`
   - Service: `HTTP` → `web:80`
3. Cloudflare tự tạo DNS + cấp SSL.

---

## 5. Triển khai cụm

```bash
# Trên VM
git clone https://github.com/hieuhs3/Meup.git && cd Meup
cp .env.prod.example .env.prod
nano .env.prod  # điền tất cả giá trị
```

### `.env.prod` — các biến bắt buộc

```bash
# Database
POSTGRES_DB=meup
POSTGRES_USER=meup
POSTGRES_PASSWORD=<strong-password>

# JWT
JWT_KEY=<openssl rand -base64 48>
JWT_ISSUER=MeUp.Api
JWT_AUDIENCE=MeUp.Client

# App
FRONTEND_ORIGIN=https://app.yourdomain.com
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=<strong-password>

# Cloudflare
CLOUDFLARE_TUNNEL_TOKEN=<tunnel-token>

# Tùy chọn
AI_API_KEY=
GOOGLE_CLIENT_ID=
EMAIL_HOST=
EMAIL_PORT=587
EMAIL_USER=
EMAIL_PASSWORD=
EMAIL_FROM=no-reply@yourdomain.com
```

### Chạy stack

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
docker compose -f docker-compose.prod.yml --env-file .env.prod ps
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f
```

API tự **migrate DB + seed admin** khi khởi động.

---

## 6. Hậu kiểm

```bash
# Kiểm tra app live
curl https://app.yourdomain.com/api/ai/status
# → 401 = API sống

# Kiểm tra frontend
# Mở https://app.yourdomain.com → đăng nhập với ADMIN_EMAIL/ADMIN_PASSWORD
```

---

## 7. Vận hành

### Cập nhật bản mới

```bash
cd ~/Meup && git pull
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

### Sao lưu DB

```bash
docker exec meup-db pg_dump -U meup meup > backup_$(date +%F).sql
```

### Xem logs

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f api
docker compose -f docker-compose.prod.yml --env-file .env.prod restart api
```

---

## 8. Services trong Docker Compose Production

| Service | Image | Port | Vai trò |
|---------|-------|------|---------|
| `db` | postgres:16 | (nội bộ) | PostgreSQL database |
| `redis` | redis:7 | (nội bộ) | Cache/session |
| `api` | build từ ./backend | 8080 (nội bộ) | .NET 9 Web API |
| `web` | build từ ./frontend | 80 (nội bộ) | Nginx + Angular static |
| `cloudflared` | cloudflare/cloudflared | — | Tunnel ra internet |

### Volumes

| Volume | Nội dung | Quan trọng |
|--------|----------|-----------|
| `meup-db-data` | PostgreSQL data | ⚠️ KHÔNG xóa! |
| `meup-uploads` | Avatar + document files | ⚠️ KHÔNG xóa! |
| `meup-keys` | Data Protection keys | ⚠️ KHÔNG xóa! |

> **Cảnh báo**: KHÔNG chạy `docker compose down -v` (xóa volumes = mất toàn bộ dữ liệu).

---

## 9. Troubleshooting

| Triệu chứng | Nguyên nhân & Xử lý |
|-------------|---------------------|
| Trang trắng / 404 | `docker compose logs web` — container web chưa chạy? |
| `/api` lỗi 502 | `docker compose logs api` — DB chưa sẵn sàng? `restart api` |
| Domain không phản hồi | `docker compose logs cloudflared` — tunnel token đúng? hostname → `web:80`? |
| VM bị OOM / kill | Đảm bảo đã bật swap; `docker stats` xem RAM |
| Không tạo được free tier | Phải chọn đúng region + machine type `e2-micro` |
| Avatar mất sau redeploy | Volume `meup-uploads` bị xóa? Đừng dùng `down -v` |

---

## 10. Dockerfile

### Backend (`backend/MeUp.Api/Dockerfile`)

Multi-stage build:
1. **Build stage**: SDK image → `dotnet publish`
2. **Runtime stage**: Runtime-only image → copy publish output

### Frontend (`frontend/Dockerfile`)

Multi-stage build:
1. **Build stage**: Node → `npm run build`
2. **Nginx stage**: Copy `dist/frontend/browser/` → nginx

**Nginx config** (`frontend/nginx.conf`): proxy `/api` và `/uploads` sang `api:8080`.
