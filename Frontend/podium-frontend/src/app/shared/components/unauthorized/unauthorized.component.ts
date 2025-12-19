import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';


@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <div class="unauthorized-container">
      <mat-card class="unauthorized-card">
        <mat-card-content>
          <div class="icon-wrapper">
            <mat-icon class="error-icon">block</mat-icon>
          </div>
          <h1>Access Denied</h1>
          <p>You don't have permission to access this page.</p>
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
    .unauthorized-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      padding: 20px;
    }

    .unauthorized-card {
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
      color: #f44336;
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
export class UnauthorizedComponent {}