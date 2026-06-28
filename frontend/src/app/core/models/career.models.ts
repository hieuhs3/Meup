export interface Skill {
  id: string;
  name: string;
  category: string | null;
  level: number; // 1–5
  createdAt: string;
}

export interface SaveSkillRequest {
  name: string;
  category?: string | null;
  level: number;
}

export interface Certification {
  id: string;
  name: string;
  issuer: string | null;
  issuedAt: string | null;
  expiresAt: string | null;
  createdAt: string;
}

export interface SaveCertificationRequest {
  name: string;
  issuer?: string | null;
  issuedAt?: string | null;
  expiresAt?: string | null;
}

export interface CareerProject {
  id: string;
  name: string;
  role: string | null;
  description: string | null;
  startedAt: string | null;
  endedAt: string | null;
  createdAt: string;
}

export interface SaveCareerProjectRequest {
  name: string;
  role?: string | null;
  description?: string | null;
  startedAt?: string | null;
  endedAt?: string | null;
}
