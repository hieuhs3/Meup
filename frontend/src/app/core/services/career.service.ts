import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE } from '../api.config';
import {
  CareerProject,
  Certification,
  SaveCareerProjectRequest,
  SaveCertificationRequest,
  SaveSkillRequest,
  Skill,
} from '../models/career.models';

@Injectable({ providedIn: 'root' })
export class CareerService {
  private readonly http = inject(HttpClient);
  private readonly base = `${API_BASE}/career`;

  // Skills
  getSkills(): Observable<Skill[]> { return this.http.get<Skill[]>(`${this.base}/skills`); }
  createSkill(b: SaveSkillRequest): Observable<Skill> { return this.http.post<Skill>(`${this.base}/skills`, b); }
  updateSkill(id: string, b: SaveSkillRequest): Observable<Skill> { return this.http.put<Skill>(`${this.base}/skills/${id}`, b); }
  deleteSkill(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/skills/${id}`); }

  // Certifications
  getCertifications(): Observable<Certification[]> { return this.http.get<Certification[]>(`${this.base}/certifications`); }
  createCertification(b: SaveCertificationRequest): Observable<Certification> { return this.http.post<Certification>(`${this.base}/certifications`, b); }
  updateCertification(id: string, b: SaveCertificationRequest): Observable<Certification> { return this.http.put<Certification>(`${this.base}/certifications/${id}`, b); }
  deleteCertification(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/certifications/${id}`); }

  // Projects
  getProjects(): Observable<CareerProject[]> { return this.http.get<CareerProject[]>(`${this.base}/projects`); }
  createProject(b: SaveCareerProjectRequest): Observable<CareerProject> { return this.http.post<CareerProject>(`${this.base}/projects`, b); }
  updateProject(id: string, b: SaveCareerProjectRequest): Observable<CareerProject> { return this.http.put<CareerProject>(`${this.base}/projects/${id}`, b); }
  deleteProject(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/projects/${id}`); }
}
