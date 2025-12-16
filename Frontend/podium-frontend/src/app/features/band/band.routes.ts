import { Routes } from '@angular/router';

export const BAND_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    loadComponent: () => import('./band-list/band-list/band-list.component').then(m => m.BandListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/band-detail/band-detail.component').then(m => m.BandDetailComponent)
  }
];