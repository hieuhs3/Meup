# F0E — Mở rộng Hồ sơ cá nhân & Auth nâng cao (OAuth2 + 2FA)

Mở rộng của **F0** (đã hoàn tất). Không phá vỡ API/luồng cũ — chỉ bổ sung.

Trạng thái pipeline: Yêu cầu ✅ · Thiết kế ✅ · Lập kế hoạch ✅ · Code ✅ · Test ✅ — **HOÀN TẤT**
Ưu tiên: P0 (vẫn thuộc nền tảng auth).

> Đã kiểm chứng end-to-end (API chạy + Postgres): register/login (bao bọc 2FA), hồ sơ mở rộng
> (PUT round-trip cả tiếng Việt, DateOnly, gender), đổi email (chặn sai mật khẩu), upload avatar
> (chặn sai định dạng), 2FA setup → enable (TOTP) → login 2 bước hoàn tất bằng TOTP **và** mã khôi phục
> (mã khôi phục dùng một lần), Google login báo lỗi gọn khi chưa cấu hình ClientId.
>
> **Test tự động: 25/25 pass** = 10 unit (TokenService, gồm token 2FA) + 15 integration
> (`MeUp.Tests/Integration/`, dùng `WebApplicationFactory` + database test `meup_test` + Google validator giả).
> Integration phủ: login wrapper, hồ sơ mở rộng, đổi email (3 case), avatar (2 case),
> Google login (2 case), 2FA TOTP + mã khôi phục dùng một lần, xóa tài khoản (2 case).
> Chạy: cần Postgres ở cổng 5433 (`docker compose up -d`), rồi `cd backend && dotnet test`.

> Chốt phạm vi với chủ dự án:
> - **auth2 = OAuth2 (đăng nhập Google) + 2FA (TOTP)** — làm cả hai.
> - **Hồ sơ**: thông tin mở rộng + upload avatar + đổi email + xóa/vô hiệu hóa tài khoản.

---

# 1. YÊU CẦU (Requirements)

## 1.1 Mục tiêu
Cho người dùng kiểm soát đầy đủ hồ sơ cá nhân và tăng cường an toàn đăng nhập:
đăng nhập bằng Google, bật xác thực 2 lớp (2FA/TOTP), và tự quản lý vòng đời tài khoản.

## 1.2 User stories
- Là người dùng, tôi muốn **bổ sung hồ sơ** (ảnh đại diện, SĐT, ngày sinh, giới tính, tiểu sử, múi giờ, ngôn ngữ).
- Là người dùng, tôi muốn **tải ảnh đại diện** và thấy nó ở khắp ứng dụng.
- Là người dùng, tôi muốn **đổi email** đăng nhập của mình một cách an toàn.
- Là người dùng, tôi muốn **xóa / vô hiệu hóa** tài khoản khi không dùng nữa.
- Là khách, tôi muốn **đăng nhập bằng Google** mà không cần tạo mật khẩu.
- Là người dùng, tôi muốn **bật 2FA (TOTP)** để bảo vệ tài khoản, và có **mã khôi phục**.

## 1.3 Tiêu chí chấp nhận (AC)
- **AC-P1** Hồ sơ hỗ trợ các trường: `displayName, phoneNumber, dateOfBirth, gender, bio, avatarUrl, timeZone, locale`. Tất cả (trừ displayName) là tùy chọn.
- **AC-P2** Upload avatar: chỉ chấp nhận `image/png|jpeg|webp`, ≤ **2 MB**; sai loại/quá lớn → 400 với thông báo tiếng Việt. Ảnh cũ bị thay thế.
- **AC-P3** Đổi email: yêu cầu **mật khẩu hiện tại đúng**; email mới hợp lệ & **chưa được dùng**; trùng → báo lỗi rõ. Đổi xong các phiên cũ vẫn dùng được token đến khi hết hạn (access ngắn hạn).
- **AC-P4** Xóa tài khoản: yêu cầu **mật khẩu hiện tại đúng** (hoặc xác nhận với tài khoản chỉ-Google); thu hồi toàn bộ refresh token; dữ liệu người dùng bị xóa (cascade). Admin **không thể** tự xóa tài khoản admin cuối cùng (giữ nhất quán với ràng buộc tự-khóa của F0).
- **AC-G1** Đăng nhập Google: client gửi **Google ID token**; server **xác thực** token (đúng audience = ClientId, còn hạn, do Google ký).
- **AC-G2** Nếu email Google **chưa có** tài khoản → tạo mới (EmailConfirmed = true, không mật khẩu), gắn `external login (Google)`. Nếu **đã có** → liên kết external login vào tài khoản hiện có rồi đăng nhập.
- **AC-G3** Tài khoản bị khóa (`IsLocked`) → chặn cả đăng nhập Google.
- **AC-T1** Bật 2FA: server cấp **shared key + otpauth URI** (để quét QR); chỉ **bật** sau khi người dùng nhập đúng mã TOTP một lần.
- **AC-T2** Khi bật 2FA, trả về **mã khôi phục** (recovery codes) một lần duy nhất.
- **AC-T3** Đăng nhập (email+mật khẩu) với tài khoản đã bật 2FA → **không** cấp token ngay; trả **thử thách 2FA** (token tạm, hạn ngắn). Bước 2 nhập mã TOTP **hoặc** mã khôi phục đúng → mới cấp access+refresh token.
- **AC-T4** Tắt 2FA yêu cầu **mật khẩu hiện tại đúng**.
- **AC-T5** Trạng thái 2FA (`twoFactorEnabled`) và provider đăng nhập hiển thị trong hồ sơ.
- **AC-X1** Mọi thông báo lỗi/giao diện **tiếng Việt**. Không thay đổi/đập vỡ các endpoint F0 cũ.

## 1.4 Trường hợp biên
- Upload file rỗng / sai MIME / quá lớn / không phải ảnh.
- Đổi email sang email của người khác / email không hợp lệ / sai mật khẩu.
- Google ID token giả/hết hạn/sai audience.
- Tài khoản chỉ-Google (không mật khẩu): đổi mật khẩu / đổi email / xóa cần luồng riêng (xác nhận không cần mật khẩu cũ vì chưa có).
- Bật 2FA khi chưa setup key; nhập mã 2FA sai; token thử thách 2FA hết hạn; dùng lại mã khôi phục đã dùng.

---

# 2. THIẾT KẾ (Design)

## 2.1 Mô hình dữ liệu (mở rộng `ApplicationUser`)
Bổ sung cột (đều **nullable**, không phá dữ liệu cũ):
```
ApplicationUser (kế thừa IdentityUser<Guid>)
  + DateOfBirth   DateOnly?        ngày sinh
  + Gender        string?  (≤20)   "male" | "female" | "other"
  + Bio           string?  (≤500)  tiểu sử ngắn
  + AvatarUrl     string?  (≤256)  đường dẫn tương đối, vd "/uploads/avatars/{id}.png"
  + TimeZone      string?  (≤64)   vd "Asia/Ho_Chi_Minh"
  + Locale        string?  (≤10)   vd "vi"
  (PhoneNumber, TwoFactorEnabled, AuthenticatorKey... đã có sẵn từ Identity)
```
Tận dụng bảng Identity **đã tạo ở migration InitialAuth**: `AspNetUserLogins` (external login),
`AspNetUserTokens` (authenticator key + recovery codes). Không cần bảng mới.

## 2.2 Lưu trữ avatar
- Lưu file tại `wwwroot/uploads/avatars/{userId}{ext}`; phục vụ tĩnh qua `UseStaticFiles`.
- DB chỉ lưu **đường dẫn tương đối**; client ghép với origin API để hiển thị.
- Validate MIME + dung lượng ở controller; ghi đè ảnh cũ của chính user.

## 2.3 API (REST) — bổ sung
| Method | Endpoint | Mô tả | Quyền |
|--------|----------|-------|-------|
| PUT  | `/api/users/me` | Sửa hồ sơ mở rộng (nhiều trường) | đã đăng nhập |
| POST | `/api/users/me/avatar` | Upload avatar (multipart `file`) | đã đăng nhập |
| DELETE | `/api/users/me/avatar` | Xóa avatar | đã đăng nhập |
| POST | `/api/users/me/change-email` | Đổi email (kèm mật khẩu) | đã đăng nhập |
| DELETE | `/api/users/me` | Xóa tài khoản (kèm mật khẩu) | đã đăng nhập |
| POST | `/api/users/me/2fa/setup` | Sinh key + otpauth URI | đã đăng nhập |
| POST | `/api/users/me/2fa/enable` | Bật 2FA (xác minh mã) → recovery codes | đã đăng nhập |
| POST | `/api/users/me/2fa/disable` | Tắt 2FA (kèm mật khẩu) | đã đăng nhập |
| POST | `/api/auth/google` | Đăng nhập/đăng ký bằng Google ID token | công khai |
| POST | `/api/auth/login/2fa` | Hoàn tất đăng nhập 2FA (token tạm + mã) | công khai |

`POST /api/auth/login` **đổi response** thành bao bọc:
```jsonc
// không bật 2FA:
{ "requiresTwoFactor": false, "twoFactorToken": null, "auth": { ...AuthResponse } }
// đã bật 2FA:
{ "requiresTwoFactor": true,  "twoFactorToken": "<jwt ngắn hạn>", "auth": null }
```

## 2.4 Luồng OAuth2 Google
```
[Angular] Google Identity Services → idToken
   └─► POST /api/auth/google { idToken }
          └─► IGoogleTokenValidator: gọi https://oauth2.googleapis.com/tokeninfo?id_token=…
                 kiểm tra: aud == ClientId, exp còn hạn, email_verified
          └─► tìm user theo email → có thì liên kết login, không thì tạo mới
          └─► IssueTokensAsync → access + refresh (giống login thường)
```
> Dùng tokeninfo qua `HttpClient` để **không thêm NuGet** (build offline an toàn).
> `IGoogleTokenValidator` là interface → test mock được, production gọi HTTP thật.

## 2.5 Luồng 2FA (TOTP, dựa trên ASP.NET Identity)
- **Setup**: `ResetAuthenticatorKeyAsync` (nếu chưa có) → `GetAuthenticatorKeyAsync` → tạo `otpauth://totp/MeUp:{email}?secret=…&issuer=MeUp`.
- **Enable**: `VerifyTwoFactorTokenAsync(user, AuthenticatorProvider, code)` đúng → `SetTwoFactorEnabledAsync(true)` → `GenerateNewTwoFactorRecoveryCodesAsync(n=10)`.
- **Login bước 1**: đúng mật khẩu + chưa khóa + `TwoFactorEnabled` → trả **twoFactorToken** = JWT có claim `purpose=twofa`, `sub=userId`, hạn 5 phút (ký cùng khóa Jwt).
- **Login bước 2** `/api/auth/login/2fa`: xác thực twoFactorToken (chữ ký + hạn + purpose) → load user → thử `VerifyTwoFactorTokenAsync` (TOTP) **hoặc** `RedeemTwoFactorRecoveryCodeAsync` → đúng thì `IssueTokensAsync`.
- **Disable**: kiểm tra mật khẩu → `SetTwoFactorEnabledAsync(false)` + `ResetAuthenticatorKeyAsync`.

## 2.6 Bảo mật & lưu ý
- Avatar: chặn theo MIME thực + đuôi an toàn; giới hạn kích thước qua `[RequestSizeLimit]`.
- Token thử thách 2FA: hạn ngắn (5'), claim `purpose` để **không** dùng được như access token (middleware vẫn yêu cầu token thường cho API nghiệp vụ).
- Tài khoản chỉ-Google: `HasPasswordAsync == false` → đổi email/xóa **không** đòi mật khẩu cũ (đòi xác nhận chuỗi "XÓA"/đúng email thay thế).
- Không trả `PasswordHash`, `AuthenticatorKey`, recovery codes (trừ lúc enable) ra DTO.
- Google ClientId đọc từ `Authentication:Google:ClientId` (env/user-secrets ở production).

## 2.7 Frontend (Angular)
- `UserProfile` mở rộng + `twoFactorEnabled`, `hasPassword`, `authProviders`.
- `users.service`: `updateProfile`, `uploadAvatar`, `deleteAvatar`, `changeEmail`, `deleteAccount`, `twoFactorSetup/Enable/Disable`.
- `auth.service`: `login` trả `LoginResponse`; thêm `loginTwoFactor`, `googleLogin`.
- Trang **Hồ sơ**: form mở rộng + khối avatar + đổi email + khối 2FA (QR/secret + mã + recovery codes) + nút xóa tài khoản (xác nhận).
- Trang **Đăng nhập**: bước nhập mã 2FA khi `requiresTwoFactor`; nút **Đăng nhập với Google** (Google Identity Services trong `index.html`).
- `shell`: hiển thị avatar cạnh tên.

---

# 3. LẬP KẾ HOẠCH (Plan)

| # | Task | Phạm vi | Phụ thuộc | Lớn |
|---|------|---------|-----------|-----|
| E1 | Mở rộng entity + DTO + migration | backend | — | M |
| E2 | UsersController: update hồ sơ, avatar, đổi email, xóa tài khoản + static files | backend | E1 | L |
| E3 | IGoogleTokenValidator + AuthService.GoogleLoginAsync + endpoint | backend | E1 | M |
| E4 | Token 2FA + 2FA endpoints + sửa luồng login | backend | E1 | L |
| E5 | Models + services + trang Hồ sơ mở rộng (FE) | frontend | E2–E4 | L |
| E6 | Login 2 bước + nút Google (FE) | frontend | E4, E3 | M |
| E7 | Unit test (token 2FA, Google validator) + build xanh | backend/test | E3, E4 | M |
| E8 | Cập nhật README + 03-feature-plan | docs | mọi task | S |

**Rủi ro / quyết định:**
- Xóa tài khoản: chọn **xóa cứng** (cascade) cho MVP; có thể đổi sang "vô hiệu hóa mềm" (`IsDeactivated`) sau nếu cần khôi phục.
- Đổi email: bỏ qua bước gửi email xác thực ở MVP (chưa có hạ tầng mail) — đổi trực tiếp sau khi xác minh mật khẩu; ghi chú nâng cấp ở P2.
- Google: dùng tokeninfo (đơn giản, không thêm package) thay cho `Google.Apis.Auth`.

**DoD:** build backend + `dotnet test` xanh; `ng build` sạch; các AC ở mục 1.3 đạt khi chạy thủ công.
