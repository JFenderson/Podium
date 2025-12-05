import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { DocumentService } from '../../../core/services/document.service';
import { Document, DocumentUpdateRequest } from '../../../core/models/document.model';

@Component({
  selector: 'app-document-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  template: `
    <div class="detail-container">
      <mat-card *ngIf="loading" class="loading-card">
        <mat-spinner></mat-spinner>
      </mat-card>

      <mat-card *ngIf="!loading && document">
        <mat-card-header>
          <mat-card-title>
            <div class="header-row">
              <h2>Document Details</h2>
              <div class="actions">
                <button mat-icon-button (click)="downloadDocument()">
                  <mat-icon>download</mat-icon>
                </button>
                <button mat-icon-button (click)="goBack()">
                  <mat-icon>arrow_back</mat-icon>
                </button>
              </div>
            </div>
          </mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <!-- Document Info Section -->
          <div class="info-section">
            <h3>File Information</h3>
            <div class="info-grid">
              <div class="info-item">
                <span class="label">File Name:</span>
                <span class="value">{{ document.fileName }}</span>
              </div>
              <div class="info-item">
                <span class="label">File Type:</span>
                <span class="value">{{ document.fileExtension }}</span>
              </div>
              <div class="info-item">
                <span class="label">File Size:</span>
                <span class="value">{{ formatFileSize(document.fileSizeInBytes) }}</span>
              </div>
              <div class="info-item">
                <span class="label">Uploaded:</span>
                <span class="value">{{ document.uploadedAt | date:'medium' }}</span>
              </div>
              <div class="info-item" *ngIf="document.modifiedAt">
                <span class="label">Modified:</span>
                <span class="value">{{ document.modifiedAt | date:'medium' }}</span>
              </div>
              <div class="info-item">
                <span class="label">Status:</span>
                <span class="value">{{ document.status }}</span>
              </div>
              <div class="info-item">
                <span class="label">Version:</span>
                <span class="value">{{ document.version }}</span>
              </div>
            </div>
          </div>

          <mat-divider></mat-divider>

          <!-- Edit Form Section -->
          <div class="edit-section">
            <h3>Edit Document</h3>
            <form [formGroup]="editForm" (ngSubmit)="onSave()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Title</mat-label>
                <input matInput formControlName="title">
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Description</mat-label>
                <textarea matInput formControlName="description" rows="4"></textarea>
              </mat-form-field>

              <mat-checkbox formControlName="isPublic" class="checkbox">
                Make this document public
              </mat-checkbox>

              <div class="form-actions">
                <button mat-button type="button" (click)="resetForm()">Reset</button>
                <button mat-raised-button color="primary" type="submit" [disabled]="saving || !editForm.dirty">
                  <span *ngIf="!saving">Save Changes</span>
                  <mat-spinner *ngIf="saving" diameter="20"></mat-spinner>
                </button>
              </div>
            </form>
          </div>

          <mat-divider></mat-divider>

          <!-- Danger Zone -->
          <div class="danger-zone">
            <h3>Danger Zone</h3>
            <button mat-stroked-button color="warn" (click)="deleteDocument()">
              <mat-icon>delete</mat-icon>
              Delete Document
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .detail-container {
      padding: 24px;
      max-width: 1000px;
      margin: 0 auto;
    }

    .loading-card {
      display: flex;
      justify-content: center;
      padding: 60px;
    }

    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
    }

    .actions {
      display: flex;
      gap: 8px;
    }

    .info-section, .edit-section, .danger-zone {
      margin: 24px 0;
    }

    .info-section h3, .edit-section h3, .danger-zone h3 {
      margin-bottom: 16px;
      color: #333;
    }

    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 16px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .label {
      font-weight: 500;
      color: #666;
      font-size: 0.9em;
    }

    .value {
      color: #333;
    }

    mat-divider {
      margin: 24px 0;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .checkbox {
      display: block;
      margin-bottom: 24px;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
    }

    .danger-zone {
      padding: 16px;
      border: 2px solid #f44336;
      border-radius: 4px;
      background-color: #ffebee;
    }
  `]
})
export class DocumentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private documentService = inject(DocumentService);
  private snackBar = inject(MatSnackBar);

  document: Document | null = null;
  editForm: FormGroup;
  loading = false;
  saving = false;

  constructor() {
    this.editForm = this.fb.group({
      title: [''],
      description: [''],
      isPublic: [false]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadDocument(+id);
    }
  }

  loadDocument(id: number): void {
    this.loading = true;
    this.documentService.getDocument(id).subscribe({
      next: (doc) => {
        this.document = doc;
        this.editForm.patchValue({
          title: doc.title,
          description: doc.description,
          isPublic: doc.isPublic
        });
        this.loading = false;
      },
      error: () => {
        this.snackBar.open('Failed to load document', 'Close', { duration: 3000 });
        this.loading = false;
        this.goBack();
      }
    });
  }

  onSave(): void {
    if (this.document && this.editForm.dirty) {
      this.saving = true;
      const request: DocumentUpdateRequest = this.editForm.value;

      this.documentService.updateDocument(this.document.id, request).subscribe({
        next: () => {
          this.snackBar.open('Document updated successfully', 'Close', { duration: 3000 });
          this.loadDocument(this.document!.id);
          this.saving = false;
        },
        error: () => {
          this.snackBar.open('Failed to update document', 'Close', { duration: 3000 });
          this.saving = false;
        }
      });
    }
  }

  resetForm(): void {
    if (this.document) {
      this.editForm.patchValue({
        title: this.document.title,
        description: this.document.description,
        isPublic: this.document.isPublic
      });
    }
  }

  downloadDocument(): void {
    if (this.document) {
      this.documentService.triggerDownload(this.document.id, this.document.fileName);
      this.snackBar.open('Downloading...', 'Close', { duration: 2000 });
    }
  }

  deleteDocument(): void {
    if (this.document && confirm(`Are you sure you want to delete "${this.document.title}"?`)) {
      this.documentService.deleteDocument(this.document.id).subscribe({
        next: () => {
          this.snackBar.open('Document deleted successfully', 'Close', { duration: 3000 });
          this.router.navigate(['/documents']);
        },
        error: () => {
          this.snackBar.open('Failed to delete document', 'Close', { duration: 3000 });
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/documents']);
  }

  formatFileSize(bytes: number): string {
    return this.documentService.formatFileSize(bytes);
  }
}