import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { RegisterRequest } from '../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <div class="register-container" [attr.data-testid]="'register-container'">
      <mat-card class="register-card" [attr.data-testid]="'register-card'">
        <mat-card-header>
          <mat-card-title [attr.data-testid]="'register-title'">
            <mat-icon class="register-icon">person_add</mat-icon>
            Create Account
          </mat-card-title>
          <mat-card-subtitle>Join the Coding Agent Dashboard</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" [attr.data-testid]="'register-form'">
            <!-- Username Field -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Username</mat-label>
              <mat-icon matPrefix>person</mat-icon>
              <input 
                matInput 
                type="text"
                formControlName="username"
                placeholder="Choose a username"
                [attr.data-testid]="'username-input'"
                autocomplete="username"
              />
              <mat-error *ngIf="registerForm.get('username')?.hasError('required')">
                Username is required
              </mat-error>
              <mat-error *ngIf="registerForm.get('username')?.hasError('minlength')">
                Username must be at least 3 characters
              </mat-error>
              <mat-error *ngIf="registerForm.get('username')?.hasError('pattern')">
                Username can only contain letters, numbers, and underscores
              </mat-error>
            </mat-form-field>

            <!-- Email Field -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email</mat-label>
              <mat-icon matPrefix>email</mat-icon>
              <input 
                matInput 
                type="email"
                formControlName="email"
                placeholder="Enter your email"
                [attr.data-testid]="'email-input'"
                autocomplete="email"
              />
              <mat-error *ngIf="registerForm.get('email')?.hasError('required')">
                Email is required
              </mat-error>
              <mat-error *ngIf="registerForm.get('email')?.hasError('email')">
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
                placeholder="Create a password"
                [attr.data-testid]="'password-input'"
                autocomplete="new-password"
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
              <mat-hint *ngIf="!registerForm.get('password')?.hasError('required')">
                Min 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special character
              </mat-hint>
              <mat-error *ngIf="registerForm.get('password')?.hasError('required')">
                Password is required
              </mat-error>
              <mat-error *ngIf="registerForm.get('password')?.hasError('passwordStrength')">
                Password must meet strength requirements
              </mat-error>
            </mat-form-field>

            <!-- Password Strength Indicator -->
            <div class="password-strength" *ngIf="registerForm.get('password')?.value">
              <div class="strength-bar">
                <div 
                  class="strength-fill" 
                  [ngClass]="passwordStrength()"
                  [style.width.%]="getPasswordStrengthPercentage()"
                ></div>
              </div>
              <span class="strength-label">{{ getPasswordStrengthLabel() }}</span>
            </div>

            <!-- Confirm Password Field -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Confirm Password</mat-label>
              <mat-icon matPrefix>lock</mat-icon>
              <input 
                matInput 
                [type]="hideConfirmPassword() ? 'password' : 'text'"
                formControlName="confirmPassword"
                placeholder="Re-enter your password"
                [attr.data-testid]="'confirm-password-input'"
                autocomplete="new-password"
              />
              <button 
                mat-icon-button 
                matSuffix 
                type="button"
                (click)="toggleConfirmPasswordVisibility()"
                [attr.aria-label]="'Toggle confirm password visibility'"
                [attr.data-testid]="'toggle-confirm-password-visibility'"
              >
                <mat-icon>{{ hideConfirmPassword() ? 'visibility' : 'visibility_off' }}</mat-icon>
              </button>
              <mat-error *ngIf="registerForm.get('confirmPassword')?.hasError('required')">
                Please confirm your password
              </mat-error>
              <mat-error *ngIf="registerForm.hasError('passwordMismatch') && registerForm.get('confirmPassword')?.touched">
                Passwords do not match
              </mat-error>
            </mat-form-field>

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
              [disabled]="registerForm.invalid || loading()"
              [attr.data-testid]="'register-button'"
            >
              <mat-spinner 
                *ngIf="loading()" 
                diameter="20" 
                class="button-spinner"
              ></mat-spinner>
              <span *ngIf="!loading()">Create Account</span>
              <span *ngIf="loading()">Creating account...</span>
            </button>
          </form>
        </mat-card-content>

        <mat-card-actions class="register-actions">
          <span class="login-prompt">
            Already have an account?
            <a 
              routerLink="/login" 
              class="login-link"
              [attr.data-testid]="'login-link'"
            >
              Sign in
            </a>
          </span>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .register-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 20px;
    }

    .register-card {
      width: 100%;
      max-width: 500px;
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

    .register-icon {
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

    .password-strength {
      margin-bottom: 20px;
    }

    .strength-bar {
      width: 100%;
      height: 6px;
      background-color: #e0e0e0;
      border-radius: 3px;
      overflow: hidden;
      margin-bottom: 4px;
    }

    .strength-fill {
      height: 100%;
      transition: width 0.3s ease, background-color 0.3s ease;
    }

    .strength-fill.weak {
      background-color: #f44336;
    }

    .strength-fill.fair {
      background-color: #ff9800;
    }

    .strength-fill.good {
      background-color: #2196f3;
    }

    .strength-fill.strong {
      background-color: #4caf50;
    }

    .strength-label {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.6);
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

    .register-actions {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 0 16px 16px;
      font-size: 14px;
    }

    .login-prompt {
      color: rgba(0, 0, 0, 0.6);
    }

    .login-link {
      color: #667eea;
      text-decoration: none;
      font-weight: 500;
      margin-left: 4px;
    }

    .login-link:hover {
      text-decoration: underline;
    }

    @media (max-width: 600px) {
      .register-container {
        padding: 10px;
      }

      .register-card {
        padding: 15px;
      }
    }
  `]
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notificationService = inject(NotificationService);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly hidePassword = signal(true);
  protected readonly hideConfirmPassword = signal(true);
  protected readonly passwordStrength = signal<'weak' | 'fair' | 'good' | 'strong'>('weak');

  protected registerForm!: FormGroup;

  ngOnInit(): void {
    this.initializeForm();
    
    // If already authenticated, redirect to dashboard
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  private initializeForm(): void {
    this.registerForm = this.fb.group({
      username: ['', [
        Validators.required,
        Validators.minLength(3),
        Validators.pattern(/^[a-zA-Z0-9_]+$/)
      ]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, this.passwordStrengthValidator]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });

    // Update password strength on password change
    this.registerForm.get('password')?.valueChanges.subscribe(password => {
      this.calculatePasswordStrength(password);
    });
  }

  protected togglePasswordVisibility(): void {
    this.hidePassword.update(value => !value);
  }

  protected toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(value => !value);
  }

  private passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    
    if (!password) {
      return null;
    }

    const hasMinLength = password.length >= 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /\d/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

    const isValid = hasMinLength && hasUpperCase && hasLowerCase && hasNumber && hasSpecialChar;

    return isValid ? null : { passwordStrength: true };
  }

  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  private calculatePasswordStrength(password: string): void {
    if (!password) {
      this.passwordStrength.set('weak');
      return;
    }

    let strength = 0;

    // Length check
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;

    // Character variety checks
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/\d/.test(password)) strength++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) strength++;

    // Map strength score to label
    if (strength <= 2) {
      this.passwordStrength.set('weak');
    } else if (strength <= 3) {
      this.passwordStrength.set('fair');
    } else if (strength <= 4) {
      this.passwordStrength.set('good');
    } else {
      this.passwordStrength.set('strong');
    }
  }

  protected getPasswordStrengthPercentage(): number {
    const strengths = { weak: 25, fair: 50, good: 75, strong: 100 };
    return strengths[this.passwordStrength()];
  }

  protected getPasswordStrengthLabel(): string {
    return `Password strength: ${this.passwordStrength()}`;
  }

  protected onSubmit(): void {
    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const { username, email, password } = this.registerForm.value;
    
    const request: RegisterRequest = {
      username,
      email,
      password
    };

    this.authService.register(request).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.notificationService.info(`Welcome, ${response.user.username}! Your account has been created.`);
        
        // Auto-redirect to dashboard (user is already logged in via AuthService)
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.loading.set(false);
        const message = error.message || 'Registration failed. Please try again.';
        this.errorMessage.set(message);
        this.notificationService.error(message);
      }
    });
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
