import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Reusable error state component
 */
@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <div class="error-container">
      <mat-icon class="error-icon">error_outline</mat-icon>
      <h3 class="error-title">{{ title || 'Something went wrong' }}</h3>
      <p class="error-message">{{ message || error }}</p>
      <button 
        *ngIf="showRetry"
        mat-raised-button 
        color="primary"
        (click)="onRetry.emit()">
        <mat-icon>refresh</mat-icon>
        Try Again
      </button>
      <button 
        *ngIf="showBack"
        mat-stroked-button
        (click)="onBack.emit()">
        <mat-icon>arrow_back</mat-icon>
        Go Back
      </button>
    </div>
  `,
  styles: [`
    .error-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      text-align: center;
      color: #666;
    }

    .error-icon {
      font-size: 96px;
      width: 96px;
      height: 96px;
      margin-bottom: 24px;
      color: #c62828;
    }

    .error-title {
      font-size: 24px;
      font-weight: 500;
      color: #333;
      margin: 0 0 8px 0;
    }

    .error-message {
      font-size: 16px;
      color: #666;
      margin: 0 0 24px 0;
      max-width: 500px;
    }

    button {
      margin: 0 8px;
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }
  `]
})
export class ErrorStateComponent {
  @Input() title?: string;
  @Input() message?: string;
  @Input() error?: string;
  @Input() showRetry = true;
  @Input() showBack = false;
  @Output() onRetry = new EventEmitter<void>();
  @Output() onBack = new EventEmitter<void>();
}

