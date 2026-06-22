# MeUp — Hướng dẫn triển khai (Go-live)

Kiến trúc go-live:

```
Người dùng (HTTPS)
   ├─►  Cloudflare Pages      →  Angular frontend (tĩnh)
   │         │ gọi API
   │         ▼
   └─►  Cloudflare Tunnel  →  meup-api (.NET) ──► PostgreSQL + Redis
            (HTTPS, free)       trên Oracle Always Free VM (docker-compose)
```

- **Frontend**: Cloudflare Pages (miễn phí, CDN toàn cầu).
- **Backend**: Oracle Cloud Always Free VM chạy `docker-compose.prod.yml`
  (API + PostgreSQL + Redis + `cloudflared`).
- **HTTPS cho API**: Cloudflare Tunnel — không cần mở cổng, né hẳn firewall của Oracle.

> Vì sao không để tất cả trên Cloudflare? CF chỉ chạy site tĩnh + Workers (JS/Wasm),
> **không chạy được server .NET, PostgreSQL hay Redis**. Nên backend phải ở VM riêng.

---

## 0. Điều kiện cần (chuẩn bị trước)

- [ ] Tài khoản **Oracle Cloud** (Always Free — cần thẻ tín dụng để xác minh, không trừ tiền).
- [ ] Tài khoản **Cloudflare** + **một domain** đã đưa vào Cloudflare (cần để gắn hostname cho Tunnel,
      vd `api.yourdomain.com`). Chưa có domain thì mua một cái rẻ rồi trỏ nameserver về Cloudflare.
- [ ] Repo đã ở GitHub: `https://github.com/hieuhs3/Meup.git`.

---

## 1. Tạo VM trên Oracle Always Free

1. OCI Console → **Compute → Instances → Create instance**.
2. **Image & shape**: Ubuntu 22.04 (hoặc 24.04); Shape = **Ampere (VM.Standard.A1.Flex)**,
   chọn ~**2 OCPU / 12GB RAM** (vẫn trong hạn mức Always Free 4 OCPU/24GB).
   - Nếu báo hết slot ARM, đổi **Availability Domain**/region rồi thử lại.
3. Thêm **SSH public key** của bạn để đăng nhập.
4. Tạo xong, ghi lại **Public IP**.

SSH vào máy:
```bash
ssh ubuntu@<PUBLIC_IP>
```

### Cài Docker + Compose plugin
```bash
sudo apt-get update && sudo apt-get install -y ca-certificates curl git
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" | sudo tee /etc/apt/sources.list.d/docker.list
sudo apt-get update && sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER && newgrp docker   # chạy docker không cần sudo
docker --version && docker compose version
```

> Không cần mở cổng nào trên Oracle Security List — Cloudflare Tunnel đi ra ngoài (outbound),
> nên firewall mặc định của Oracle vẫn để nguyên.

---

## 2. Tạo Cloudflare Tunnel (HTTPS cho API)

1. Cloudflare dashboard → **Zero Trust → Networks → Tunnels → Create a tunnel**.
2. Loại **Cloudflared**, đặt tên (vd `meup`). Bấm tạo → trang hiện **token** (chuỗi rất dài).
   - **Copy token** này, lát nữa dán vào `.env.prod` (`CLOUDFLARE_TUNNEL_TOKEN`).
3. Ở bước **Public Hostname** của tunnel, thêm:
   - **Subdomain**: `api` · **Domain**: `yourdomain.com` → thành `api.yourdomain.com`
   - **Service**: `HTTP` · `api:8080`  ← trỏ tới container API trong mạng compose.
4. Lưu. (Cloudflare tự tạo bản ghi DNS + cấp SSL cho `api.yourdomain.com`.)

---

## 3. Triển khai backend trên VM

```bash
git clone https://github.com/hieuhs3/Meup.git
cd Meup
cp .env.prod.example .env.prod
nano .env.prod        # điền tất cả giá trị (xem chú thích trong file)
```

Bắt buộc điền đúng trong `.env.prod`:
- `POSTGRES_PASSWORD` — mật khẩu DB mạnh.
- `JWT_KEY` — chuỗi ngẫu nhiên ≥ 32 ký tự (`openssl rand -base64 48`).
- `FRONTEND_ORIGIN` — URL Cloudflare Pages (vd `https://meup.pages.dev` hoặc domain riêng).
- `ADMIN_EMAIL`, `ADMIN_PASSWORD` — tài khoản admin đầu tiên.
- `CLOUDFLARE_TUNNEL_TOKEN` — token ở bước 2.
- (Tùy chọn) `AI_API_KEY`, `GOOGLE_CLIENT_ID`, `EMAIL_*`.

Chạy stack:
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
docker compose -f docker-compose.prod.yml --env-file .env.prod ps
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f api
```
API sẽ tự **migrate DB + seed admin** khi khởi động. Kiểm tra:
```bash
curl https://api.yourdomain.com/api/ai/status   # 401 (cần token) = API sống & qua tunnel OK
```

---

## 4. Triển khai frontend lên Cloudflare Pages

1. Sửa `frontend/src/environments/environment.prod.ts`:
   ```ts
   apiOrigin: 'https://api.yourdomain.com',   // domain API ở bước 2
   ```
   commit & push lên GitHub.
2. Cloudflare dashboard → **Workers & Pages → Create → Pages → Connect to Git** → chọn repo `Meup`.
3. Cấu hình build:
   - **Framework preset**: Angular (hoặc None)
   - **Build command**: `npm ci && npm run build`
   - **Build output directory**: `frontend/dist/frontend/browser`
   - **Root directory**: `frontend`
   - (Pages tự nhận `_redirects` trong `public/` để SPA routing hoạt động.)
4. Deploy. Pages cấp URL dạng `https://meup.pages.dev`.

> URL Pages này phải khớp `FRONTEND_ORIGIN` trong `.env.prod` (CORS). Nếu đổi sang domain riêng,
> cập nhật lại `.env.prod` rồi `docker compose ... up -d` lại API.

---

## 5. Hậu kiểm

- [ ] Mở `https://meup.pages.dev` → đăng nhập bằng `ADMIN_EMAIL`/`ADMIN_PASSWORD`.
- [ ] Tạo thử giao dịch/nhật ký → reload → dữ liệu còn (DB persistent).
- [ ] Trang **Gợi ý AI**: nếu đã set `AI_API_KEY` → "Tạo tổng kết tuần" chạy.

---

## 6. Vận hành

**Cập nhật bản mới** (sau khi push code lên GitHub):
```bash
# Backend (trên VM)
cd ~/Meup && git pull
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
# Frontend: Cloudflare Pages tự build lại mỗi khi push (CD sẵn).
```

**Sao lưu DB:**
```bash
docker exec meup-db pg_dump -U meup meup > backup_$(date +%F).sql
```

**Xem log / khởi động lại:**
```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod logs -f
docker compose -f docker-compose.prod.yml --env-file .env.prod restart api
```

---

## 7. Sự cố thường gặp

| Triệu chứng | Nguyên nhân & xử lý |
|---|---|
| FE gọi API bị **CORS** chặn | `FRONTEND_ORIGIN` trong `.env.prod` chưa khớp URL Pages → sửa rồi `up -d` lại API. |
| FE gọi API **mixed content** (http) | `apiOrigin` trong `environment.prod.ts` phải là **https** (qua tunnel). |
| `curl api...` không phản hồi | Kiểm tra `docker compose logs cloudflared`; xác nhận Public Hostname trỏ `api:8080`. |
| Hết slot máy ARM ở Oracle | Đổi Availability Domain/region, thử lại sau. |
| Avatar mất sau redeploy | Đã mount volume `meup-uploads`; đừng `docker compose down -v` (xóa volume). |

> Lưu ý: free tier không có SLA — hợp demo/cá nhân. Chạy nghiêm túc nên cân nhắc nâng cấp trả phí.
