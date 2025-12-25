import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service'; // Adjust path as needed

@Component({
  selector: 'app-guardian',
  standalone: true,
  imports: [CommonModule, RouterOutlet ],
  templateUrl: './guardian-layout.html',
  styles: []
})
export class GuardianLayoutComponent {
  private authService = inject(AuthService);
  
  isMobileMenuOpen = false;

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
  }

  logout() {
    this.authService.logout();
  }
}