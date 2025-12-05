import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Document,
  DocumentUploadRequest,
  DocumentUpdateRequest,
  PaginatedDocuments
} from '../models/document.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private http = inject(HttpClient);
  private readonly documentsUrl = `${environment.apiUrl}/documents`;

  getDocuments(page: number = 1, pageSize: number = 10): Observable<PaginatedDocuments> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PaginatedDocuments>(this.documentsUrl, { params });
  }

  getDocument(id: number): Observable<Document> {
    return this.http.get<Document>(`${this.documentsUrl}/${id}`);
  }

  uploadDocument(request: DocumentUploadRequest): Observable<any> {
    const formData = new FormData();
    formData.append('file', request.file);
    formData.append('title', request.title);
    if (request.description) {
      formData.append('description', request.description);
    }
    formData.append('isPublic', request.isPublic.toString());

    return this.http.post(this.documentsUrl, formData);
  }

  updateDocument(id: number, request: DocumentUpdateRequest): Observable<Document> {
    return this.http.put<Document>(`${this.documentsUrl}/${id}`, request);
  }

  deleteDocument(id: number): Observable<void> {
    return this.http.delete<void>(`${this.documentsUrl}/${id}`);
  }

  downloadDocument(id: number, fileName: string): Observable<Blob> {
    return this.http.get(`${this.documentsUrl}/${id}/download`, {
      responseType: 'blob'
    });
  }

  triggerDownload(id: number, fileName: string): void {
    this.downloadDocument(id, fileName).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}