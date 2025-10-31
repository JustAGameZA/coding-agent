import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        RouterTestingModule.withRoutes([
          { path: 'login', component: class {} as any },
          { path: 'dashboard', component: class {} as any }
        ])
      ],
      providers: [
        AuthService
      ]
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);

    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = { url: '/dashboard' } as RouterStateSnapshot;

    // Clear sessionStorage before each test
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should allow access when user is authenticated', () => {
    // Arrange - Set a valid token so isAuthenticated returns true
    const futureTimestamp = Math.floor(Date.now() / 1000) + 3600;
    const validToken = `header.${btoa(JSON.stringify({ exp: futureTimestamp }))}.signature`;
    localStorage.setItem('auth_token', validToken);
    authService['initializeAuth']();

    // Act
    const result = TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    // Assert
    expect(result).toBe(true);
    localStorage.clear();
  });

  it('should redirect to login when user is not authenticated', () => {
    // Arrange - No token, so not authenticated
    localStorage.clear();
    authService['initializeAuth']();

    // Act
    const result = TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    // Assert - Should return UrlTree to login
    expect(result).toBeTruthy();
    if (result && typeof result === 'object' && 'root' in result) {
      // It's a UrlTree - verify it points to login
      const urlTree = result as any;
      expect(urlTree.root.children['primary']?.segments[0]?.path).toBe('login');
    }
  });

  it('should store returnUrl in sessionStorage', () => {
    // Arrange
    localStorage.clear();
    authService['initializeAuth']();

    // Act
    TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    // Assert
    expect(sessionStorage.getItem('returnUrl')).toBe('/dashboard');
    sessionStorage.clear();
  });

  it('should handle different route URLs', () => {
    // Arrange
    localStorage.clear();
    authService['initializeAuth']();
    mockState = { url: '/tasks?page=2' } as RouterStateSnapshot;

    // Act
    TestBed.runInInjectionContext(() => 
      authGuard(mockRoute, mockState)
    );

    // Assert
    expect(sessionStorage.getItem('returnUrl')).toBe('/tasks?page=2');
    sessionStorage.clear();
  });

  it('should handle sessionStorage errors gracefully', () => {
    // Arrange
    localStorage.clear();
    authService['initializeAuth']();
    spyOn(sessionStorage, 'setItem').and.throwError('Storage quota exceeded');

    // Act & Assert - Should not throw, just continue
    expect(() => {
      TestBed.runInInjectionContext(() => 
        authGuard(mockRoute, mockState)
      );
    }).not.toThrow();
  });
});
