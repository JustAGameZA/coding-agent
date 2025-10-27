import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notifications/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatIconModule
  ],
  template: `
    <div class="login-container" [attr.data-testid]="'login-container'">
      <mat-card class="login-card" [attr.data-testid]="'login-card'">
        <mat-card-header>
          <mat-card-title [attr.data-testid]="'login-title'">
            <mat-icon class="login-icon">lock</mat-icon>
            Sign In
          </mat-card-title>
          <mat-card-subtitle>Welcome to Coding Agent Dashboard</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" [attr.data-testid]="'login-form'">
            <!-- Username/Email Field -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username or Email</mat-label>
              <mat-icon matPrefix>person</mat-icon>
              <input 
                matInput 
                type="text"
                formControlName="username"
                placeholder="Enter your username or email"
                [attr.data-testid]="'username-input'"
                autocomplete="username"
              />
              <mat-error *ngIf="loginForm.get('username')?.hasError('required')">
                Username is required
              </mat-error>
              <mat-error *ngIf="loginForm.get('username')?.hasError('email') && loginForm.get('username')?.value?.includes('@')">
                Please enter a valid email address
              </mat-error>
            </mat-form-field>

            <!-- Password Field -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Password</mat-label>
              <mat-icon matPrefix>lock</mat-icon>
              <input 
                matInput 
                [type]="hidePassword() ? 'password' : 'text'"
                formControlName="password"
                placeholder="Enter your password"
                [attr.data-testid]="'password-input'"
                autocomplete="current-password"
              />
              <button 
                mat-icon-button 
                matSuffix 
                type="button"
                (click)="togglePasswordVisibility()"
                [attr.aria-label]="'Toggle password visibility'"
                [attr.data-testid]="'toggle-password-visibility'"
              >
                <mat-icon>{{ hidePassword() ? 'visibility' : 'visibility_off' }}</mat-icon>
              </button>
              <mat-error *ngIf="loginForm.get('password')?.hasError('required')">
                Password is required
              </mat-error>
            </mat-form-field>

            <!-- Remember Me Checkbox -->
            <div class="remember-me-container">
              <mat-checkbox 
                formControlName="rememberMe"
                [attr.data-testid]="'remember-me-checkbox'"
              >
                Remember me
              </mat-checkbox>
            </div>

            <!-- Error Message -->
            <div 
              class="error-message" 
              *ngIf="errorMessage()"
              [attr.data-testid]="'error-message'"
            >
              <mat-icon>error</mat-icon>
              <span>{{ errorMessage() }}</span>
            </div>

            <!-- Submit Button -->
            <button 
              mat-raised-button 
              color="primary" 
              type="submit"
              class="full-width submit-button"
              [disabled]="loginForm.invalid || loading()"
              [attr.data-testid]="'login-button'"
            >
              <mat-spinner 
                *ngIf="loading()" 
                diameter="20" 
                class="button-spinner"
              ></mat-spinner>
              <span *ngIf="!loading()">Sign In</span>
              <span *ngIf="loading()">Signing in...</span>
            </button>
          </form>
        </mat-card-content>

        <mat-card-actions class="login-actions">
          <a 
            href="javascript:void(0)" 
            class="forgot-password-link"
            [attr.data-testid]="'forgot-password-link'"
          >
            Forgot password?
          </a>
          <span class="spacer"></span>
          <span class="register-prompt">
            Don't have an account?
            <a 
              routerLink="/register" 
              class="register-link"
              [attr.data-testid]="'register-link'"
            >
              Create one
            </a>
          </span>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
    }

    .login-card {
      width: 100%;
      max-width: 450px;
      padding: 20px;
    }

    mat-card-header {
      display: flex;
      flex-direction: column;
      align-items: center;
      margin-bottom: 30px;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 28px;
      font-weight: 500;
      margin-bottom: 10px;
    }

    .login-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    mat-card-subtitle {
      text-align: center;
      color: rgba(0, 0, 0, 0.6);
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    .remember-me-container {
      margin-bottom: 20px;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      margin-bottom: 16px;
      background-color: #ffebee;
      border-radius: 4px;
      color: #c62828;
    }

    .error-message mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .submit-button {
      height: 48px;
      font-size: 16px;
      margin-bottom: 16px;
      position: relative;
    }

    .button-spinner {
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
    }

    .login-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0 16px 16px;
      font-size: 14px;
    }

    .forgot-password-link {
      color: #667eea;
      text-decoration: none;
    }

    .forgot-password-link:hover {
      text-decoration: underline;
    }

    .register-prompt {
      color: rgba(0, 0, 0, 0.6);
    }

    .register-link {
      color: #667eea;
      text-decoration: none;
      font-weight: 500;
      margin-left: 4px;
    }

    .register-link:hover {
      text-decoration: underline;
    }

    .spacer {
      flex: 1;
    }

    @media (max-width: 600px) {
      .login-container {
        padding: 10px;
      }

      .login-card {
        padding: 15px;
      }

      .login-actions {
        flex-direction: column;
        gap: 12px;
        align-items: flex-start;
      }

      .spacer {
        display: none;
      }
    }
  `]
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notificationService = inject(NotificationService);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly hidePassword = signal(true);

  protected loginForm!: FormGroup;

  ngOnInit(): void {
    this.initializeForm();
    
    // If already authenticated, redirect to dashboard
    if (this.authService.isAuthenticated()) {
      this.redirectAfterLogin();
    }
  }

  private initializeForm(): void {
    this.loginForm = this.fb.group({
      username: ['', [Validators.required]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });

    // Add email validation if username contains @
    this.loginForm.get('username')?.valueChanges.subscribe(value => {
      const usernameControl = this.loginForm.get('username');
      if (value && value.includes('@')) {
        usernameControl?.setValidators([Validators.required, Validators.email]);
      } else {
        usernameControl?.setValidators([Validators.required]);
      }
      usernameControl?.updateValueAndValidity({ emitEvent: false });
    });
  }

  protected togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  protected onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const { username, password, rememberMe } = this.loginForm.value;

    this.authService.login(username, password, rememberMe).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.notificationService.info(`Welcome back, ${response.user.username}!`);
        this.redirectAfterLogin();
      },
      error: (error) => {
        this.loading.set(false);
        const message = error.message || 'Login failed. Please try again.';
        this.errorMessage.set(message);
        this.notificationService.error(message);
        
        // Clear password field on error
        this.loginForm.patchValue({ password: '' });
      }
    });
  }

  private redirectAfterLogin(): void {
    // Check for returnUrl from query params or session storage
    const returnUrl = 
      this.route.snapshot.queryParamMap.get('returnUrl') ||
      this.getStoredReturnUrl() ||
      '/dashboard';

    this.clearStoredReturnUrl();
    this.router.navigate([returnUrl]);
  }

  private getStoredReturnUrl(): string | null {
    try {
      return sessionStorage.getItem('returnUrl');
    } catch {
      return null;
    }
  }

  private clearStoredReturnUrl(): void {
    try {
      sessionStorage.removeItem('returnUrl');
    } catch {
      // Ignore errors
    }
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
}
