import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { AgenticAiService } from '../../core/services/agentic-ai.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { AgenticBadgeComponent } from '../../shared/components/agentic-badge.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';

@Component({
  selector: 'app-agentic-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatProgressBarModule,
    MatTabsModule,
    AgenticBadgeComponent,
    LoadingStateComponent
  ],
  template: `
    <div class="agentic-dashboard">
      <!-- Header -->
      <div class="dashboard-header">
        <div class="header-content">
          <div class="header-title">
            <mat-icon class="title-icon">psychology</mat-icon>
            <h1>Agentic AI Dashboard</h1>
            <app-agentic-badge label="Active"></app-agentic-badge>
          </div>
          <p class="header-subtitle">
            Monitor and interact with AI self-learning capabilities
          </p>
        </div>
      </div>

      <!-- Overview Cards -->
      <div class="overview-grid">
        <mat-card class="overview-card memory-card">
          <mat-card-header>
            <mat-icon class="card-icon">memory</mat-icon>
            <mat-card-title>Memory System</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ memoryStats().episodic }}</div>
            <div class="stat-label">Episodic Memories</div>
            <div class="stat-value">{{ memoryStats().semantic }}</div>
            <div class="stat-label">Semantic Memories</div>
            <div class="stat-value">{{ memoryStats().procedures }}</div>
            <div class="stat-label">Learned Procedures</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="overview-card reflection-card">
          <mat-card-header>
            <mat-icon class="card-icon">auto_fix_high</mat-icon>
            <mat-card-title>Reflection</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ reflectionStats().total }}</div>
            <div class="stat-label">Total Reflections</div>
            <div class="stat-value">{{ reflectionStats().avgConfidence | number:'1.0-0' }}%</div>
            <div class="stat-label">Avg Confidence</div>
            <mat-progress-bar 
              mode="determinate" 
              [value]="reflectionStats().avgConfidence"
              class="confidence-bar">
            </mat-progress-bar>
          </mat-card-content>
        </mat-card>

        <mat-card class="overview-card planning-card">
          <mat-card-header>
            <mat-icon class="card-icon">route</mat-icon>
            <mat-card-title>Planning</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ planningStats().active }}</div>
            <div class="stat-label">Active Plans</div>
            <div class="stat-value">{{ planningStats().completed }}</div>
            <div class="stat-label">Completed Plans</div>
            <div class="stat-value">{{ planningStats().successRate | number:'1.0-0' }}%</div>
            <div class="stat-label">Success Rate</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="overview-card learning-card">
          <mat-card-header>
            <mat-icon class="card-icon">trending_up</mat-icon>
            <mat-card-title>Learning</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="stat-value">{{ learningStats().feedback }}</div>
            <div class="stat-label">Feedback Received</div>
            <div class="stat-value">{{ learningStats().improvements }}</div>
            <div class="stat-label">Improvements Made</div>
            <div class="stat-value">{{ learningStats().modelUpdates }}</div>
            <div class="stat-label">Model Updates</div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Recent Activity -->
      <mat-card class="activity-card">
        <mat-card-header>
          <mat-icon class="header-icon">history</mat-icon>
          <mat-card-title>Recent Agentic AI Activity</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <div class="activity-list">
            <div *ngFor="let activity of recentActivity()" class="activity-item">
              <mat-icon class="activity-icon" [class]="getActivityIconClass(activity.type)">
                {{ getActivityIcon(activity.type) }}
              </mat-icon>
              <div class="activity-content">
                <div class="activity-title">{{ activity.title }}</div>
                <div class="activity-time">{{ formatTime(activity.timestamp) }}</div>
              </div>
              <mat-chip class="activity-status" [class]="'status-' + activity.status.toLowerCase()">
                {{ activity.status }}
              </mat-chip>
            </div>
          </div>
          <div *ngIf="recentActivity().length === 0" class="empty-activity">
            <mat-icon>inbox</mat-icon>
            <p>No recent activity</p>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Quick Actions -->
      <div class="actions-section">
        <h2>Quick Actions</h2>
        <div class="actions-grid">
          <button mat-raised-button color="primary" routerLink="/tasks">
            <mat-icon>assignment</mat-icon>
            View All Tasks
          </button>
          <button mat-stroked-button routerLink="/chat">
            <mat-icon>chat</mat-icon>
            Start Chat Session
          </button>
          <button mat-stroked-button (click)="refreshData()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon>
            Refresh Data
          </button>
        </div>
      </div>
    </div>

    <app-loading-state *ngIf="loading()" mode="spinner" [size]="50"></app-loading-state>
  `,
  styles: [`
    .agentic-dashboard {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    .dashboard-header {
      margin-bottom: 32px;
    }

    .header-content {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .header-title {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .title-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      color: #673ab7;
    }

    .header-title h1 {
      margin: 0;
      font-size: 32px;
      font-weight: 600;
      color: #333;
    }

    .header-subtitle {
      color: #666;
      font-size: 16px;
      margin: 0;
    }

    .overview-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 24px;
      margin-bottom: 32px;
    }

    .overview-card {
      transition: transform 0.2s ease, box-shadow 0.2s ease;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
      }
    }

    .card-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .memory-card .card-icon { color: #2196f3; }
    .reflection-card .card-icon { color: #ff9800; }
    .planning-card .card-icon { color: #4caf50; }
    .learning-card .card-icon { color: #009688; }

    .stat-value {
      font-size: 32px;
      font-weight: 600;
      line-height: 1;
      margin-bottom: 4px;
      color: #333;
    }

    .stat-label {
      font-size: 14px;
      color: #666;
      margin-bottom: 16px;
    }

    .confidence-bar {
      margin-top: 16px;
      height: 8px;
      border-radius: 4px;
    }

    .activity-card {
      margin-bottom: 32px;
    }

    .activity-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .activity-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 12px;
      border-radius: 8px;
      background-color: #fafafa;
      transition: background-color 0.2s ease;

      &:hover {
        background-color: #f0f0f0;
      }
    }

    .activity-icon {
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .activity-icon.icon-memory { color: #2196f3; }
    .activity-icon.icon-reflection { color: #ff9800; }
    .activity-icon.icon-planning { color: #4caf50; }
    .activity-icon.icon-learning { color: #009688; }

    .activity-content {
      flex: 1;
    }

    .activity-title {
      font-weight: 500;
      color: #333;
      margin-bottom: 4px;
    }

    .activity-time {
      font-size: 12px;
      color: #666;
    }

    .activity-status {
      font-size: 12px;
    }

    .empty-activity {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      color: #999;
    }

    .actions-section {
      margin-top: 32px;
    }

    .actions-section h2 {
      margin-bottom: 16px;
      color: #333;
    }

    .actions-grid {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }

    .actions-grid button {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    @media (max-width: 768px) {
      .overview-grid {
        grid-template-columns: 1fr;
      }

      .actions-grid {
        flex-direction: column;
      }

      .actions-grid button {
        width: 100%;
      }
    }
  `]
})
export class AgenticDashboardComponent {
  private agenticAiService = inject(AgenticAiService);
  private dashboardService = inject(DashboardService);

  loading = signal<boolean>(false);
  memoryStats = signal({ episodic: 0, semantic: 0, procedures: 0 });
  reflectionStats = signal({ total: 0, avgConfidence: 0 });
  planningStats = signal({ active: 0, completed: 0, successRate: 0 });
  learningStats = signal({ feedback: 0, improvements: 0, modelUpdates: 0 });
  recentActivity = signal<any[]>([]);

  ngOnInit() {
    this.loadData();
  }

  private loadData() {
    this.loading.set(true);
    // TODO: Load actual data from services
    // For now, use mock data
    this.memoryStats.set({ episodic: 145, semantic: 328, procedures: 23 });
    this.reflectionStats.set({ total: 89, avgConfidence: 75 });
    this.planningStats.set({ active: 5, completed: 42, successRate: 87 });
    this.learningStats.set({ feedback: 156, improvements: 34, modelUpdates: 12 });
    
    this.recentActivity.set([
      { type: 'reflection', title: 'Task execution reflected', status: 'Completed', timestamp: new Date() },
      { type: 'planning', title: 'Complex task plan created', status: 'InProgress', timestamp: new Date(Date.now() - 3600000) },
      { type: 'memory', title: 'Semantic memory stored', status: 'Completed', timestamp: new Date(Date.now() - 7200000) },
      { type: 'learning', title: 'Model updated from feedback', status: 'Completed', timestamp: new Date(Date.now() - 10800000) }
    ]);

    setTimeout(() => this.loading.set(false), 500);
  }

  refreshData() {
    this.loadData();
  }

  getActivityIcon(type: string): string {
    switch (type) {
      case 'memory': return 'memory';
      case 'reflection': return 'auto_fix_high';
      case 'planning': return 'route';
      case 'learning': return 'trending_up';
      default: return 'event';
    }
  }

  getActivityIconClass(type: string): string {
    return `icon-${type}`;
  }

  formatTime(timestamp: Date): string {
    const now = new Date();
    const diff = now.getTime() - timestamp.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  }
}

