import { Routes } from '@angular/router';

export const DIRECTOR_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./components/director-dashboard/director-dashboard').then(m => m.DirectorDashboardComponent)
  }
];