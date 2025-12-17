import { Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { RoleGuard } from '../../core/guards/role.guard';
import { Roles, Permissions } from '../../core/models/common.models';

export const STUDENT_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    loadComponent: () => import('./components/student-list/student-list.component').then(m => m.StudentListComponent),
    data: { permissions: [Permissions.ViewStudents] }
  },
  {
    path: 'profile',
    canActivate: [RoleGuard],
    data: { roles: [Roles.Student] },
    loadComponent: () => import('./components/student-profile/student-profile.component').then(m => m.StudentProfileComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/student-detail/student-detail.component').then(m => m.StudentDetailComponent)
  }
];