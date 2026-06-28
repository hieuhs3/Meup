export type DocumentCategory = 'cv' | 'certificate' | 'contract' | 'invoice' | 'personal' | 'other';

export const DOCUMENT_CATEGORIES: DocumentCategory[] = ['cv', 'certificate', 'contract', 'invoice', 'personal', 'other'];

export const DOCUMENT_CATEGORY_LABELS: Record<DocumentCategory, string> = {
  cv: 'CV', certificate: 'Chứng chỉ', contract: 'Hợp đồng', invoice: 'Hóa đơn', personal: 'Cá nhân', other: 'Khác',
};

export interface DocumentItem {
  id: string;
  category: DocumentCategory;
  fileName: string;
  contentType: string;
  size: number;
  uploadedAt: string;
}
