import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { Roles } from './core/models/common.models';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'dashboard',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'students',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/student/student.routes').then(m => m.STUDENT_ROUTES)
  },
  {
    path: 'bands',
    loadChildren: () => import('./features/band/band.routes').then(m => m.BAND_ROUTES)
  },
  {
    path: 'scholarships',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/scholarship/scholarship.routes').then(m => m.SCHOLARSHIP_ROUTES)
  },
  {
    path: 'guardian',
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: [Roles.Guardian] },
    loadChildren: () => import('./features/guardian/guardian.routes').then(m => m.GUARDIAN_ROUTES)
  },
  {
    path: 'director',
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: [Roles.Director, Roles.BandStaff] },
    loadChildren: () => import('./features/director/director.routes').then(m => m.DIRECTOR_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/profile/profile.routes').then(m => m.PROFILE_ROUTES)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./shared/components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  {
    path: 'not-found',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
  {
    path: '**',
    redirectTo: '/not-found'
  }
];