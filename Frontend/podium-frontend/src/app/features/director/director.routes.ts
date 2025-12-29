import { Routes } from '@angular/router';

export const DIRECTOR_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/director-dashboard/director-dashboard.component')
      .then(m => m.DirectorDashboardComponent)
  },
  // ADD THIS:
  {
    path: 'staff-dashboard',
    loadComponent: () => import('../band-staff/components/band-staff-dashboard/band-staff-dashboard.component')
      .then(m => m.BandStaffDashboardComponent)
  }
];