import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { Roles, Permissions } from '../../core/models/common.models';

export const STUDENT_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
    {
    path: 'dashboard',
    loadComponent: () => import('./components/student-dashboard/student-dashboard.component').then(m => m.StudentDashboardComponent)
  },
  {
    path: 'list',
    loadComponent: () => import('./components/student-list/student-list.component').then(m => m.StudentListComponent),
    data: { permissions: [Permissions.ViewStudents] }
  },
  {
    path: 'profile',
    canActivate: [roleGuard],
    data: { roles: [Roles.Student] },
    loadComponent: () => import('./components/student-profile/student-profile.component').then(m => m.StudentProfileComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/student-detail/student-detail.component').then(m => m.StudentDetailComponent)
  }
];