import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CareerService } from '../../core/services/career.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { CareerProject, Certification, Skill } from '../../core/models/career.models';

@Component({
  selector: 'app-career',
  imports: [ReactiveFormsModule],
  template: `
    <header class="page-head">
      <h1>Sự nghiệp</h1>
      <p class="muted">Kỹ năng, chứng chỉ và dự án — hồ sơ phát triển bản thân.</p>
    </header>
    @if (error()) { <p class="error">{{ error() }}</p> }

    <!-- Kỹ năng -->
    <section class="card">
      <h3>Kỹ năng</h3>
      <form class="row" [formGroup]="skillForm" (ngSubmit)="addSkill()">
        <label class="grow">Tên <input type="text" formControlName="name" maxlength="100" /></label>
        <label>Nhóm <input type="text" formControlName="category" maxlength="50" placeholder="vd Backend" /></label>
        <label>Mức (1–5) <input type="number" formControlName="level" min="1" max="5" style="width:80px" /></label>
        <button type="submit" [disabled]="skillForm.invalid">Thêm</button>
      </form>
      @for (s of skills(); track s.id) {
        <div class="item">
          <span class="grow"><b>{{ s.name }}</b>
            @if (s.category) { <small class="muted"> · {{ s.category }}</small> }
          </span>
          <span class="stars" [title]="s.level + '/5'">{{ stars(s.level) }}</span>
          <button class="icon-btn danger" (click)="delSkill(s)" aria-label="Xóa">✕</button>
        </div>
      } @empty { <p class="muted">Chưa có kỹ năng.</p> }
    </section>

    <!-- Chứng chỉ -->
    <section class="card">
      <h3>Chứng chỉ</h3>
      <form class="row" [formGroup]="certForm" (ngSubmit)="addCert()">
        <label class="grow">Tên <input type="text" formControlName="name" maxlength="150" /></label>
        <label>Đơn vị cấp <input type="text" formControlName="issuer" maxlength="100" placeholder="vd AWS" /></label>
        <label>Cấp ngày <input type="date" formControlName="issuedAt" /></label>
        <label>Hết hạn <input type="date" formControlName="expiresAt" /></label>
        <button type="submit" [disabled]="certForm.invalid">Thêm</button>
      </form>
      @for (c of certs(); track c.id) {
        <div class="item">
          <span class="grow"><b>{{ c.name }}</b>
            @if (c.issuer) { <small class="muted"> · {{ c.issuer }}</small> }
          </span>
          @if (c.issuedAt) { <small class="muted">{{ c.issuedAt }}@if (c.expiresAt) { → {{ c.expiresAt }} }</small> }
          <button class="icon-btn danger" (click)="delCert(c)" aria-label="Xóa">✕</button>
        </div>
      } @empty { <p class="muted">Chưa có chứng chỉ.</p> }
    </section>

    <!-- Dự án -->
    <section class="card">
      <h3>Dự án</h3>
      <form class="row" [formGroup]="projForm" (ngSubmit)="addProj()">
        <label class="grow">Tên <input type="text" formControlName="name" maxlength="150" /></label>
        <label>Vai trò <input type="text" formControlName="role" maxlength="100" /></label>
        <label>Bắt đầu <input type="date" formControlName="startedAt" /></label>
        <label>Kết thúc <input type="date" formControlName="endedAt" /></label>
        <button type="submit" [disabled]="projForm.invalid">Thêm</button>
      </form>
      <label style="display:block">Mô tả
        <input type="text" [formControl]="projForm.controls.description" maxlength="2000" placeholder="Mô tả ngắn (tùy chọn)" />
      </label>
      @for (p of projects(); track p.id) {
        <div class="item">
          <span class="grow"><b>{{ p.name }}</b>
            @if (p.role) { <small class="muted"> · {{ p.role }}</small> }
            @if (p.description) { <div class="muted desc">{{ p.description }}</div> }
          </span>
          @if (p.startedAt) { <small class="muted">{{ p.startedAt }}@if (p.endedAt) { → {{ p.endedAt }} } @else { → nay }</small> }
          <button class="icon-btn danger" (click)="delProj(p)" aria-label="Xóa">✕</button>
        </div>
      } @empty { <p class="muted">Chưa có dự án.</p> }
    </section>
  `,
  styles: [`
    .row { display: flex; gap: .6rem; align-items: flex-end; flex-wrap: wrap; margin-bottom: .8rem; }
    .row label { margin-bottom: 0; }
    .row .grow { flex: 1; min-width: 150px; }
    .item { display: flex; align-items: center; gap: .6rem; padding: .5rem 0; border-bottom: 1px solid var(--border); }
    .item .grow { flex: 1; }
    .desc { font-size: .85rem; margin-top: .15rem; }
    .stars { color: #f5a623; letter-spacing: 1px; white-space: nowrap; }
  `],
})
export class Career implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly career = inject(CareerService);
  private readonly confirm = inject(ConfirmService);

  readonly skills = signal<Skill[]>([]);
  readonly certs = signal<Certification[]>([]);
  readonly projects = signal<CareerProject[]>([]);
  readonly error = signal<string | null>(null);

  readonly skillForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    category: [''],
    level: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
  });
  readonly certForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    issuer: [''],
    issuedAt: [''],
    expiresAt: [''],
  });
  readonly projForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    role: [''],
    description: [''],
    startedAt: [''],
    endedAt: [''],
  });

  ngOnInit(): void {
    this.loadSkills();
    this.loadCerts();
    this.loadProjects();
  }

  stars(level: number): string {
    return '★'.repeat(Math.max(0, Math.min(5, level))) + '☆'.repeat(5 - Math.max(0, Math.min(5, level)));
  }

  private loadSkills(): void { this.career.getSkills().subscribe({ next: (s) => this.skills.set(s) }); }
  private loadCerts(): void { this.career.getCertifications().subscribe({ next: (c) => this.certs.set(c) }); }
  private loadProjects(): void { this.career.getProjects().subscribe({ next: (p) => this.projects.set(p) }); }

  addSkill(): void {
    if (this.skillForm.invalid) return;
    const v = this.skillForm.getRawValue();
    this.career.createSkill({ name: v.name, category: v.category || null, level: Number(v.level) }).subscribe({
      next: () => { this.skillForm.reset({ name: '', category: '', level: 3 }); this.loadSkills(); },
      error: (e) => this.error.set(e?.error?.error ?? 'Thêm kỹ năng thất bại.'),
    });
  }

  addCert(): void {
    if (this.certForm.invalid) return;
    const v = this.certForm.getRawValue();
    this.career.createCertification({
      name: v.name, issuer: v.issuer || null, issuedAt: v.issuedAt || null, expiresAt: v.expiresAt || null,
    }).subscribe({
      next: () => { this.certForm.reset({ name: '', issuer: '', issuedAt: '', expiresAt: '' }); this.loadCerts(); },
      error: (e) => this.error.set(e?.error?.error ?? 'Thêm chứng chỉ thất bại.'),
    });
  }

  addProj(): void {
    if (this.projForm.invalid) return;
    const v = this.projForm.getRawValue();
    this.career.createProject({
      name: v.name, role: v.role || null, description: v.description || null,
      startedAt: v.startedAt || null, endedAt: v.endedAt || null,
    }).subscribe({
      next: () => { this.projForm.reset({ name: '', role: '', description: '', startedAt: '', endedAt: '' }); this.loadProjects(); },
      error: (e) => this.error.set(e?.error?.error ?? 'Thêm dự án thất bại.'),
    });
  }

  async delSkill(s: Skill): Promise<void> {
    if (!(await this.confirm.ask(`Xóa kỹ năng "${s.name}"?`))) return;
    this.career.deleteSkill(s.id).subscribe({ next: () => this.loadSkills() });
  }
  async delCert(c: Certification): Promise<void> {
    if (!(await this.confirm.ask(`Xóa chứng chỉ "${c.name}"?`))) return;
    this.career.deleteCertification(c.id).subscribe({ next: () => this.loadCerts() });
  }
  async delProj(p: CareerProject): Promise<void> {
    if (!(await this.confirm.ask(`Xóa dự án "${p.name}"?`))) return;
    this.career.deleteProject(p.id).subscribe({ next: () => this.loadProjects() });
  }
}
