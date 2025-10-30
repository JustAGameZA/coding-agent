import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { LoginResponse } from '../../core/models/auth.models';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let router: jasmine.SpyObj<Router>;

  const mockLoginResponse: LoginResponse = {
    token: 'mock-jwt-token',
    refreshToken: 'mock-refresh-token',
    expiresIn: 3600,
    user: {
      id: '123',
      username: 'newuser',
      email: 'new@example.com',
      roles: ['user']
    }
  };

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('AuthService', ['register', 'isAuthenticated']);
    const notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['info', 'error']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        RegisterComponent,
        ReactiveFormsModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: NotificationService, useValue: notificationServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    authService.isAuthenticated.and.returnValue(false);

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation', () => {
    it('should initialize with empty form', () => {
      expect(component['registerForm'].get('username')?.value).toBe('');
      expect(component['registerForm'].get('email')?.value).toBe('');
      expect(component['registerForm'].get('password')?.value).toBe('');
      expect(component['registerForm'].get('confirmPassword')?.value).toBe('');
    });

    it('should require username', () => {
      const usernameControl = component['registerForm'].get('username');
      usernameControl?.setValue('');
      expect(usernameControl?.hasError('required')).toBe(true);
      
      usernameControl?.setValue('testuser');
      expect(usernameControl?.hasError('required')).toBe(false);
    });

    it('should enforce username minimum length', () => {
      const usernameControl = component['registerForm'].get('username');
      usernameControl?.setValue('ab');
      expect(usernameControl?.hasError('minlength')).toBe(true);
      
      usernameControl?.setValue('abc');
      expect(usernameControl?.hasError('minlength')).toBe(false);
    });

    it('should validate username pattern', () => {
      const usernameControl = component['registerForm'].get('username');
      
      usernameControl?.setValue('valid_user123');
      expect(usernameControl?.hasError('pattern')).toBe(false);
      
      usernameControl?.setValue('invalid-user');
      expect(usernameControl?.hasError('pattern')).toBe(true);
      
      usernameControl?.setValue('invalid user');
      expect(usernameControl?.hasError('pattern')).toBe(true);
    });

    it('should require email', () => {
      const emailControl = component['registerForm'].get('email');
      emailControl?.setValue('');
      expect(emailControl?.hasError('required')).toBe(true);
      
      emailControl?.setValue('test@example.com');
      expect(emailControl?.hasError('required')).toBe(false);
    });

    it('should validate email format', () => {
      const emailControl = component['registerForm'].get('email');
      
      emailControl?.setValue('valid@example.com');
      expect(emailControl?.hasError('email')).toBe(false);
      
      emailControl?.setValue('invalid-email');
      expect(emailControl?.hasError('email')).toBe(true);
    });

    it('should require password', () => {
      const passwordControl = component['registerForm'].get('password');
      passwordControl?.setValue('');
      expect(passwordControl?.hasError('required')).toBe(true);
      
      passwordControl?.setValue('ValidPass123!');
      expect(passwordControl?.hasError('required')).toBe(false);
    });

    it('should validate password strength - minimum length', () => {
      const passwordControl = component['registerForm'].get('password');
      
      passwordControl?.setValue('Short1!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(true);
      
      passwordControl?.setValue('LongPass123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(false);
    });

    it('should validate password strength - uppercase', () => {
      const passwordControl = component['registerForm'].get('password');
      
      passwordControl?.setValue('lowercase123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(true);
      
      passwordControl?.setValue('Uppercase123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(false);
    });

    it('should validate password strength - lowercase', () => {
      const passwordControl = component['registerForm'].get('password');
      
      passwordControl?.setValue('UPPERCASE123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(true);
      
      passwordControl?.setValue('UPPERlower123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(false);
    });

    it('should validate password strength - number', () => {
      const passwordControl = component['registerForm'].get('password');
      
      passwordControl?.setValue('NoNumbers!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(true);
      
      passwordControl?.setValue('WithNumber1!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(false);
    });

    it('should validate password strength - special character', () => {
      const passwordControl = component['registerForm'].get('password');
      
      passwordControl?.setValue('NoSpecial123');
      expect(passwordControl?.hasError('passwordStrength')).toBe(true);
      
      passwordControl?.setValue('WithSpecial123!');
      expect(passwordControl?.hasError('passwordStrength')).toBe(false);
    });

    it('should require confirm password', () => {
      const confirmPasswordControl = component['registerForm'].get('confirmPassword');
      confirmPasswordControl?.setValue('');
      expect(confirmPasswordControl?.hasError('required')).toBe(true);
      
      confirmPasswordControl?.setValue('password');
      expect(confirmPasswordControl?.hasError('required')).toBe(false);
    });

    it('should validate password match', () => {
      component['registerForm'].patchValue({
        password: 'ValidPass123!',
        confirmPassword: 'DifferentPass123!'
      });
      
      expect(component['registerForm'].hasError('passwordMismatch')).toBe(true);
      
      component['registerForm'].patchValue({
        password: 'ValidPass123!',
        confirmPassword: 'ValidPass123!'
      });
      
      expect(component['registerForm'].hasError('passwordMismatch')).toBe(false);
    });

    it('should disable submit button when form is invalid', () => {
      component['registerForm'].patchValue({
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
      });
      fixture.detectChanges();
      
      const submitButton = fixture.nativeElement.querySelector('[data-testid="register-button"]');
      expect(submitButton.disabled).toBe(true);
    });

    it('should enable submit button when form is valid', () => {
      component['registerForm'].patchValue({
        username: 'validuser',
        email: 'valid@example.com',
        password: 'ValidPass123!',
        confirmPassword: 'ValidPass123!'
      });
      fixture.detectChanges();
      
      const submitButton = fixture.nativeElement.querySelector('[data-testid="register-button"]');
      expect(submitButton.disabled).toBe(false);
    });
  });

  describe('Password Strength Indicator', () => {
    it('should calculate weak password strength', () => {
      component['calculatePasswordStrength']('short');
      expect(component['passwordStrength']()).toBe('weak');
    });

    it('should calculate fair password strength', () => {
      component['calculatePasswordStrength']('Longer123');
      expect(component['passwordStrength']()).toBe('fair');
    });

    it('should calculate good password strength', () => {
      component['calculatePasswordStrength']('GoodPass123!');
      expect(component['passwordStrength']()).toBe('good');
    });

    it('should calculate strong password strength', () => {
      component['calculatePasswordStrength']('VeryStrongPass123!@#');
      expect(component['passwordStrength']()).toBe('strong');
    });

    it('should return correct strength percentage', () => {
      component['passwordStrength'].set('weak');
      expect(component['getPasswordStrengthPercentage']()).toBe(25);
      
      component['passwordStrength'].set('fair');
      expect(component['getPasswordStrengthPercentage']()).toBe(50);
      
      component['passwordStrength'].set('good');
      expect(component['getPasswordStrengthPercentage']()).toBe(75);
      
      component['passwordStrength'].set('strong');
      expect(component['getPasswordStrengthPercentage']()).toBe(100);
    });
  });

  describe('Registration Submission', () => {
    beforeEach(() => {
      component['registerForm'].patchValue({
        username: 'newuser',
        email: 'new@example.com',
        password: 'ValidPass123!',
        confirmPassword: 'ValidPass123!'
      });
    });

    it('should call authService.register with correct data', () => {
      authService.register.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      expect(authService.register).toHaveBeenCalledWith({
        username: 'newuser',
        email: 'new@example.com',
        password: 'ValidPass123!'
      });
    });

    it('should show loading state during registration', () => {
      authService.register.and.returnValue(of(mockLoginResponse));
      
      expect(component['loading']()).toBe(false);
      
      component['onSubmit']();
      
      expect(component['loading']()).toBe(true);
    });

    it('should navigate to dashboard on successful registration', (done) => {
      authService.register.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
        expect(component['loading']()).toBe(false);
        done();
      }, 0);
    });

    it('should show success notification on successful registration', (done) => {
      authService.register.and.returnValue(of(mockLoginResponse));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(notificationService.info).toHaveBeenCalledWith('Welcome, newuser! Your account has been created.');
        done();
      }, 0);
    });

    it('should display error message on registration failure', (done) => {
      const error = new Error('Username or email already exists');
      authService.register.and.returnValue(throwError(() => error));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(component['errorMessage']()).toBe('Username or email already exists');
        expect(component['loading']()).toBe(false);
        done();
      }, 0);
    });

    it('should show error notification on registration failure', (done) => {
      const error = new Error('Username or email already exists');
      authService.register.and.returnValue(throwError(() => error));
      
      component['onSubmit']();
      
      setTimeout(() => {
        expect(notificationService.error).toHaveBeenCalledWith('Username or email already exists');
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

    it('should toggle confirm password visibility', () => {
      expect(component['hideConfirmPassword']()).toBe(true);
      
      component['toggleConfirmPasswordVisibility']();
      expect(component['hideConfirmPassword']()).toBe(false);
      
      component['toggleConfirmPasswordVisibility']();
      expect(component['hideConfirmPassword']()).toBe(true);
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
      
      expect(compiled.querySelector('[data-testid="register-container"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="register-card"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="register-title"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="register-form"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="username-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="email-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="password-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="confirm-password-input"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="register-button"]')).toBeTruthy();
      expect(compiled.querySelector('[data-testid="login-link"]')).toBeTruthy();
    });
  });
});
