import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';

/**
 * Reusable loading state component
 */
@Component({
  selector: 'app-loading-state',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, MatProgressBarModule],
  template: `
    <div class="loading-container" [class.fullscreen]="fullscreen">
      <mat-progress-spinner 
        *ngIf="mode === 'spinner'"
        mode="indeterminate" 
        [diameter]="size"
        [class]="sizeClass">
      </mat-progress-spinner>
      <mat-progress-bar 
        *ngIf="mode === 'bar'"
        mode="indeterminate">
      </mat-progress-bar>
      <p *ngIf="message" class="loading-message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 16px;
      padding: 24px;
    }

    .loading-container.fullscreen {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.9);
      z-index: 1000;
    }

    .loading-message {
      color: #666;
      font-size: 14px;
      margin: 0;
    }

    ::ng-deep .mat-mdc-progress-spinner {
      --mdc-circular-progress-active-indicator-color: #673ab7;
    }
  `]
})
export class LoadingStateComponent {
  @Input() mode: 'spinner' | 'bar' = 'spinner';
  @Input() size: number = 50;
  @Input() fullscreen = false;
  @Input() message?: string;

  get sizeClass(): string {
    if (this.size <= 30) return 'size-small';
    if (this.size <= 50) return 'size-medium';
    return 'size-large';
  }
}

