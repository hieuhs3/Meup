import { Component, inject, signal, input, output, computed, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FinanceService } from '../../core/services/finance.service';
import { Category, FinanceType } from '../../core/models/finance.models';
import { HapticsService } from '../../core/services/haptics.service';

/**
 * MobileFinanceSheet — Bottom sheet thêm giao dịch nhanh trên mobile.
 * Slide lên từ dưới với:
 * - Toggle Chi/Thu
 * - Nhập số tiền lớn
 * - Category picker dạng scroll ngang
 * - Ghi chú + ngày
 * - Nút Lưu
 */
@Component({
  selector: 'app-mobile-finance-sheet',
  imports: [ReactiveFormsModule],
  template: `
    <!-- Backdrop -->
    <div class="sheet-backdrop" (click)="closed.emit()" aria-hidden="true"></div>

    <div class="sheet" role="dialog" aria-modal="true" aria-label="Thêm giao dịch">
      <!-- Handle -->
      <div class="handle" aria-hidden="true"></div>

      <!-- Header -->
      <div class="sheet-header">
        <h2 class="sheet-title">Thêm giao dịch</h2>
        <button class="close-btn" (click)="closed.emit()" aria-label="Đóng">✕</button>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <!-- Toggle Chi / Thu -->
        <div class="type-toggle">
          <button type="button"
            class="type-btn"
            [class.active-expense]="txType() === 'expense'"
            (click)="setType('expense')">
            Chi tiêu
          </button>
          <button type="button"
            class="type-btn"
            [class.active-income]="txType() === 'income'"
            (click)="setType('income')">
            Thu nhập
          </button>
        </div>

        <!-- Số tiền -->
        <div class="amount-wrap">
          <input
            class="amount-input"
            type="number"
            formControlName="amount"
            placeholder="0"
            inputmode="decimal"
            min="0"
            step="1000"
            aria-label="Số tiền"
          />
          <span class="amount-currency">đ</span>
        </div>

        <!-- Category scroll -->
        <div class="cat-scroll" role="group" aria-label="Danh mục">
          @for (cat of catsForType(); track cat.id) {
            <button
              type="button"
              class="cat-chip"
              [class.selected]="form.controls.categoryId.value === cat.id"
              [style.--cat-color]="cat.color || '#4361ee'"
              (click)="selectCategory(cat.id)"
            >
              <span class="cat-dot"></span>
              {{ cat.name }}
            </button>
          }
        </div>

        <!-- Ghi chú + ngày -->
        <div class="meta-row">
          <input class="meta-input" type="text" formControlName="note"
            placeholder="Ghi chú…" aria-label="Ghi chú" />
          <input class="meta-input date-input" type="date" formControlName="date"
            aria-label="Ngày" />
        </div>

        <!-- Lỗi -->
        @if (error()) {
          <p class="field-error">{{ error() }}</p>
        }

        <!-- Save -->
        <button type="submit" class="save-btn" [disabled]="saving() || form.invalid">
          @if (saving()) { <span class="spinner"></span> } @else { Lưu giao dịch }
        </button>
      </form>
    </div>
  `,
  styles: [`
    .sheet-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.5);
      backdrop-filter: blur(4px);
      -webkit-backdrop-filter: blur(4px);
      z-index: 130;
      animation: fadeIn .2s ease;
    }

    .sheet {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 140;
      background: var(--surface);
      border-radius: 24px 24px 0 0;
      padding: 12px 20px calc(20px + env(safe-area-inset-bottom, 0px));
      box-shadow: 0 -12px 48px rgba(0,0,0,.3);
      animation: slideUp .3s cubic-bezier(.32,.72,0,1);
      border-top: 1px solid var(--border);
    }

    .handle {
      width: 40px; height: 4px;
      background: var(--border);
      border-radius: 2px;
      margin: 0 auto 16px;
    }

    .sheet-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 16px;
    }

    .sheet-title {
      font-size: 1rem;
      font-weight: 700;
      margin: 0;
    }

    .close-btn {
      background: var(--bg);
      color: var(--muted);
      border: none;
      border-radius: 50%;
      width: 32px;
      height: 32px;
      padding: 0;
      font-size: .9rem;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    /* Toggle Chi / Thu */
    .type-toggle {
      display: flex;
      background: var(--bg);
      border-radius: 12px;
      padding: 3px;
      gap: 3px;
      margin-bottom: 16px;
    }

    .type-btn {
      flex: 1;
      background: transparent;
      color: var(--muted);
      border: none;
      border-radius: 10px;
      padding: 8px;
      font-size: .9rem;
      font-weight: 500;
      transition: background .2s, color .2s;
    }

    .type-btn.active-expense {
      background: var(--danger);
      color: #fff;
    }

    .type-btn.active-income {
      background: var(--success);
      color: #fff;
    }

    /* Số tiền */
    .amount-wrap {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      margin-bottom: 16px;
    }

    .amount-input {
      font-size: 2rem;
      font-weight: 700;
      text-align: right;
      border: none;
      border-bottom: 2px solid var(--border);
      border-radius: 0;
      background: transparent;
      width: auto;
      max-width: 200px;
      color: var(--text);
      padding: 4px 0;
      margin: 0;
    }
    .amount-input:focus {
      outline: none;
      border-bottom-color: var(--primary);
    }

    .amount-currency {
      font-size: 1.2rem;
      color: var(--muted);
      font-weight: 600;
    }

    /* Category scroll */
    .cat-scroll {
      display: flex;
      gap: 8px;
      overflow-x: auto;
      padding-bottom: 4px;
      margin-bottom: 16px;
      scrollbar-width: none;
    }
    .cat-scroll::-webkit-scrollbar { display: none; }

    .cat-chip {
      flex: 0 0 auto;
      display: flex;
      align-items: center;
      gap: 6px;
      background: var(--bg);
      border: 1.5px solid var(--border);
      border-radius: 999px;
      padding: 6px 12px;
      font-size: .82rem;
      color: var(--text);
      white-space: nowrap;
      transition: border-color .15s, background .15s;
    }

    .cat-chip.selected {
      border-color: var(--cat-color, var(--primary));
      background: color-mix(in srgb, var(--cat-color, var(--primary)) 15%, transparent);
      color: var(--cat-color, var(--primary));
      font-weight: 600;
    }

    .cat-dot {
      width: 8px; height: 8px;
      border-radius: 50%;
      background: var(--cat-color, var(--primary));
      flex: 0 0 auto;
    }

    /* Meta: note + date */
    .meta-row {
      display: flex;
      gap: 10px;
      margin-bottom: 16px;
    }

    .meta-input {
      flex: 1;
      font-size: .9rem;
      padding: 10px 12px;
      border-radius: 10px;
    }

    .date-input {
      flex: 0 0 auto;
      width: auto;
    }

    /* Save button */
    .save-btn {
      width: 100%;
      padding: 14px;
      font-size: 1rem;
      font-weight: 600;
      border-radius: 14px;
      background: linear-gradient(135deg, #5b7cfa, #7c5bfa);
      color: #fff;
      border: none;
      box-shadow: 0 4px 16px rgba(91, 124, 250, 0.4);
      transition: opacity .2s;
    }
    .save-btn:disabled { opacity: .5; }

    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes slideUp { from { transform: translateY(100%); } to { transform: translateY(0); } }
  `],
})
export class MobileFinanceSheet implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly financeSvc = inject(FinanceService);

  private readonly haptics = inject(HapticsService);

  readonly categories = input<Category[]>([]);
  readonly closed = output<void>();
  readonly saved = output<void>();

  readonly txType = signal<FinanceType>('expense');
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly catsForType = computed(() =>
    this.categories().filter(c => c.type === this.txType())
  );

  readonly form = this.fb.nonNullable.group({
    type: ['expense' as FinanceType],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    categoryId: [''],
    date: [this.todayIso()],
    note: [''],
  });

  ngOnInit(): void {
    this.haptics.openSheet();
  }

  setType(t: FinanceType): void {
    this.haptics.tick();
    this.txType.set(t);
    this.form.controls.type.setValue(t);
    this.form.controls.categoryId.setValue('');
  }

  selectCategory(id: string): void {
    this.haptics.tick();
    this.form.controls.categoryId.setValue(id);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    this.financeSvc.createTransaction({
      type: v.type,
      amount: v.amount!,
      categoryId: v.categoryId || undefined,
      date: v.date,
      note: v.note,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.haptics.save();
        this.saved.emit();
        this.closed.emit();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.haptics.error();
        const msg = (err as { error?: { message?: string } })?.error?.message;
        this.error.set(msg ?? 'Lỗi khi lưu giao dịch');
      },
    });
  }

  private todayIso(): string {
    return new Date().toISOString().split('T')[0];
  }
}
