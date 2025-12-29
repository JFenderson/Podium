import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service';
import { Roles } from '../../core/models/common.models';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/dashboard/dashboard.component')
      .then(m => m.DashboardComponent),
    canActivate: [() => {
      const authService = inject(AuthService);
      const router = inject(Router);
      
      // Redirect to appropriate dashboard
      if (authService.hasRole(Roles.Director)) {
        router.navigate(['/director/dashboard']);
        return false;
      }
      if (authService.hasRole(Roles.BandStaff)) {
        router.navigate(['/staff/dashboard']);
        return false;
      }
      if (authService.hasRole(Roles.Student)) {
        router.navigate(['/students/dashboard']);
        return false;
      }
      if (authService.hasRole(Roles.Guardian)) {
        router.navigate(['/guardian/dashboard']);
        return false;
      }
      
      return true; // Generic dashboard
    }]
  }
];