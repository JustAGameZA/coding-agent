import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';

/**
 * Reusable status chip component with consistent styling
 */
@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatIconModule],
  template: `
    <mat-chip 
      [class]="getChipClass()"
      [class.agentic-enabled]="agenticEnabled"
      class="status-chip">
      <mat-icon *ngIf="showIcon" class="chip-icon">{{ getIcon() }}</mat-icon>
      <span>{{ label || status }}</span>
      <span *ngIf="badge" class="chip-badge">{{ badge }}</span>
    </mat-chip>
  `,
  styles: [`
    .status-chip {
      font-size: 12px;
      font-weight: 500;
      min-height: 24px;
      padding: 4px 12px;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }

    .chip-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .chip-badge {
      background: rgba(255, 255, 255, 0.3);
      padding: 2px 6px;
      border-radius: 12px;
      font-size: 10px;
      font-weight: 600;
    }

    .status-chip.status-success {
      background-color: #e8f5e9;
      color: #388e3c;
    }

    .status-chip.status-error,
    .status-chip.status-failed {
      background-color: #ffebee;
      color: #c62828;
    }

    .status-chip.status-warning,
    .status-chip.status-running,
    .status-chip.status-pending {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .status-chip.status-info {
      background-color: #e3f2fd;
      color: #1976d2;
    }

    .status-chip.agentic-enabled {
      border: 2px solid #673ab7;
      position: relative;
    }

    .status-chip.agentic-enabled::after {
      content: '';
      position: absolute;
      top: -2px;
      right: -2px;
      width: 8px;
      height: 8px;
      background: #673ab7;
      border-radius: 50%;
      border: 2px solid white;
    }
  `]
})
export class StatusChipComponent {
  @Input() status: string = '';
  @Input() label?: string;
  @Input() showIcon = true;
  @Input() badge?: string;
  @Input() agenticEnabled = false; // Indicates agentic AI features are active

  getChipClass(): string {
    const statusLower = this.status.toLowerCase();
    if (statusLower.includes('success') || statusLower.includes('completed')) {
      return 'status-success';
    }
    if (statusLower.includes('error') || statusLower.includes('failed')) {
      return 'status-error';
    }
    if (statusLower.includes('warning') || statusLower.includes('running') || statusLower.includes('pending')) {
      return 'status-warning';
    }
    return 'status-info';
  }

  getIcon(): string {
    const statusLower = this.status.toLowerCase();
    if (statusLower.includes('success') || statusLower.includes('completed')) {
      return 'check_circle';
    }
    if (statusLower.includes('error') || statusLower.includes('failed')) {
      return 'error';
    }
    if (statusLower.includes('running') || statusLower.includes('pending')) {
      return statusLower.includes('running') ? 'sync' : 'radio_button_unchecked';
    }
    return 'info';
  }
}

