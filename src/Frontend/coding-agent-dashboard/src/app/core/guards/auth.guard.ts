import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Functional route guard to protect authenticated routes.
 * Redirects to /login with returnUrl query param if not authenticated.
 */
export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Store intended destination for post-login redirect
  const returnUrl = state.url;
  
  // Store in sessionStorage so it survives redirects but not browser close
  try {
    sessionStorage.setItem('returnUrl', returnUrl);
  } catch {
    // Ignore storage errors
  }

  // Redirect to login with returnUrl as query param
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl }
  });
};
