import { Routes } from '@angular/router';
import { Roles } from '../../core/models/common.models';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/dashboard/dashboard.component').then(m => m.DashboardComponent)
  }
];