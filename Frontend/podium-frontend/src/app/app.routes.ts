import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { Roles } from './core/models/common';

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
    canActivate: [authGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'students',
    canActivate: [authGuard],
    loadChildren: () => import('./features/student/student.routes').then(m => m.STUDENT_ROUTES)
  },
  {
    path: 'bands',
    loadChildren: () => import('./features/band/band.routes').then(m => m.BAND_ROUTES)
  },
  {
    path: 'scholarships',
    canActivate: [authGuard],
    loadChildren: () => import('./features/scholarship/scholarship.routes').then(m => m.SCHOLARSHIP_ROUTES)
  },
  {
    path: 'guardian',
    canActivate: [authGuard, roleGuard],
    data: { roles: [Roles.Guardian] },
    loadChildren: () => import('./features/guardian/guardian.routes').then(m => m.GUARDIAN_ROUTES)
  },
  {
    path: 'director',
    canActivate: [authGuard, roleGuard],
    data: { roles: [Roles.Director, Roles.BandStaff] },
    loadChildren: () => import('./features/director/director.routes').then(m => m.DIRECTOR_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/profile/profile.routes').then(m => m.PROFILE_ROUTES)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./shared/components/unauthorized/unauthorized').then(m => m.UnauthorizedComponent)
  },
  {
    path: 'not-found',
    loadComponent: () => import('./shared/components/not-found/not-found').then(m => m.NotFoundComponent)
  },
  {
    path: '**',
    redirectTo: '/not-found'
  }
];