import { Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { RoleGuard } from '../../core/guards/role.guard';
import { Roles, Permissions } from '../../core/models/common';

export const STUDENT_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
    {
    path: 'dashboard',
    loadComponent: () => import('./components/student-dashboard/student-dashboard').then(m => m.StudentDashboardComponent)
  },
  {
    path: 'list',
    loadComponent: () => import('./components/student-list/student-list').then(m => m.StudentListComponent),
    data: { permissions: [Permissions.ViewStudents] }
  },
  {
    path: 'profile',
    canActivate: [RoleGuard],
    data: { roles: [Roles.Student] },
    loadComponent: () => import('./components/student-profile/student-profile').then(m => m.StudentProfileComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/student-detail/student-detail').then(m => m.StudentDetailComponent)
  }
];