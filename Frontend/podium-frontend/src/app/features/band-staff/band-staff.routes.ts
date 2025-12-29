import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';
import { Roles } from '../../core/models/common.models';

export const BAND_STAFF_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    canActivate: [roleGuard],
    data: { roles: [Roles.BandStaff, Roles.Director] },
    loadComponent: () => import('./components/band-staff-dashboard/band-staff-dashboard.component')
      .then(m => m.BandStaffDashboardComponent)
  }
];