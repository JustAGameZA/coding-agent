import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notifications/notification.service';

@Component({
  selector: 'app-password-change',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="password-change-container">
      <mat-card class="password-change-card">
        <mat-card-header>
          <mat-icon class="header-icon">lock</mat-icon>
          <mat-card-title>Change Password</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="passwordForm" (ngSubmit)="onSubmit()">
            <!-- Current Password -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Current Password</mat-label>
              <mat-icon matPrefix>lock</mat-icon>
              <input 
                matInput 
                [type]="hideCurrentPassword() ? 'password' : 'text'"
                formControlName="currentPassword"
                placeholder="Enter your current password"
                [attr.data-testid]="'current-password-input'"
                autocomplete="current-password"
                required>
              <button 
                mat-icon-button 
                matSuffix 
                type="button"
                (click)="toggleCurrentPasswordVisibility()"
                [attr.aria-label]="'Toggle current password visibility'"
                [attr.data-testid]="'toggle-current-password-visibility'">
                <mat-icon>{{ hideCurrentPassword() ? 'visibility' : 'visibility_off' }}</mat-icon>
              </button>
              <mat-error *ngIf="passwordForm.get('currentPassword')?.hasError('required')">
                Current password is required
              </mat-error>
            </mat-form-field>

            <!-- New Password -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>New Password</mat-label>
              <mat-icon matPrefix>lock_outline</mat-icon>
              <input 
                matInput 
                [type]="hideNewPassword() ? 'password' : 'text'"
                formControlName="newPassword"
                placeholder="Enter new password"
                [attr.data-testid]="'new-password-input'"
                autocomplete="new-password"
                required>
              <button 
                mat-icon-button 
                matSuffix 
                type="button"
                (click)="toggleNewPasswordVisibility()"
                [attr.aria-label]="'Toggle new password visibility'"
                [attr.data-testid]="'toggle-new-password-visibility'">
                <mat-icon>{{ hideNewPassword() ? 'visibility' : 'visibility_off' }}</mat-icon>
              </button>
              <mat-hint *ngIf="!passwordForm.get('newPassword')?.hasError('required')">
                Min 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special character
              </mat-hint>
              <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('required')">
                New password is required
              </mat-error>
              <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('passwordStrength')">
                Password must meet strength requirements
              </mat-error>
            </mat-form-field>

            <!-- Confirm New Password -->
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Confirm New Password</mat-label>
              <mat-icon matPrefix>lock_outline</mat-icon>
              <input 
                matInput 
                [type]="hideConfirmPassword() ? 'password' : 'text'"
                formControlName="confirmPassword"
                placeholder="Re-enter new password"
                [attr.data-testid]="'confirm-password-input'"
                autocomplete="new-password"
                required>
              <button 
                mat-icon-button 
                matSuffix 
                type="button"
                (click)="toggleConfirmPasswordVisibility()"
                [attr.aria-label]="'Toggle confirm password visibility'"
                [attr.data-testid]="'toggle-confirm-password-visibility'">
                <mat-icon>{{ hideConfirmPassword() ? 'visibility' : 'visibility_off' }}</mat-icon>
              </button>
              <mat-error *ngIf="passwordForm.get('confirmPassword')?.hasError('required')">
                Please confirm your new password
              </mat-error>
              <mat-error *ngIf="passwordForm.hasError('passwordMismatch') && passwordForm.get('confirmPassword')?.touched">
                Passwords do not match
              </mat-error>
            </mat-form-field>

            <!-- Error Message -->
            <div 
              class="error-message" 
              *ngIf="errorMessage()"
              [attr.data-testid]="'error-message'">
              <mat-icon>error</mat-icon>
              <span>{{ errorMessage() }}</span>
            </div>

            <!-- Submit Button -->
            <button 
              mat-raised-button 
              color="primary" 
              type="submit"
              class="full-width submit-button"
              [disabled]="passwordForm.invalid || submitting()"
              [attr.data-testid]="'change-password-button'">
              <mat-spinner 
                *ngIf="submitting()" 
                diameter="20" 
                class="button-spinner">
              </mat-spinner>
              <span *ngIf="!submitting()">Change Password</span>
              <span *ngIf="submitting()">Changing password...</span>
            </button>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .password-change-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 64px);
      padding: 20px;
    }

    .password-change-card {
      width: 100%;
      max-width: 500px;
      padding: 24px;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 24px;
    }

    .header-icon {
      color: #673ab7;
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
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
      font-size: 14px;
    }

    .error-message mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .submit-button {
      height: 48px;
      font-size: 16px;
      margin-top: 8px;
      position: relative;
    }

    .button-spinner {
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
    }

    @media (max-width: 600px) {
      .password-change-container {
        padding: 10px;
      }

      .password-change-card {
        padding: 16px;
      }
    }
  `]
})
export class PasswordChangeComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  passwordForm: FormGroup;
  hideCurrentPassword = signal<boolean>(true);
  hideNewPassword = signal<boolean>(true);
  hideConfirmPassword = signal<boolean>(true);
  submitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  constructor() {
    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, this.passwordStrengthValidator]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.value;
    if (!password) return null;

    const hasMinLength = password.length >= 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

    if (!hasMinLength || !hasUpperCase || !hasLowerCase || !hasNumber || !hasSpecialChar) {
      return { passwordStrength: true };
    }

    return null;
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (!newPassword || !confirmPassword) return null;

    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  toggleCurrentPasswordVisibility(): void {
    this.hideCurrentPassword.update(value => !value);
  }

  toggleNewPasswordVisibility(): void {
    this.hideNewPassword.update(value => !value);
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.update(value => !value);
  }

  onSubmit(): void {
    if (this.passwordForm.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const currentPassword = this.passwordForm.value.currentPassword;
    const newPassword = this.passwordForm.value.newPassword;

    // TODO: Call auth service change password method when implemented
    // For now, simulate API call
    setTimeout(() => {
      // Mock success for now
      this.notificationService.success('Password changed successfully. You will be logged out.');
      this.passwordForm.reset();
      this.submitting.set(false);
      
      // Simulate logout after password change (security best practice)
      setTimeout(() => {
        this.authService.logout();
      }, 2000);
    }, 1000);
  }
}

