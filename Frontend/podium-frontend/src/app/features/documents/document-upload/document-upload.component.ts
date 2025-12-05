import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { DocumentService } from '../../../core/services/document.service';
import { DocumentUploadRequest } from '../../../core/models/document.model';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatIconModule
  ],
  template: `
    <div class="upload-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Upload Document</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="uploadForm" (ngSubmit)="onSubmit()">
            <!-- File Upload -->
            <div class="file-upload-section">
              <button mat-raised-button type="button" (click)="fileInput.click()">
                <mat-icon>attach_file</mat-icon>
                Choose File
              </button>
              <input #fileInput type="file" style="display: none" (change)="onFileSelected($event)">
              
              <div *ngIf="selectedFile" class="file-info">
                <mat-icon>insert_drive_file</mat-icon>
                <span>{{ selectedFile.name }}</span>
                <span class="file-size">({{ formatFileSize(selectedFile.size) }})</span>
              </div>
              
              <div *ngIf="!selectedFile" class="no-file">
                No file selected
              </div>
            </div>

            <!-- Title -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Title</mat-label>
              <input matInput formControlName="title" required>
              <mat-error *ngIf="uploadForm.get('title')?.hasError('required')">
                Title is required
              </mat-error>
            </mat-form-field>

            <!-- Description -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Description</mat-label>
              <textarea matInput formControlName="description" rows="4"></textarea>
            </mat-form-field>

            <!-- Is Public -->
            <mat-checkbox formControlName="isPublic" class="checkbox">
              Make this document public
            </mat-checkbox>

            <!-- Actions -->
            <div class="actions">
              <button mat-button type="button" (click)="cancel()">Cancel</button>
              <button mat-raised-button color="primary" type="submit" 
                      [disabled]="loading || !uploadForm.valid || !selectedFile">
                <span *ngIf="!loading">Upload</span>
                <mat-spinner *ngIf="loading" diameter="20"></mat-spinner>
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .upload-container {
      padding: 24px;
      max-width: 800px;
      margin: 0 auto;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .file-upload-section {
      margin-bottom: 24px;
    }

    .file-info {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 16px;
      padding: 12px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }

    .file-size {
      color: #666;
      font-size: 0.9em;
    }

    .no-file {
      margin-top: 8px;
      color: #666;
      font-style: italic;
    }

    .checkbox {
      display: block;
      margin-bottom: 24px;
    }

    .actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
    }
  `]
})
export class DocumentUploadComponent {
  private fb = inject(FormBuilder);
  private documentService = inject(DocumentService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  uploadForm: FormGroup;
  selectedFile: File | null = null;
  loading = false;

  constructor() {
    this.uploadForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      isPublic: [false]
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      
      // Auto-populate title if empty
      if (!this.uploadForm.get('title')?.value) {
        const fileName = this.selectedFile.name.replace(/\.[^/.]+$/, '');
        this.uploadForm.patchValue({ title: fileName });
      }
    }
  }

  onSubmit(): void {
    if (this.uploadForm.valid && this.selectedFile) {
      this.loading = true;

      const request: DocumentUploadRequest = {
        file: this.selectedFile,
        title: this.uploadForm.value.title,
        description: this.uploadForm.value.description,
        isPublic: this.uploadForm.value.isPublic
      };

      this.documentService.uploadDocument(request).subscribe({
        next: () => {
          this.snackBar.open('Document uploaded successfully!', 'Close', { duration: 3000 });
          this.router.navigate(['/documents']);
        },
        error: (error) => {
          this.loading = false;
          const errorMessage = error.error?.error || 'Upload failed. Please try again.';
          this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/documents']);
  }

  formatFileSize(bytes: number): string {
    return this.documentService.formatFileSize(bytes);
  }
}