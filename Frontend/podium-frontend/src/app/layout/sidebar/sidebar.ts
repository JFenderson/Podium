// sidebar.ts - FIXED VERSION
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss'],
})
export class Sidebar {
  private authService = inject(AuthService);

  // Add hasRole method for template
  hasRole(role: string): boolean {
    return this.authService.hasRole(role);
  }

  get isStaff(): boolean {
    return this.authService.hasAnyRole(['Director', 'BandStaff']);
  }

  get isGuardian(): boolean {
    return this.authService.hasRole('Guardian');
  }

  get isStudent(): boolean {
    return this.authService.hasRole('Student');
  }

  logout(): void {
    this.authService.logout();
  }
}