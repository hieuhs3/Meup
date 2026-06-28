export type FinanceType = 'income' | 'expense';

export interface Category {
  id: string;
  name: string;
  type: FinanceType;
  color: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  type: FinanceType;
  color?: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  color?: string | null;
}

export interface Transaction {
  id: string;
  type: FinanceType;
  amount: number;
  categoryId: string | null;
  categoryName: string | null;
  categoryColor: string | null;
  date: string; // yyyy-MM-dd
  note: string | null;
  createdAt: string;
}

export interface CreateTransactionRequest {
  type: FinanceType;
  amount: number;
  categoryId?: string | null;
  date: string;
  note?: string | null;
}

export interface TransactionList {
  items: Transaction[];
  total: number;
  page: number;
  pageSize: number;
}

export interface Summary {
  date: string;
  balance: number;
  dayIncome: number;
  dayExpense: number;
  monthIncome: number;
  monthExpense: number;
}

export interface Budget {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string | null;
  amount: number;
  spent: number;
  remaining: number;
  percent: number;
}

export interface TransactionFilter {
  from?: string | null;
  to?: string | null;
  type?: FinanceType | '';
  categoryId?: string | '';
  q?: string | null;
  page?: number;
  pageSize?: number;
}

// --- Tài sản & Net Worth (G4) ---

export type AssetTypeKey = 'cash' | 'bank' | 'stock' | 'crypto' | 'gold' | 'other';

export const ASSET_TYPES: AssetTypeKey[] = ['cash', 'bank', 'stock', 'crypto', 'gold', 'other'];

export const ASSET_TYPE_LABELS: Record<AssetTypeKey, string> = {
  cash: 'Tiền mặt', bank: 'Ngân hàng', stock: 'Cổ phiếu', crypto: 'Crypto', gold: 'Vàng', other: 'Khác',
};

export interface Asset {
  id: string;
  name: string;
  type: AssetTypeKey;
  value: number;
  note: string | null;
  updatedAt: string;
}

export interface SaveAssetRequest {
  name: string;
  type: AssetTypeKey;
  value: number;
  note?: string | null;
}

export interface AssetGroup {
  type: AssetTypeKey;
  total: number;
}

export interface CashFlowPoint {
  month: string; // yyyy-MM
  income: number;
  expense: number;
  net: number;
}

export interface NetWorth {
  netWorth: number;
  byType: AssetGroup[];
  monthIncome: number;
  monthExpense: number;
  savingRate: number; // %
  cashFlow: CashFlowPoint[];
}
