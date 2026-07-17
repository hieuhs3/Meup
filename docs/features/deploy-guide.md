# Hướng dẫn triển khai (Deployment Guide) - MeUp

Tài liệu này hướng dẫn chi tiết quy trình triển khai toàn bộ hệ thống MeUp (Web Frontend + API Backend + Database + Redis) lên máy chủ VPS (môi trường Production) sử dụng Docker Compose và Cloudflare Tunnel.

---

## 1. Yêu cầu hệ thống trên VPS
Máy chủ cần cài đặt:
- Hệ điều hành: Ubuntu 20.04 LTS / 22.04 LTS trở lên.
- **Docker** & **Docker Compose** v2 trở lên.

### Hướng dẫn cài đặt nhanh Docker trên Ubuntu:
```bash
# Cập nhật hệ thống
sudo apt update && sudo apt upgrade -y

# Cài đặt Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Cài đặt Docker Compose
sudo apt install docker-compose-plugin -y

# Chạy Docker không cần quyền root (tùy chọn)
sudo usermod -aG docker $USER
newgrp docker
```

---

## 2. Thiết lập Cloudflare Tunnel (Cung cấp HTTPS miễn phí)
Cloudflare Tunnel giúp expose ứng dụng từ VPS ra internet qua cổng HTTPS an toàn mà không cần mở bất kỳ port nào trên Router/Firewall của VPS.

1. Đăng nhập vào [Cloudflare Dashboard](https://dash.cloudflare.com/).
2. Chọn **Zero Trust > Networks > Tunnels**.
3. Bấm **Create a Tunnel**, chọn loại **Cloudflared** (Connector). đặt tên là `meup-tunnel`.
4. Cloudflare sẽ cấp cho bạn một lệnh cài đặt chứa chuỗi **Tunnel Token**. Sao chép mã Token đó (chuỗi ký tự dài nằm sau `--token`).
5. Quay lại Cloudflare Tunnel Dashboard, thêm cấu hình Route:
   - **Public Hostname**: Ví dụ `meup.yourdomain.com`.
   - **Service**: Chọn `HTTP` và URL trỏ vào container nginx `http://web:80`.
6. Lưu cấu hình.

---

## 3. Cấu hình file `.env.prod`
Trên VPS, tạo file `.env.prod` dựa vào file mẫu `.env.prod.example`:
```bash
cp .env.prod.example .env.prod
nano .env.prod
```

### Các thông số quan trọng cần cập nhật:
- `POSTGRES_PASSWORD`: Nhập mật khẩu cơ sở dữ liệu mạnh.
- `JWT_KEY`: Khóa JWT bí mật (chuỗi dài tối thiểu 32 ký tự). Tạo nhanh bằng lệnh:
  ```bash
  openssl rand -base64 48
  ```
- `FRONTEND_ORIGIN`: Điền domain thật của bạn (ví dụ `https://meup.yourdomain.com`), đây là địa chỉ cho phép gọi CORS và làm gốc gửi email.
- `CLOUDFLARE_TUNNEL_TOKEN`: Điền Token lấy được từ Bước 2.
- `ADMIN_EMAIL` & `ADMIN_PASSWORD`: Thông tin tài khoản Admin khởi tạo lần đầu để quản trị hệ thống.
- **Email SMTP** (Gmail/SendGrid...): Điền các thông số SMTP để kích hoạt tính năng gửi mã xác thực 2FA, khôi phục mật khẩu.

---

## 4. Thực thi Deploy bằng Docker Compose
Sau khi cấu hình xong `.env.prod`, chạy lệnh sau tại thư mục gốc của dự án trên VPS:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

Lệnh này sẽ tự động:
1. Kéo image Postgres 16 và Redis 7 về.
2. Build Image API Backend .NET 9 từ source code.
3. Build Code Frontend Angular 20, đóng gói vào container Nginx để phục vụ file tĩnh.
4. Chạy Cloudflare Tunnel kết nối tới Zero Trust Network.

---

## 5. Kiểm tra & Quản lý Log

Kiểm tra trạng thái các container đang chạy:
```bash
docker compose -f docker-compose.prod.yml ps
```

Xem log hoạt động của hệ thống (ví dụ kiểm tra API):
```bash
docker compose -f docker-compose.prod.yml logs -f api
```

Xem log của Cloudflare Tunnel để biết kết nối thành công chưa:
```bash
docker compose -f docker-compose.prod.yml logs -f cloudflared
```

Dừng hệ thống:
```bash
docker compose -f docker-compose.prod.yml down
```

---

## 6. Lưu ý về sao lưu dữ liệu (Backup)
Các thư mục dữ liệu quan trọng đã được cấu hình lưu vào Docker Volumes để không bị mất khi redeploy:
- `meup-db-data`: Toàn bộ database PostgreSQL.
- `meup-uploads`: Các tệp tin tải lên (avatar của người dùng, tài liệu lưu trữ).
- `meup-keys`: Khóa Data Protection của .NET (dùng để giải mã API keys).

Bạn nên sao lưu thư mục `/var/lib/docker/volumes/` định kỳ để đảm bảo an toàn dữ liệu.
