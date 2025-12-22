import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service';

@Component({
  selector: 'app-guardian-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="d-flex" id="wrapper">
      <div class="bg-light border-end" id="sidebar-wrapper" style="width: 250px; min-height: 100vh;">
        <div class="sidebar-heading border-bottom bg-white p-3">
            <strong class="text-primary">Podium Guardian</strong>
        </div>
        <div class="list-group list-group-flush">
          <a routerLink="/guardian/dashboard" 
             routerLinkActive="active" 
             class="list-group-item list-group-item-action list-group-item-light p-3">
             <i class="fas fa-tachometer-alt me-2"></i> Dashboard
          </a>
          <a routerLink="/guardian/link-student" 
             routerLinkActive="active" 
             class="list-group-item list-group-item-action list-group-item-light p-3">
             <i class="fas fa-link me-2"></i> Link Student
          </a>
          <a routerLink="/guardian/profile" 
             routerLinkActive="active" 
             class="list-group-item list-group-item-action list-group-item-light p-3">
             <i class="fas fa-user-cog me-2"></i> My Profile
          </a>
           <a (click)="logout()" 
             class="list-group-item list-group-item-action list-group-item-light p-3 text-danger" 
             style="cursor: pointer;">
             <i class="fas fa-sign-out-alt me-2"></i> Logout
          </a>
        </div>
      </div>

      <div id="page-content-wrapper" class="w-100">
        <nav class="navbar navbar-expand-lg navbar-light bg-white border-bottom px-3">
            <span class="navbar-brand">Guardian Portal</span>
        </nav>

        <div class="container-fluid p-4">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .list-group-item.active {
      background-color: #e9ecef;
      color: #0d6efd;
      border-color: #dee2e6;
      font-weight: 500;
    }
  `]
})
export class GuardianLayoutComponent {
  constructor(private auth: AuthService) {}
  
  logout() {
    this.auth.logout();
  }
}