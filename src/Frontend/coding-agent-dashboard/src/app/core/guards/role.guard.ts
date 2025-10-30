import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Functional route guard to protect role-based routes.
 * Redirects to /dashboard if user doesn't have required role.
 */
export const roleGuard = (requiredRole: string): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    
    const user = authService.currentUser();
    if (!user || !user.roles?.includes(requiredRole)) {
      return router.createUrlTree(['/dashboard']);
    }
    return true;
  };
};
