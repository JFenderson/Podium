import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { Roles } from './core/models/common.models';

export const routes: Routes = [
  // Root path - redirect to login if not authenticated
  {
    path: '',
    loadComponent: () => import('./shared/components/landing/landing.component').then(m => m.LandingComponent)
  },
  // Login page - no guard needed
  {
    path: 'login',
    loadComponent: () => import('./features/auth/components/login/login').then(m => m.LoginComponent)
  },
  // Register page - no guard needed
  {
    path: 'register',
    loadComponent: () => import('./features/auth/components/register/register.component').then(m => m.RegisterComponent)
  },
  // Auth routes (login, register, forgot password, etc)
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  // Dashboard - requires authentication
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  // Students - requires authentication
  {
    path: 'students',
    canActivate: [authGuard],
    loadChildren: () => import('./features/student/student.routes').then(m => m.STUDENT_ROUTES)
  },
  // Bands - public access
  {
    path: 'bands',
    loadChildren: () => import('./features/band/band.routes').then(m => m.BAND_ROUTES)
  },
  // Scholarships - requires authentication
  {
    path: 'scholarships',
    canActivate: [authGuard],
    loadChildren: () => import('./features/scholarship/scholarship.routes').then(m => m.SCHOLARSHIP_ROUTES)
  },
  // Guardian - requires Guardian role
  {
    path: 'guardian',
    canActivate: [authGuard, roleGuard],
    data: { roles: [Roles.Guardian] },
    loadChildren: () => import('./features/guardian/guardian.routes').then(m => m.GUARDIAN_ROUTES)
  },
  // Director/Staff - requires Director or BandStaff role
  {
    path: 'director',
    canActivate: [authGuard, roleGuard],
    data: { roles: [Roles.Director, Roles.BandStaff] },
    loadChildren: () => import('./features/director/director.routes').then(m => m.DIRECTOR_ROUTES)
  },
  // Profile - requires authentication
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/profile/profile.routes').then(m => m.PROFILE_ROUTES)
  },
  // Unauthorized page
  {
    path: 'unauthorized',
    loadComponent: () => import('./shared/components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  // Not found page
  {
    path: 'not-found',
    loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
  // Catch all - redirect to not found
  {
    path: '**',
    redirectTo: '/not-found'
  }
];