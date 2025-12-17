import { Routes } from '@angular/router';

export const BAND_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    loadComponent: () => import('./components/band-list/band-list').then(m => m.BandListComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./components/band-detail/band-detail').then(m => m.BandDetailComponent)
  }
];