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

ng g c features/student/student-list/student-list --standalone
ng g c features/student/student-detail/student-detail --standalone
ng g c features/student/student-profile/student-profile --standalone
ng g c features/band/band-list/band-list --standalone
ng g c features/band/band-detail/band-detail --standalone
ng g c features/scholarship/scholarship-list/scholarship-list --standalone
ng g c features/scholarship/create-offer/create-offer --standalone
ng g c features/scholarship/offer-detail/offer-detail --standalone
ng g c features/scholarship/my-offers/my-offers --standalone
ng g c layout/header --standalone
ng g c layout/sidebar --standalone
ng g c layout/footer --standalone