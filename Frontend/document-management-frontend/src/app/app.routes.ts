import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { noAuthGuard } from './core/guards/no-auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/documents',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [noAuthGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      }
    ]
  },
  {
    path: 'documents',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/documents/document-list/document-list.component').then(m => m.DocumentListComponent)
      },
      {
        path: 'upload',
        loadComponent: () => import('./features/documents/document-upload/document-upload.component').then(m => m.DocumentUploadComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/documents/document-detail/document-detail.component').then(m => m.DocumentDetailComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/documents'
  }
];