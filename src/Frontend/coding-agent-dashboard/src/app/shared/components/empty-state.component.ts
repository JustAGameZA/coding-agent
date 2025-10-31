import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Reusable empty state component
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  template: `
    <div class="empty-state">
      <mat-icon class="empty-icon">{{ icon }}</mat-icon>
      <h3 *ngIf="title" class="empty-title">{{ title }}</h3>
      <p *ngIf="message" class="empty-message">{{ message }}</p>
      <button 
        *ngIf="actionLabel && action"
        mat-raised-button 
        color="primary"
        (click)="action.emit()">
        {{ actionLabel }}
      </button>
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 24px;
      text-align: center;
      color: #666;
    }

    .empty-icon {
      font-size: 96px;
      width: 96px;
      height: 96px;
      margin-bottom: 24px;
      color: #bdbdbd;
    }

    .empty-title {
      font-size: 24px;
      font-weight: 500;
      color: #333;
      margin: 0 0 8px 0;
    }

    .empty-message {
      font-size: 16px;
      color: #666;
      margin: 0 0 24px 0;
      max-width: 400px;
    }

    button {
      margin-top: 8px;
    }
  `]
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input() title?: string;
  @Input() message = 'No items found';
  @Input() actionLabel?: string;
  @Input() action?: { emit: () => void };
}

