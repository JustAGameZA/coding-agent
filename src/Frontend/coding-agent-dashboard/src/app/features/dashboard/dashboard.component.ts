import { Component, signal, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardStats } from '../../core/models/dashboard.models';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { formatDuration as formatDurationUtil } from '../../shared/utils/time.utils';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, RouterModule, MatButtonModule],
  template: `
    <div class="dashboard-container" [attr.data-testid]="'dashboard-root'">
      <h1 [attr.data-testid]="'dashboard-title'">
        <mat-icon>dashboard</mat-icon>
        Coding Agent Dashboard
      </h1>

      <div class="dashboard-actions">
        <button mat-stroked-button color="primary" routerLink="/chat" [attr.data-testid]="'go-to-chat'">
          <mat-icon>chat</mat-icon>
          <span>Go to Chat</span>
        </button>
      </div>
      
      <div class="loading-overlay" *ngIf="loading()">
        <mat-progress-spinner mode="indeterminate" diameter="60"></mat-progress-spinner>
      </div>

      <div class="error-message" *ngIf="error()">
        <mat-icon>error</mat-icon>
        <span>{{ error() }}</span>
      </div>

      <div class="dashboard-stats" *ngIf="stats() && !loading()">
        <!-- Conversations & Messages -->
        <mat-card class="stat-card" [attr.data-testid]="'stat-card-conversations'">
          <mat-card-header>
            <mat-icon class="stat-icon chat-icon">chat</mat-icon>
            <mat-card-title>Conversations</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ stats()?.totalConversations || 0 }}</div>
            <div class="stat-label">Total Messages: {{ stats()?.totalMessages || 0 }}</div>
          </mat-card-content>
        </mat-card>

        <!-- Total Tasks -->
        <mat-card class="stat-card" [attr.data-testid]="'stat-card-total'">
          <mat-card-header>
            <mat-icon class="stat-icon task-icon">assignment</mat-icon>
            <mat-card-title>Total Tasks</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ stats()?.totalTasks || 0 }}</div>
            <div class="stat-label">All time tasks</div>
          </mat-card-content>
        </mat-card>

        <!-- Completed Tasks -->
        <mat-card class="stat-card success" [attr.data-testid]="'stat-card-completed'">
          <mat-card-header>
            <mat-icon class="stat-icon success-icon">check_circle</mat-icon>
            <mat-card-title>Completed</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ stats()?.completedTasks || 0 }}</div>
            <div class="stat-label">
              {{ completionRate() }}% completion rate
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Running Tasks -->
        <mat-card class="stat-card running" [attr.data-testid]="'stat-card-active'">
          <mat-card-header>
            <mat-icon class="stat-icon running-icon">sync</mat-icon>
            <mat-card-title>Running</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ stats()?.runningTasks || 0 }}</div>
            <div class="stat-label">Currently executing</div>
          </mat-card-content>
        </mat-card>

        <!-- Failed Tasks -->
        <mat-card class="stat-card failed" [attr.data-testid]="'stat-card-failed'">
          <mat-card-header>
            <mat-icon class="stat-icon failed-icon">error</mat-icon>
            <mat-card-title>Failed</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ stats()?.failedTasks || 0 }}</div>
            <div class="stat-label">Need attention</div>
          </mat-card-content>
        </mat-card>

        <!-- Average Duration -->
        <mat-card class="stat-card" [attr.data-testid]="'stat-card-duration'">
          <mat-card-header>
            <mat-icon class="stat-icon duration-icon">timer</mat-icon>
            <mat-card-title>Avg Duration</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ formatDuration(stats()?.averageTaskDuration || 0) }}</div>
            <div class="stat-label">Per task average</div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="last-updated" *ngIf="stats()" [attr.data-testid]="'last-updated'">
        <mat-icon>update</mat-icon>
        Last updated: {{ formatTimestamp(stats()?.lastUpdated || '') }}
        <span class="auto-refresh">(Auto-refreshing every 30s)</span>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    h1 {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 32px;
      color: #333;
      font-size: 28px;
    }

    .loading-overlay {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 400px;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background-color: #ffebee;
      color: #c62828;
      border-radius: 4px;
      margin-bottom: 24px;
    }

    .dashboard-stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 24px;
      margin-bottom: 24px;
    }

    .dashboard-actions {
      display: flex;
      justify-content: flex-end;
      margin-bottom: 16px;
      gap: 12px;
    }

    .stat-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
    }

    .stat-card mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;
    }

    .stat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .chat-icon { color: #1976d2; }
    .task-icon { color: #7b1fa2; }
    .success-icon { color: #388e3c; }
    .running-icon { color: #f57c00; }
    .failed-icon { color: #d32f2f; }
    .duration-icon { color: #0097a7; }

    .stat-value {
      font-size: 48px;
      font-weight: 600;
      line-height: 1;
      margin-bottom: 8px;
    }

    .stat-label {
      font-size: 14px;
      color: #666;
    }

    .stat-card.success .stat-value { color: #388e3c; }
    .stat-card.running .stat-value { color: #f57c00; }
    .stat-card.failed .stat-value { color: #d32f2f; }

    .last-updated {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 14px;
      color: #666;
      padding: 16px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }

    .auto-refresh {
      margin-left: 8px;
      font-style: italic;
      color: #999;
    }
  `]
})
export class DashboardComponent {
  private dashboardService = inject(DashboardService);
  private notificationService = inject(NotificationService);
  private destroyRef = inject(DestroyRef);

  stats = signal<DashboardStats | null>(null);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);

  ngOnInit() {
    // Initial load
    this.loadStats();

    // Auto-refresh every 30 seconds
    interval(30000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadStats(true); // Silent refresh
      });
  }

  private loadStats(silent = false) {
    if (!silent) {
      this.loading.set(true);
    }
    this.error.set(null);

    this.dashboardService.getStats().subscribe({
      next: (data) => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        // Dashboard service not yet implemented - show default stats
        const errorMsg = err.status === 404 
          ? 'Dashboard service not available yet' 
          : 'Failed to load dashboard statistics';
        
        // For 404, set default empty stats instead of error
        if (err.status === 404) {
          this.stats.set({
            totalTasks: 0,
            completedTasks: 0,
            runningTasks: 0,
            failedTasks: 0,
            totalConversations: 0,
            totalMessages: 0,
            averageTaskDuration: 0,
            lastUpdated: new Date().toISOString()
          });
          this.loading.set(false);
          // Don't show error notification for 404 - service not implemented yet
          console.info('Dashboard Service not available (404) - showing default stats');
        } else {
          this.error.set(errorMsg);
          this.loading.set(false);
          if (!silent) {
            this.notificationService.error(errorMsg);
          }
          console.error('Dashboard stats error:', err);
        }
      }
    });
  }

  completionRate(): number {
    const s = this.stats();
    if (!s || s.totalTasks === 0) return 0;
    return Math.round((s.completedTasks / s.totalTasks) * 100);
  }

  // Expose shared util for template binding
  formatDuration = formatDurationUtil;

  formatTimestamp(timestamp: string): string {
    if (!timestamp) return 'N/A';
    try {
      const date = new Date(timestamp);
      return date.toLocaleString();
    } catch {
      return timestamp;
    }
  }
}
