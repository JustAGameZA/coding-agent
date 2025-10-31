import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

/**
 * Badge component to indicate agentic AI features are active
 */
@Component({
  selector: 'app-agentic-badge',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatIconModule, MatTooltipModule],
  template: `
    <mat-chip 
      class="agentic-badge"
      [matTooltip]="tooltip || 'Agentic AI features enabled'"
      matTooltipPosition="above">
      <mat-icon class="badge-icon">psychology</mat-icon>
      <span class="badge-text">{{ label || 'Agentic AI' }}</span>
    </mat-chip>
  `,
  styles: [`
    .agentic-badge {
      background: linear-gradient(135deg, #673ab7 0%, #512da8 100%);
      color: white;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      padding: 4px 10px;
      min-height: 24px;
      display: inline-flex;
      align-items: center;
      gap: 6px;
      box-shadow: 0 2px 4px rgba(103, 58, 183, 0.3);
      transition: transform 0.2s ease, box-shadow 0.2s ease;

      &:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 8px rgba(103, 58, 183, 0.4);
      }
    }

    .badge-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      animation: pulse 2s infinite;
    }

    .badge-text {
      font-size: 11px;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.7;
      }
    }
  `]
})
export class AgenticBadgeComponent {
  @Input() label?: string;
  @Input() tooltip?: string;
}

