import { Routes } from '@angular/router';
import { RoleGuard } from '../../core/guards/role.guard';
import { Roles, Permissions } from '../../core/models/common.models';

export const SCHOLARSHIP_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    loadComponent: () => import('./components/scholarship-list/scholarship-list.component').then(m => m.ScholarshipListComponent)
  },
  {
    path: 'my-offers',
    canActivate: [RoleGuard],
    data: { roles: [Roles.Student] },
    loadComponent: () => import('./components/my-offers/my-offers.component').then(m => m.MyOffersComponent)
  },
  {
    path: 'create',
    canActivate: [RoleGuard],
    data: { permissions: [Permissions.SendOffers] },
    loadComponent: () => import('./components/create-offer/create-offer.component').then(m => m.CreateOfferComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/offer-detail/offer-detail.component').then(m => m.OfferDetailComponent)
  }
];