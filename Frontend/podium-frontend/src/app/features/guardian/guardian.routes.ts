import { Routes } from '@angular/router';
import { GuardianDashboardComponent } from './components/guardian-dashboard/guardian-dashboard.component';
import { LinkStudentComponent } from './components/link-student/link-student.component';
import { GuardianLayoutComponent } from '../../layout/guardian-layout/guardian-layout';
import { GuardianProfileComponent } from './components/guardian-profile/guardian-profile.component';
import { GuardianStudentDetailsComponent } from './components/guardian-student-details/guardian-student-details.component';

export const GUARDIAN_ROUTES: Routes = [
  {
    path: '',
    component: GuardianLayoutComponent, // Wraps all child routes
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: GuardianDashboardComponent },
      { path: 'link-student', component: LinkStudentComponent },
      { path: 'profile', component: GuardianProfileComponent },
      {
        path: 'student/:id', // matches routerLink from dashboard
        component: GuardianStudentDetailsComponent,
      },
    ],
  },
];
