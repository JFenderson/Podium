import { Routes } from '@angular/router';

export const GUARDIAN_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/guardian-dashboard/guardian-dashboard.component').then(m => m.GuardianDashboardComponent)
  }
];