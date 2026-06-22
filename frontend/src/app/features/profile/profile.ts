import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { API_ORIGIN } from '../../core/api.config';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { TwoFactorSetup } from '../../core/models/auth.models';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule],
  template: `
    <header class="page-head"><h1>Hồ sơ</h1></header>

    <!-- Ảnh đại diện -->
    <section class="card">
      <h3>Ảnh đại diện</h3>
      <div class="avatar-row">
        @if (avatarUrl()) {
          <img class="avatar-lg" [src]="avatarUrl()" alt="Ảnh đại diện" />
        } @else {
          <div class="avatar-lg placeholder">{{ initial() }}</div>
        }
        <div>
          <input #fileInput type="file" accept="image/png,image/jpeg,image/webp" (change)="onFile(fileInput)" />
          <p class="muted">PNG, JPEG hoặc WebP, tối đa 2 MB.</p>
          @if (auth.user()?.avatarUrl) {
            <button type="button" class="ghost" (click)="removeAvatar()">Xóa ảnh</button>
          }
          @if (avatarMsg()) { <p [class]="avatarOk() ? 'success' : 'error'">{{ avatarMsg() }}</p> }
        </div>
      </div>
    </section>

    <!-- Thông tin cá nhân -->
    <section class="card">
      <h3>Thông tin cá nhân</h3>
      <p class="muted">Email: {{ auth.user()?.email }} · Vai trò: {{ auth.user()?.role }}</p>

      <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
        <label>Tên hiển thị
          <input type="text" formControlName="displayName" />
        </label>
        <label>Số điện thoại
          <input type="tel" formControlName="phoneNumber" />
        </label>
        <label>Ngày sinh
          <input type="date" formControlName="dateOfBirth" />
        </label>
        <label>Giới tính
          <select formControlName="gender">
            <option value="">— Không nói —</option>
            <option value="male">Nam</option>
            <option value="female">Nữ</option>
            <option value="other">Khác</option>
          </select>
        </label>
        <label>Múi giờ
          <input type="text" formControlName="timeZone" placeholder="Asia/Ho_Chi_Minh" />
        </label>
        <label>Ngôn ngữ
          <input type="text" formControlName="locale" placeholder="vi" />
        </label>
        <label>Tiểu sử
          <textarea formControlName="bio" rows="3" maxlength="500"></textarea>
        </label>
        @if (profileMsg()) { <p [class]="profileOk() ? 'success' : 'error'">{{ profileMsg() }}</p> }
        <button type="submit" [disabled]="profileForm.invalid">Lưu hồ sơ</button>
      </form>
    </section>

    <!-- Đổi email -->
    <section class="card">
      <h3>Đổi email</h3>
      <form [formGroup]="emailForm" (ngSubmit)="changeEmail()">
        <label>Email mới
          <input type="email" formControlName="newEmail" autocomplete="email" />
        </label>
        @if (auth.user()?.hasPassword) {
          <label>Mật khẩu hiện tại
            <input type="password" formControlName="currentPassword" autocomplete="current-password" />
          </label>
        }
        @if (emailMsg()) { <p [class]="emailOk() ? 'success' : 'error'">{{ emailMsg() }}</p> }
        <button type="submit" [disabled]="emailForm.invalid">Đổi email</button>
      </form>
    </section>

    <!-- Đổi mật khẩu (chỉ tài khoản có mật khẩu) -->
    @if (auth.user()?.hasPassword) {
      <section class="card">
        <h3>Đổi mật khẩu</h3>
        <form [formGroup]="passwordForm" (ngSubmit)="changePassword()">
          <label>Mật khẩu hiện tại
            <input type="password" formControlName="currentPassword" autocomplete="current-password" />
          </label>
          <label>Mật khẩu mới (tối thiểu 8 ký tự)
            <input type="password" formControlName="newPassword" autocomplete="new-password" />
          </label>
          @if (passwordMsg()) { <p [class]="passwordOk() ? 'success' : 'error'">{{ passwordMsg() }}</p> }
          <button type="submit" [disabled]="passwordForm.invalid">Đổi mật khẩu</button>
        </form>
      </section>
    }

    <!-- Xác thực 2 lớp (2FA) -->
    <section class="card">
      <h3>Xác thực 2 lớp (2FA)</h3>

      @if (auth.user()?.twoFactorEnabled) {
        <p class="success">✓ 2FA đang bật.</p>
        @if (auth.user()?.hasPassword) {
          <label>Mật khẩu hiện tại để tắt
            <input type="password" [value]="disablePwd()" (input)="disablePwd.set($any($event.target).value)" />
          </label>
        }
        @if (twoFaMsg()) { <p [class]="twoFaOk() ? 'success' : 'error'">{{ twoFaMsg() }}</p> }
        <button type="button" class="ghost" (click)="disable2fa()">Tắt 2FA</button>
      } @else if (setup()) {
        <p class="muted">1. Mở app Authenticator và quét QR hoặc nhập khóa thủ công:</p>
        <p><strong>Khóa:</strong> <code>{{ setup()!.sharedKey }}</code></p>
        <p class="muted">URI: <code style="word-break:break-all">{{ setup()!.authenticatorUri }}</code></p>
        <label>2. Nhập mã 6 số từ app
          <input type="text" inputmode="numeric" autocomplete="one-time-code"
                 [value]="enableCode()" (input)="enableCode.set($any($event.target).value)" />
        </label>
        @if (twoFaMsg()) { <p [class]="twoFaOk() ? 'success' : 'error'">{{ twoFaMsg() }}</p> }
        <button type="button" (click)="enable2fa()">Bật 2FA</button>

        @if (recoveryCodes().length) {
          <div class="recovery">
            <p class="success">Đã bật 2FA. Lưu lại các mã khôi phục (hiển thị một lần):</p>
            <ul>@for (c of recoveryCodes(); track c) { <li><code>{{ c }}</code></li> }</ul>
          </div>
        }
      } @else {
        <p class="muted">Tăng bảo mật bằng mã OTP từ app Authenticator (Google Authenticator, Authy…).</p>
        <button type="button" (click)="startSetup()">Bật 2FA</button>
      }
    </section>

    <!-- Xóa tài khoản -->
    <section class="card danger">
      <h3>Xóa tài khoản</h3>
      <p class="muted">Hành động này không thể hoàn tác. Mọi dữ liệu của bạn sẽ bị xóa vĩnh viễn.</p>
      @if (auth.user()?.hasPassword) {
        <label>Mật khẩu hiện tại
          <input type="password" [value]="deletePwd()" (input)="deletePwd.set($any($event.target).value)" />
        </label>
      }
      @if (deleteMsg()) { <p class="error">{{ deleteMsg() }}</p> }
      @if (!confirmDelete()) {
        <button type="button" class="danger-btn" (click)="confirmDelete.set(true)">Xóa tài khoản</button>
      } @else {
        <p class="error">Bạn chắc chắn?</p>
        <button type="button" class="danger-btn" (click)="deleteAccount()">Xác nhận xóa</button>
        <button type="button" class="ghost" (click)="confirmDelete.set(false)">Hủy</button>
      }
    </section>
  `,
  styles: [`
    .avatar-row { display: flex; gap: 1rem; align-items: flex-start; }
    .avatar-lg { width: 88px; height: 88px; border-radius: 50%; object-fit: cover; }
    .avatar-lg.placeholder { display: flex; align-items: center; justify-content: center;
      background: #e2e8f0; color: #475569; font-size: 2rem; font-weight: 700; }
    .recovery ul { columns: 2; }
    .card.danger { border: 1px solid #fecaca; }
    .danger-btn { background: #dc2626; color: #fff; }
  `],
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  private readonly users = inject(UsersService);
  readonly auth = inject(AuthService);

  readonly avatarUrl = computed(() => {
    const u = this.auth.user()?.avatarUrl;
    return u ? `${API_ORIGIN}${u}` : null;
  });
  readonly initial = computed(() => (this.auth.user()?.displayName ?? '?').charAt(0).toUpperCase());

  readonly profileMsg = signal<string | null>(null);
  readonly profileOk = signal(false);
  readonly passwordMsg = signal<string | null>(null);
  readonly passwordOk = signal(false);
  readonly emailMsg = signal<string | null>(null);
  readonly emailOk = signal(false);
  readonly avatarMsg = signal<string | null>(null);
  readonly avatarOk = signal(false);

  readonly setup = signal<TwoFactorSetup | null>(null);
  readonly enableCode = signal('');
  readonly recoveryCodes = signal<string[]>([]);
  readonly disablePwd = signal('');
  readonly twoFaMsg = signal<string | null>(null);
  readonly twoFaOk = signal(false);

  readonly confirmDelete = signal(false);
  readonly deletePwd = signal('');
  readonly deleteMsg = signal<string | null>(null);

  private readonly u = this.auth.user();

  readonly profileForm = this.fb.nonNullable.group({
    displayName: [this.u?.displayName ?? '', [Validators.required, Validators.maxLength(100)]],
    phoneNumber: [this.u?.phoneNumber ?? ''],
    dateOfBirth: [this.u?.dateOfBirth ?? ''],
    gender: [this.u?.gender ?? ''],
    timeZone: [this.u?.timeZone ?? ''],
    locale: [this.u?.locale ?? ''],
    bio: [this.u?.bio ?? '', [Validators.maxLength(500)]],
  });

  readonly emailForm = this.fb.nonNullable.group({
    newEmail: ['', [Validators.required, Validators.email]],
    currentPassword: [''],
  });

  readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
  });

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    const v = this.profileForm.getRawValue();
    this.users
      .updateProfile({
        displayName: v.displayName,
        phoneNumber: v.phoneNumber || null,
        dateOfBirth: v.dateOfBirth || null,
        gender: v.gender || null,
        timeZone: v.timeZone || null,
        locale: v.locale || null,
        bio: v.bio || null,
      })
      .subscribe({
        next: (user) => {
          this.auth.updateUser(user);
          this.profileOk.set(true);
          this.profileMsg.set('Đã lưu hồ sơ.');
        },
        error: (err) => {
          this.profileOk.set(false);
          this.profileMsg.set(err?.error?.error ?? 'Lưu hồ sơ thất bại.');
        },
      });
  }

  onFile(input: HTMLInputElement): void {
    const file = input.files?.[0];
    if (!file) return;
    this.users.uploadAvatar(file).subscribe({
      next: (user) => {
        this.auth.updateUser(user);
        this.avatarOk.set(true);
        this.avatarMsg.set('Đã cập nhật ảnh đại diện.');
        input.value = '';
      },
      error: (err) => {
        this.avatarOk.set(false);
        this.avatarMsg.set(err?.error?.error ?? 'Tải ảnh thất bại.');
      },
    });
  }

  removeAvatar(): void {
    this.users.deleteAvatar().subscribe({
      next: (user) => {
        this.auth.updateUser(user);
        this.avatarOk.set(true);
        this.avatarMsg.set('Đã xóa ảnh đại diện.');
      },
      error: (err) => {
        this.avatarOk.set(false);
        this.avatarMsg.set(err?.error?.error ?? 'Xóa ảnh thất bại.');
      },
    });
  }

  changeEmail(): void {
    if (this.emailForm.invalid) return;
    const { newEmail, currentPassword } = this.emailForm.getRawValue();
    this.users.changeEmail(newEmail, currentPassword || null).subscribe({
      next: (user) => {
        this.auth.updateUser(user);
        this.emailOk.set(true);
        this.emailMsg.set('Đã đổi email.');
        this.emailForm.reset();
      },
      error: (err) => {
        this.emailOk.set(false);
        this.emailMsg.set(err?.error?.error ?? 'Đổi email thất bại.');
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    this.users.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordOk.set(true);
        this.passwordMsg.set('Đã đổi mật khẩu.');
        this.passwordForm.reset();
      },
      error: (err) => {
        this.passwordOk.set(false);
        this.passwordMsg.set(err?.error?.error ?? 'Đổi mật khẩu thất bại.');
      },
    });
  }

  // --- 2FA ---

  startSetup(): void {
    this.twoFaMsg.set(null);
    this.users.twoFactorSetup().subscribe({
      next: (s) => this.setup.set(s),
      error: (err) => {
        this.twoFaOk.set(false);
        this.twoFaMsg.set(err?.error?.error ?? 'Không thể khởi tạo 2FA.');
      },
    });
  }

  enable2fa(): void {
    this.users.twoFactorEnable(this.enableCode()).subscribe({
      next: (res) => {
        this.recoveryCodes.set(res.recoveryCodes);
        this.twoFaOk.set(true);
        this.twoFaMsg.set(null);
        const u = this.auth.user();
        if (u) this.auth.updateUser({ ...u, twoFactorEnabled: true });
        this.setup.set(null);
        this.enableCode.set('');
      },
      error: (err) => {
        this.twoFaOk.set(false);
        this.twoFaMsg.set(err?.error?.error ?? 'Mã xác thực không đúng.');
      },
    });
  }

  disable2fa(): void {
    this.users.twoFactorDisable(this.disablePwd() || null).subscribe({
      next: () => {
        const u = this.auth.user();
        if (u) this.auth.updateUser({ ...u, twoFactorEnabled: false });
        this.recoveryCodes.set([]);
        this.disablePwd.set('');
        this.twoFaOk.set(true);
        this.twoFaMsg.set('Đã tắt 2FA.');
      },
      error: (err) => {
        this.twoFaOk.set(false);
        this.twoFaMsg.set(err?.error?.error ?? 'Tắt 2FA thất bại.');
      },
    });
  }

  deleteAccount(): void {
    this.users.deleteAccount(this.deletePwd() || null).subscribe({
      next: () => {
        this.auth.clearSession();
        location.href = '/login';
      },
      error: (err) => this.deleteMsg.set(err?.error?.error ?? 'Xóa tài khoản thất bại.'),
    });
  }
}
