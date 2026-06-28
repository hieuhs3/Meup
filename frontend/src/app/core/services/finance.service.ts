import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import {
  Asset,
  Budget,
  Category,
  CreateCategoryRequest,
  CreateTransactionRequest,
  FinanceType,
  NetWorth,
  SaveAssetRequest,
  Summary,
  Transaction,
  TransactionFilter,
  TransactionList,
  UpdateCategoryRequest,
} from '../models/finance.models';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/finance`;

  // --- Danh mục ---

  getCategories(type?: FinanceType): Observable<Category[]> {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.get<Category[]>(`${this.base}/categories`, { params });
  }

  createCategory(body: CreateCategoryRequest): Observable<Category> {
    return this.http.post<Category>(`${this.base}/categories`, body);
  }

  updateCategory(id: string, body: UpdateCategoryRequest): Observable<Category> {
    return this.http.put<Category>(`${this.base}/categories/${id}`, body);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/categories/${id}`);
  }

  // --- Giao dịch ---

  getTransactions(filter: TransactionFilter): Observable<TransactionList> {
    let params = new HttpParams();
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);
    if (filter.type) params = params.set('type', filter.type);
    if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
    if (filter.q) params = params.set('q', filter.q);
    params = params.set('page', String(filter.page ?? 1));
    params = params.set('pageSize', String(filter.pageSize ?? 20));
    return this.http.get<TransactionList>(`${this.base}/transactions`, { params });
  }

  createTransaction(body: CreateTransactionRequest): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.base}/transactions`, body);
  }

  updateTransaction(id: string, body: CreateTransactionRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.base}/transactions/${id}`, body);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/transactions/${id}`);
  }

  // --- Tổng hợp ---

  getSummary(date?: string): Observable<Summary> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<Summary>(`${this.base}/summary`, { params });
  }

  // --- Ngân sách (A1) ---

  getBudgets(month?: string): Observable<Budget[]> {
    let params = new HttpParams();
    if (month) params = params.set('month', month);
    return this.http.get<Budget[]>(`${this.base}/budgets`, { params });
  }

  createBudget(categoryId: string, amount: number): Observable<Budget> {
    return this.http.post<Budget>(`${this.base}/budgets`, { categoryId, amount });
  }

  updateBudget(id: string, amount: number): Observable<Budget> {
    return this.http.put<Budget>(`${this.base}/budgets/${id}`, { amount });
  }

  deleteBudget(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/budgets/${id}`);
  }

  // --- Tài sản & Net Worth (G4) ---

  getAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(`${this.base}/assets`);
  }

  createAsset(body: SaveAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.base}/assets`, body);
  }

  updateAsset(id: string, body: SaveAssetRequest): Observable<Asset> {
    return this.http.put<Asset>(`${this.base}/assets/${id}`, body);
  }

  deleteAsset(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/assets/${id}`);
  }

  getNetWorth(month?: string): Observable<NetWorth> {
    let params = new HttpParams();
    if (month) params = params.set('month', month);
    return this.http.get<NetWorth>(`${this.base}/networth`, { params });
  }
}
