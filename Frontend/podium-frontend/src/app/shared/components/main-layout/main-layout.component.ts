import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Observable } from 'rxjs';
import { User } from '../../../core/models/auth.models';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet],
  templateUrl: './main-layout.component.html'
})
export class MainLayoutComponent {
  private authService = inject(AuthService);
  
  currentUser$: Observable<User | null> = this.authService.currentUser$;
  
  // Expose helper to template
  hasRole(role: string): boolean {
    return this.authService.hasRole([role]);
  }

  logout() {
    this.authService.logout();
  }
}