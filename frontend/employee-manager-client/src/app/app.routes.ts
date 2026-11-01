import { Routes } from '@angular/router';

import { adminGuard } from './core/auth/admin.guard';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'org-chart' },
  {
    path: 'login',
    title: 'Sign in',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    path: 'org-chart',
    title: 'Organisation chart',
    canActivate: [authGuard],
    loadComponent: () => import('./features/org-chart/org-chart').then((m) => m.OrgChart),
  },
  {
    path: 'employees',
    title: 'Employees',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./features/employees/employees').then((m) => m.Employees),
  },
];
