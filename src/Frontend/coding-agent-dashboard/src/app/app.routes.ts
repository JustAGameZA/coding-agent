import { Routes } from '@angular/router';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadComponent: () => import('./features/chat/chat.component').then(m => m.ChatComponent),
    canActivate: [authGuard]
  },
  {
    path: 'tasks',
    loadComponent: () => import('./features/tasks/tasks.component').then(m => m.TasksComponent),
    canActivate: [authGuard]
  }
];
