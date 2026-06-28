import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FinanceService } from '../../core/services/finance.service';
import { AiService } from '../../core/services/ai.service';
import { ConfirmService } from '../../core/services/confirm.service';
import { MoneyPipe } from '../../core/pipes/money.pipe';
import {
  ASSET_TYPE_LABELS,
  ASSET_TYPES,
  Asset,
  AssetTypeKey,
  Budget,
  Category,
  FinanceType,
  NetWorth,
  Summary,
  Transaction,
  TransactionList,
} from '../../core/models/finance.models';

@Component({
  selector: 'app-finance',
  imports: [ReactiveFormsModule, MoneyPipe],
  templateUrl: './finance.html',
  styles: [`
    .sum-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 1rem; margin-bottom: 1.25rem; }
    .sum { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 1.1rem 1.25rem; }
    .sum .label { color: var(--muted); font-size: .82rem; }
    .sum .value { font-size: 1.5rem; font-weight: 700; margin-top: .25rem; }
    .value.income, .amount.income { color: var(--success); }
    .value.expense, .amount.expense { color: var(--danger); }
    .row { display: flex; gap: .75rem; flex-wrap: wrap; align-items: flex-end; }
    .row label { flex: 1; min-width: 120px; margin-bottom: 0; }
    .amount { font-weight: 600; text-align: right; white-space: nowrap; }
    .dot { display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-right: .4rem; vertical-align: middle; }
    .badge.income { background: #e4f6ec; color: var(--success); }
    .badge.expense { background: #fde7ea; color: var(--danger); }
    .actions button { padding: .3rem .6rem; font-size: .82rem; margin-left: .35rem; }
    .pager { display: flex; gap: .75rem; align-items: center; justify-content: flex-end; margin-top: 1rem; }
    .cat-list { display: flex; flex-wrap: wrap; gap: .5rem; }
    .cat-chip { display: flex; align-items: center; gap: .4rem; border: 1px solid var(--border); border-radius: 999px; padding: .3rem .7rem; font-size: .85rem; }
    .cat-chip button { background: transparent; color: var(--muted); padding: 0 .2rem; }
    .danger-btn { background: var(--danger); }
    .inline-input { display: inline-block; width: auto; margin: 0; padding: .2rem .4rem; }
    .asset-row { display: flex; align-items: center; gap: .6rem; padding: .5rem 0; border-bottom: 1px solid var(--border); }
    .asset-row .nm { flex: 1; }
    .asset-row .ty { font-size: .75rem; background: #eef1fb; color: var(--primary); border-radius: 999px; padding: .1rem .5rem; }
    .nw-grid { display: flex; flex-wrap: wrap; gap: .5rem; margin: .5rem 0; }
    .nw-grid .g { background: var(--surface); border: 1px solid var(--border); border-radius: 10px; padding: .5rem .8rem; font-size: .85rem; }
    .nw-grid .g b { display: block; }
    .flow { display: flex; align-items: flex-end; gap: 8px; height: 110px; padding-top: .5rem; }
    .flow .col { display: flex; flex-direction: column; align-items: center; justify-content: flex-end; flex: 1; gap: 2px; }
    .flow .bars { display: flex; align-items: flex-end; gap: 2px; height: 80px; }
    .flow .bars span { width: 10px; border-radius: 3px 3px 0 0; }
    .flow .bars .inc { background: var(--success); }
    .flow .bars .exp { background: var(--danger); }
    .flow .m { font-size: .6rem; color: var(--muted); }
  `],
})
export class Finance implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly finance = inject(FinanceService);
  private readonly ai = inject(AiService);
  private readonly confirm = inject(ConfirmService);
  readonly suggesting = signal(false);

  readonly summary = signal<Summary | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly list = signal<TransactionList | null>(null);
  readonly error = signal<string | null>(null);
  readonly txMsg = signal<string | null>(null);

  readonly editingId = signal<string | null>(null);
  readonly txType = signal<FinanceType>('expense');
  readonly page = signal(1);

  readonly editingCatId = signal<string | null>(null);
  readonly catMsg = signal<string | null>(null);

  readonly budgets = signal<Budget[]>([]);
  readonly budgetMsg = signal<string | null>(null);
  readonly expenseCategories = computed(() => this.categories().filter((c) => c.type === 'expense'));

  // Danh mục lọc theo loại đang chọn ở form giao dịch.
  readonly categoriesForType = computed(() =>
    this.categories().filter((c) => c.type === this.txType()),
  );
  readonly totalPages = computed(() => {
    const l = this.list();
    return l ? Math.max(1, Math.ceil(l.total / l.pageSize)) : 1;
  });

  readonly txForm = this.fb.nonNullable.group({
    type: ['expense' as FinanceType, [Validators.required]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    categoryId: [''],
    date: [this.todayIso(), [Validators.required]],
    note: [''],
  });

  readonly filterForm = this.fb.nonNullable.group({
    from: [''],
    to: [''],
    type: ['' as FinanceType | ''],
    categoryId: [''],
    q: [''],
  });

  readonly catForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    type: ['expense' as FinanceType, [Validators.required]],
    color: ['#4361ee'],
  });

  readonly budgetForm = this.fb.nonNullable.group({
    categoryId: ['', [Validators.required]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  // --- Tài sản & Net Worth (G4) ---
  readonly assets = signal<Asset[]>([]);
  readonly netWorth = signal<NetWorth | null>(null);
  readonly assetMsg = signal<string | null>(null);
  readonly editingAssetId = signal<string | null>(null);
  readonly assetTypes = ASSET_TYPES;
  assetTypeLabel(t: string): string { return ASSET_TYPE_LABELS[t as AssetTypeKey] ?? t; }

  readonly assetForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    type: ['cash' as AssetTypeKey, [Validators.required]],
    value: [null as number | null, [Validators.required, Validators.min(0)]],
    note: [''],
  });

  constructor() {
    this.txForm.controls.type.valueChanges.subscribe((t) => {
      this.txType.set(t);
      const cat = this.categories().find((c) => c.id === this.txForm.controls.categoryId.value);
      if (cat && cat.type !== t) this.txForm.controls.categoryId.setValue('');
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadSummary();
    this.loadTransactions();
    this.loadBudgets();
    this.loadAssets();
    this.loadNetWorth();
  }

  // --- Tài sản & Net Worth (G4) ---

  loadAssets(): void {
    this.finance.getAssets().subscribe({ next: (a) => this.assets.set(a) });
  }

  loadNetWorth(): void {
    this.finance.getNetWorth().subscribe({ next: (n) => this.netWorth.set(n) });
  }

  submitAsset(): void {
    if (this.assetForm.invalid) return;
    const v = this.assetForm.getRawValue();
    const body = { name: v.name, type: v.type, value: Number(v.value), note: v.note || null };
    const id = this.editingAssetId();
    const req = id ? this.finance.updateAsset(id, body) : this.finance.createAsset(body);
    req.subscribe({
      next: () => {
        this.assetMsg.set(id ? 'Đã cập nhật tài sản.' : 'Đã thêm tài sản.');
        this.resetAssetForm();
        this.loadAssets();
        this.loadNetWorth();
      },
      error: (err) => this.assetMsg.set(err?.error?.error ?? 'Lưu tài sản thất bại.'),
    });
  }

  editAsset(a: Asset): void {
    this.editingAssetId.set(a.id);
    this.assetForm.setValue({ name: a.name, type: a.type, value: a.value, note: a.note ?? '' });
    this.assetMsg.set(null);
  }

  resetAssetForm(): void {
    this.editingAssetId.set(null);
    this.assetForm.reset({ name: '', type: 'cash', value: null, note: '' });
  }

  async deleteAsset(a: Asset): Promise<void> {
    if (!(await this.confirm.ask(`Xóa tài sản "${a.name}"?`))) return;
    this.finance.deleteAsset(a.id).subscribe({
      next: () => { this.loadAssets(); this.loadNetWorth(); },
    });
  }

  // --- Ngân sách (A1) ---

  loadBudgets(): void {
    this.finance.getBudgets().subscribe({ next: (b) => this.budgets.set(b) });
  }

  addBudget(): void {
    if (this.budgetForm.invalid) return;
    const v = this.budgetForm.getRawValue();
    this.finance.createBudget(v.categoryId, Number(v.amount)).subscribe({
      next: () => {
        this.budgetMsg.set('Đã đặt ngân sách.');
        this.budgetForm.reset({ categoryId: '', amount: null });
        this.loadBudgets();
      },
      error: (err) => this.budgetMsg.set(err?.error?.error ?? 'Đặt ngân sách thất bại.'),
    });
  }

  saveBudget(b: Budget, value: string): void {
    const amount = Number(value);
    if (!amount || amount <= 0) return;
    this.finance.updateBudget(b.id, amount).subscribe({
      next: (updated) => this.budgets.update((list) => list.map((x) => (x.id === b.id ? updated : x))),
    });
  }

  async deleteBudget(b: Budget): Promise<void> {
    if (!(await this.confirm.ask(`Xóa ngân sách "${b.categoryName}"?`))) return;
    this.finance.deleteBudget(b.id).subscribe({ next: () => this.loadBudgets() });
  }

  // --- Tải dữ liệu ---

  private loadCategories(): void {
    this.finance.getCategories().subscribe({
      next: (c) => this.categories.set(c),
      error: () => this.error.set('Không tải được danh mục.'),
    });
  }

  private loadSummary(): void {
    this.finance.getSummary(this.todayIso()).subscribe({
      next: (s) => this.summary.set(s),
      error: () => this.error.set('Không tải được tổng quan.'),
    });
  }

  loadTransactions(): void {
    const f = this.filterForm.getRawValue();
    this.finance
      .getTransactions({
        from: f.from || null,
        to: f.to || null,
        type: f.type || '',
        categoryId: f.categoryId || '',
        q: f.q || null,
        page: this.page(),
        pageSize: 20,
      })
      .subscribe({
        next: (l) => this.list.set(l),
        error: () => this.error.set('Không tải được danh sách giao dịch.'),
      });
  }

  // --- Giao dịch ---

  submitTransaction(): void {
    if (this.txForm.invalid) return;
    const v = this.txForm.getRawValue();
    const body = {
      type: v.type,
      amount: Number(v.amount),
      categoryId: v.categoryId || null,
      date: v.date,
      note: v.note || null,
    };

    const id = this.editingId();
    const req = id
      ? this.finance.updateTransaction(id, body)
      : this.finance.createTransaction(body);

    req.subscribe({
      next: () => {
        this.txMsg.set(id ? 'Đã cập nhật giao dịch.' : 'Đã thêm giao dịch.');
        this.resetForm();
        this.refreshAfterChange();
      },
      error: (err) => this.txMsg.set(err?.error?.error ?? 'Lưu giao dịch thất bại.'),
    });
  }

  editTransaction(t: Transaction): void {
    this.editingId.set(t.id);
    this.txType.set(t.type);
    this.txForm.setValue({
      type: t.type,
      amount: t.amount,
      categoryId: t.categoryId ?? '',
      date: t.date,
      note: t.note ?? '',
    });
    this.txMsg.set(null);
  }

  async deleteTransaction(t: Transaction): Promise<void> {
    if (!(await this.confirm.ask('Xóa giao dịch này?'))) return;
    this.finance.deleteTransaction(t.id).subscribe({
      next: () => this.refreshAfterChange(),
      error: (err) => this.error.set(err?.error?.error ?? 'Xóa thất bại.'),
    });
  }

  /** Gợi ý danh mục từ ghi chú bằng AI (Haiku). */
  suggestCategory(): void {
    const v = this.txForm.getRawValue();
    if (!v.note?.trim()) return;
    this.suggesting.set(true);
    this.ai.categorize(v.note, v.type).subscribe({
      next: (s) => {
        this.suggesting.set(false);
        if (s.categoryId) {
          this.txForm.controls.categoryId.setValue(s.categoryId);
          this.txMsg.set(`AI gợi ý danh mục: ${s.categoryName}`);
        } else {
          this.txMsg.set(s.enabled ? 'AI chưa tìm được danh mục phù hợp.' : 'Tính năng AI chưa được bật.');
        }
      },
      error: () => {
        this.suggesting.set(false);
        this.txMsg.set('Gợi ý AI gặp lỗi. Vui lòng thử lại.');
      },
    });
  }

  resetForm(): void {
    this.editingId.set(null);
    this.txType.set('expense');
    this.txForm.reset({ type: 'expense', amount: null, categoryId: '', date: this.todayIso(), note: '' });
  }

  // --- Lọc / phân trang ---

  applyFilter(): void {
    this.page.set(1);
    this.loadTransactions();
  }

  resetFilter(): void {
    this.filterForm.reset({ from: '', to: '', type: '', categoryId: '', q: '' });
    this.page.set(1);
    this.loadTransactions();
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.loadTransactions();
    }
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.loadTransactions();
    }
  }

  // --- Danh mục ---

  addCategory(): void {
    if (this.catForm.invalid) return;
    this.finance.createCategory(this.catForm.getRawValue()).subscribe({
      next: () => {
        this.catMsg.set('Đã thêm danh mục.');
        this.catForm.controls.name.reset('');
        this.loadCategories();
      },
      error: (err) => this.catMsg.set(err?.error?.error ?? 'Thêm danh mục thất bại.'),
    });
  }

  startEditCategory(c: Category): void {
    this.editingCatId.set(c.id);
  }

  saveCategory(c: Category, name: string): void {
    if (!name.trim()) return;
    this.finance.updateCategory(c.id, { name: name.trim(), color: c.color }).subscribe({
      next: () => {
        this.editingCatId.set(null);
        this.loadCategories();
        this.refreshAfterChange();
      },
      error: (err) => this.catMsg.set(err?.error?.error ?? 'Lưu danh mục thất bại.'),
    });
  }

  async deleteCategory(c: Category): Promise<void> {
    if (!(await this.confirm.ask(`Xóa danh mục "${c.name}"? Giao dịch liên quan sẽ được gỡ liên kết.`))) return;
    this.finance.deleteCategory(c.id).subscribe({
      next: () => {
        this.loadCategories();
        this.refreshAfterChange();
      },
      error: (err) => this.catMsg.set(err?.error?.error ?? 'Xóa danh mục thất bại.'),
    });
  }

  // --- Tiện ích ---

  private refreshAfterChange(): void {
    this.loadSummary();
    this.loadTransactions();
    this.loadBudgets();
    this.loadNetWorth();
  }


  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
