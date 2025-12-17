import { Routes } from '@angular/router';
import { Roles } from '../../core/models/common';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/dashboard/dashboard').then(m => m.DashboardComponent)
  }
];