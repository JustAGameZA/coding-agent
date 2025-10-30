import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';

/**
 * Admin module routes - all protected by Admin role guard
 */
export const adminRoutes: Routes = [
  {
    path: '',
    redirectTo: 'infrastructure',
    pathMatch: 'full'
  },
  {
    path: 'infrastructure',
    loadComponent: () => import('./infrastructure/infrastructure.component').then(m => m.InfrastructureComponent),
    canActivate: [roleGuard('Admin')]
  },
  {
    path: 'users',
    loadComponent: () => import('./users/user-list.component').then(m => m.UserListComponent),
    canActivate: [roleGuard('Admin')]
  }
];
