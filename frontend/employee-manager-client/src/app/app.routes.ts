import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'org-chart' },
  {
    path: 'org-chart',
    title: 'Organisation chart',
    loadComponent: () => import('./features/org-chart/org-chart').then((m) => m.OrgChart),
  },
];
