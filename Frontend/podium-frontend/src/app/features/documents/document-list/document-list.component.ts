import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { DocumentService } from '../../../core/services/document.service';
import { Document } from '../../../core/models/document.model';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatDialogModule
  ],
  template: `
    <div class="document-list-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>
            <div class="header-row">
              <h2>My Documents</h2>
              <button mat-raised-button color="primary" routerLink="/documents/upload">
                <mat-icon>upload_file</mat-icon>
                Upload Document
              </button>
            </div>
          </mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div *ngIf="loading" class="loading-container">
            <mat-spinner></mat-spinner>
          </div>

          <div *ngIf="!loading && documents.length === 0" class="empty-state">
            <mat-icon>folder_open</mat-icon>
            <h3>No documents yet</h3>
            <p>Upload your first document to get started</p>
            <button mat-raised-button color="primary" routerLink="/documents/upload">
              Upload Document
            </button>
          </div>

          <table mat-table [dataSource]="documents" *ngIf="!loading && documents.length > 0" class="documents-table">
            <!-- Title Column -->
            <ng-container matColumnDef="title">
              <th mat-header-cell *matHeaderCellDef>Title</th>
              <td mat-cell *matCellDef="let doc">{{ doc.title }}</td>
            </ng-container>

            <!-- File Name Column -->
            <ng-container matColumnDef="fileName">
              <th mat-header-cell *matHeaderCellDef>File Name</th>
              <td mat-cell *matCellDef="let doc">{{ doc.fileName }}</td>
            </ng-container>

            <!-- Size Column -->
            <ng-container matColumnDef="size">
              <th mat-header-cell *matHeaderCellDef>Size</th>
              <td mat-cell *matCellDef="let doc">{{ formatFileSize(doc.fileSizeInBytes) }}</td>
            </ng-container>

            <!-- Uploaded Date Column -->
            <ng-container matColumnDef="uploadedAt">
              <th mat-header-cell *matHeaderCellDef>Uploaded</th>
              <td mat-cell *matCellDef="let doc">{{ doc.uploadedAt | date:'short' }}</td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let doc">
                <button mat-icon-button [routerLink]="['/documents', doc.id]" matTooltip="View Details">
                  <mat-icon>visibility</mat-icon>
                </button>
                <button mat-icon-button (click)="downloadDocument(doc)" matTooltip="Download">
                  <mat-icon>download</mat-icon>
                </button>
                <button mat-icon-button (click)="deleteDocument(doc)" matTooltip="Delete" color="warn">
                  <mat-icon>delete</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator 
            *ngIf="!loading && totalCount > 0"
            [length]="totalCount"
            [pageSize]="pageSize"
            [pageSizeOptions]="[5, 10, 25, 50]"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .document-list-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 40px;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-state mat-icon {
      font-size: 72px;
      width: 72px;
      height: 72px;
      color: #ccc;
    }

    .documents-table {
      width: 100%;
      margin-top: 20px;
    }

    .mat-column-actions {
      width: 150px;
      text-align: right;
    }
  `]
})
export class DocumentListComponent implements OnInit {
  private documentService = inject(DocumentService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  documents: Document[] = [];
  displayedColumns = ['title', 'fileName', 'size', 'uploadedAt', 'actions'];
  loading = false;
  totalCount = 0;
  page = 1;
  pageSize = 10;

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.loading = true;
    this.documentService.getDocuments(this.page, this.pageSize).subscribe({
      next: (response) => {
        this.documents = response.data;
        this.totalCount = response.totalCount;
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        this.snackBar.open('Failed to load documents', 'Close', { duration: 3000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadDocuments();
  }

  downloadDocument(doc: Document): void {
    this.documentService.triggerDownload(doc.id, doc.fileName);
    this.snackBar.open('Downloading...', 'Close', { duration: 2000 });
  }

  deleteDocument(doc: Document): void {
    if (confirm(`Are you sure you want to delete "${doc.title}"?`)) {
      this.documentService.deleteDocument(doc.id).subscribe({
        next: () => {
          this.snackBar.open('Document deleted successfully', 'Close', { duration: 3000 });
          this.loadDocuments();
        },
        error: () => {
          this.snackBar.open('Failed to delete document', 'Close', { duration: 3000 });
        }
      });
    }
  }

  formatFileSize(bytes: number): string {
    return this.documentService.formatFileSize(bytes);
  }
}