import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { LoginResponse } from '../../core/models/auth.models';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let router: jasmine.SpyObj<Router>;
  let activatedRoute: any;

  const mockLoginResponse: LoginResponse = {
    token: 'mock-jwt-token',
    refreshToken: 'mock-refresh-token',
    expiresIn: 3600,
    user: {
      id: '123',
      username: 'testuser',
      email: 'test@example.com',
      roles: ['user']
    }
  };

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['login', 'isAuthenticated']);
    const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['info', 'error']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    
    activatedRoute = {
      snapshot: {
        queryParamMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRoute }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    authService.isAuthenticated.and.returnValue(false);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation', () => {
    it('should initialize with empty form', () => {
      expect(component['loginForm'].get('username')?.value).toBe('');
      expect(component['loginForm'].get('password')?.value).toBe('');
      expect(component['loginForm'].get('rememberMe')?.value).toBe(false);
    });

    it('should require username', () => {
      const usernameControl = component['loginForm'].get('username');
      usernameControl?.setValue('');
      expect(usernameControl?.hasError('required')).toBe(true);
      
      usernameControl?.setValue('testuser');
      expect(usernameControl?.hasError('required')).toBe(false);
    });

    it('should require password', () => {
      const passwordControl = component['loginForm'].get('password');
      passwordControl?.setValue('');
      expect(passwordControl?.hasError('required')).toBe(true);
      
      passwordControl?.setValue('password123');
      expect(passwordControl?.hasError('required')).toBe(false);
    });

    it('should validate email format when username contains @', () => {
      const usernameControl = component['loginForm'].get('username');
      
      usernameControl?.setValue('test@example.com');
      fixture.detectChanges();
      
      expect(usernameControl?.hasError('email')).toBe(false);
      
      usernameControl?.setValue('invalid-email@');
      fixture.detectChanges();
      
      expect(usernameControl?.hasError('email')).toBe(true);
    });

    it('should not validate email format for non-email usernames', () => {
      const usernameControl = component['loginForm'].get('username');
      
      usernameControl?.setValue('username123');
      fixture.detectChanges();
      
      expect(usernameControl?.hasError('email')).toBe(false);
    });

    it('should disable submit button when form is invalid', () => {
      component['loginForm'].patchValue({ username: '', password: '' });
      fixture.detectChanges();
      
      const submitButton = fixture.nativeElement.querySelector('[data-testid="login-button"]');
      expect(submitButton.disabled).toBe(true);
    });

    it('should enable submit button when form is valid', () => {
      component['loginForm'].patchValue({ 
        username: 'testuser', 
        password: 'password123' 
      });
      fixture.detectChanges();
      
      const submitButton = fixture.nativeElement.querySelector('[data-testid="login-button"]');
      expect(submitButton.disabled).toBe(false);
    });
  });

  describe('Login Submission', () => {
    beforeEach(() => {
      component['loginForm'].patchValue({
        username: 'testuser',
        password: 'password123',
        rememberMe: true
      });
    });

    it('should call authService.login with correct credentials', () => {
      authService.login.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      expect(authService.login).toHaveBeenCalledWith('testuser', 'password123', true);
    });

    it('should show loading state during login', () => {
      authService.login.and.returnValue(of(mockLoginResponse).pipe());
      
      expect(component['loading']()).toBe(false);
      
      component['onSubmit']();
      
      // Loading state is set synchronously
      expect(component['loading']()).toBe(true);
    });

    it('should navigate to dashboard on successful login', (done) => {
      authService.login.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
        expect(component['loading']()).toBe(false);
        done();
      }, 0);
    });

    it('should show success notification on successful login', (done) => {
      authService.login.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(notificationService.info).toHaveBeenCalledWith('Welcome back, testuser!');
        done();
      }, 0);
    });

    it('should handle returnUrl from query params', (done) => {
      activatedRoute.snapshot.queryParamMap.get.and.returnValue('/tasks');
      authService.login.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(router.navigate).toHaveBeenCalledWith(['/tasks']);
        done();
      }, 0);
    });

    it('should display error message on login failure', (done) => {
      const error = new Error('Invalid username or password');
      authService.login.and.returnValue(throwError(() => error));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(component['errorMessage']()).toBe('Invalid username or password');
        expect(component['loading']()).toBe(false);
        done();
      }, 0);
    });

    it('should show error notification on login failure', (done) => {
      const error = new Error('Invalid username or password');
      authService.login.and.returnValue(throwError(() => error));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(notificationService.error).toHaveBeenCalledWith('Invalid username or password');
        done();
      }, 0);
    });

    it('should clear password field on error', (done) => {
      const error = new Error('Invalid username or password');
      authService.login.and.returnValue(throwError(() => error));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(component['loginForm'].get('password')?.value).toBe('');
        done();
      }, 0);
    });
  });

  describe('Password Visibility Toggle', () => {
    it('should toggle password visibility', () => {
      expect(component['hidePassword']()).toBe(true);
      
      component['togglePasswordVisibility']();
      expect(component['hidePassword']()).toBe(false);
      
      component['togglePasswordVisibility']();
      expect(component['hidePassword']()).toBe(true);
    });
  });

  describe('Already Authenticated', () => {
    it('should redirect to dashboard if already authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);
      
      component.ngOnInit();
      
      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });
  });

  describe('Data Test IDs', () => {
    it('should have data-testid attributes for E2E testing', () => {
      const compiled = fixture.nativeElement;
      
      expect(compiled.querySelector('[data-testid="login-container"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="login-card"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="login-title"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="login-form"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="username-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="password-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="remember-me-checkbox"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="login-button"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="register-link"]')).toBeTruthy();
    });
  });
});
