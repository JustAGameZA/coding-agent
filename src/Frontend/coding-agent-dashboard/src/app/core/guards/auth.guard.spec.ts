import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated']);
    const routerSpy = jasmine.createSpyObj('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = { url: '/dashboard' } as RouterStateSnapshot;

    // Clear sessionStorage before each test
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should allow access when user is authenticated', () => {
    authService.isAuthenticated.and.returnValue(true);

    const result = TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    expect(result).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('should redirect to login when user is not authenticated', () => {
    authService.isAuthenticated.and.returnValue(false);
    const mockUrlTree = {} as any;
    router.createUrlTree.and.returnValue(mockUrlTree);

    const result = TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    expect(result).toBe(mockUrlTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/login'],
      { queryParams: { returnUrl: '/dashboard' } }
    );
  });

  it('should store returnUrl in sessionStorage', () => {
    authService.isAuthenticated.and.returnValue(false);
    router.createUrlTree.and.returnValue({} as any);

    TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    expect(sessionStorage.getItem('returnUrl')).toBe('/dashboard');
  });

  it('should handle different route URLs', () => {
    authService.isAuthenticated.and.returnValue(false);
    const mockUrlTree = {} as any;
    router.createUrlTree.and.returnValue(mockUrlTree);
    
    mockState = { url: '/tasks?page=2' } as RouterStateSnapshot;

    TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/login'],
      { queryParams: { returnUrl: '/tasks?page=2' } }
    );
    expect(sessionStorage.getItem('returnUrl')).toBe('/tasks?page=2');
  });

  it('should handle sessionStorage errors gracefully', () => {
    authService.isAuthenticated.and.returnValue(false);
    router.createUrlTree.and.returnValue({} as any);

    // Mock sessionStorage.setItem to throw error
    spyOn(sessionStorage, 'setItem').and.throwError('Storage quota exceeded');

    // Should not throw, just continue
    expect(() => {
      TestBed.runInInjectionContext(() => 
        authGuard(mockRoute, mockState)
      );
    }).not.toThrow();

    expect(router.createUrlTree).toHaveBeenCalled();
  });
});
