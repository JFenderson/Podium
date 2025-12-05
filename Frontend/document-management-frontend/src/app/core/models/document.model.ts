export interface Document {
  id: number;
  title: string;
  description: string;
  fileName: string;
  fileExtension: string;
  fileSizeInBytes: number;
  contentType: string;
  uploadedAt: Date;
  modifiedAt?: Date;
  status: string;
  version: number;
  isPublic: boolean;
}

export interface DocumentUploadRequest {
  file: File;
  title: string;
  description?: string;
  isPublic: boolean;
}

export interface DocumentUpdateRequest {
  title?: string;
  description?: string;
  isPublic?: boolean;
}

export interface PaginatedDocuments {
  data: Document[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}