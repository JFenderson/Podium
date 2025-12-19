import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <div class="not-found-container">
      <mat-card class="not-found-card">
        <mat-card-content>
          <div class="icon-wrapper">
            <mat-icon class="error-icon">search_off</mat-icon>
          </div>
          <h1>404 - Page Not Found</h1>
          <p>The page you're looking for doesn't exist.</p>
          <div class="actions">
            <button mat-raised-button color="primary" routerLink="/dashboard">
              Go to Dashboard
            </button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      padding: 20px;
    }

    .not-found-card {
      max-width: 500px;
      text-align: center;
      padding: 40px;
    }

    .icon-wrapper {
      margin-bottom: 20px;
    }

    .error-icon {
      font-size: 80px;
      width: 80px;
      height: 80px;
      color: #ff9800;
    }

    h1 {
      margin: 20px 0 10px;
      color: #333;
    }

    p {
      color: #666;
      margin-bottom: 30px;
    }

    .actions {
      display: flex;
      justify-content: center;
      gap: 10px;
    }
  `]
})
export class NotFoundComponent {}