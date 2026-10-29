import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./core/layout/layout').then((m) => m.Layout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'clientes',
      },
      {
        path: 'clientes',
        loadChildren: () => import('./features/clientes/routes'),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
