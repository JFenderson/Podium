import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule
  ],
  template: `
    <div class="app-container">
      <mat-toolbar color="primary" *ngIf="authService.isAuthenticated()">
        <span class="app-title">Document Management</span>
        <span class="spacer"></span>
        
        <button mat-button routerLink="/documents">
          <mat-icon>folder</mat-icon>
          Documents
        </button>
        
        <button mat-button routerLink="/documents/upload">
          <mat-icon>upload_file</mat-icon>
          Upload
        </button>
        
        <button mat-icon-button [matMenuTriggerFor]="userMenu">
          <mat-icon>account_circle</mat-icon>
        </button>
        
        <mat-menu #userMenu="matMenu">
          <button mat-menu-item disabled *ngIf="currentUser$ | async as user">
            <mat-icon>person</mat-icon>
            <span>{{ user.firstName }} {{ user.lastName }}</span>
          </button>
          <button mat-menu-item (click)="logout()">
            <mat-icon>logout</mat-icon>
            <span>Logout</span>
          </button>
        </mat-menu>
      </mat-toolbar>
      
      <main class="content">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app-container {
      height: 100vh;
      display: flex;
      flex-direction: column;
    }
    
    .app-title {
      font-size: 20px;
      font-weight: 500;
    }
    
    .spacer {
      flex: 1 1 auto;
    }
    
    .content {
      flex: 1;
      overflow-y: auto;
      background-color: #f5f5f5;
    }
  `]
})
export class AppComponent {
  authService = inject(AuthService);
  private router = inject(Router);
  
  currentUser$ = this.authService.currentUser$;

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/auth/login']);
      }
    });
  }
}