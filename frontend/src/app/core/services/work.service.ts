import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import {
  CreateTaskRequest,
  Goal,
  Habit,
  TaskItem,
  TaskStatus,
  UpdateTaskRequest,
  WorkSummary,
} from '../models/work.models';

@Injectable({ providedIn: 'root' })
export class WorkService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/work`;

  // --- Task ---

  getTasks(status: TaskStatus = 'all'): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(`${this.base}/tasks`, { params: new HttpParams().set('status', status) });
  }

  createTask(body: CreateTaskRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.base}/tasks`, body);
  }

  updateTask(id: string, body: UpdateTaskRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.base}/tasks/${id}`, body);
  }

  toggleTask(id: string): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.base}/tasks/${id}/toggle`, {});
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/tasks/${id}`);
  }

  // --- Goal ---

  getGoals(): Observable<Goal[]> {
    return this.http.get<Goal[]>(`${this.base}/goals`);
  }

  createGoal(name: string, progress: number): Observable<Goal> {
    return this.http.post<Goal>(`${this.base}/goals`, { name, progress });
  }

  updateGoal(id: string, name: string, progress: number): Observable<Goal> {
    return this.http.put<Goal>(`${this.base}/goals/${id}`, { name, progress });
  }

  deleteGoal(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/goals/${id}`);
  }

  // --- Habit ---

  getHabits(date?: string): Observable<Habit[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<Habit[]>(`${this.base}/habits`, { params });
  }

  createHabit(name: string): Observable<Habit> {
    return this.http.post<Habit>(`${this.base}/habits`, { name });
  }

  updateHabit(id: string, name: string): Observable<Habit> {
    return this.http.put<Habit>(`${this.base}/habits/${id}`, { name });
  }

  deleteHabit(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/habits/${id}`);
  }

  setHabitCheck(id: string, date: string, checked: boolean): Observable<Habit> {
    const params = new HttpParams().set('date', date);
    return checked
      ? this.http.post<Habit>(`${this.base}/habits/${id}/check`, {}, { params })
      : this.http.delete<Habit>(`${this.base}/habits/${id}/check`, { params });
  }

  // --- Tổng quan ---

  getSummary(date?: string): Observable<WorkSummary> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<WorkSummary>(`${this.base}/summary`, { params });
  }
}
