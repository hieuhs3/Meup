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
