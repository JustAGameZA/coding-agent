import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReflectionPanelComponent } from './components/reflection-panel.component';
import { PlanningProgressComponent } from './components/planning-progress.component';
import { FeedbackSubmitComponent } from './components/feedback-submit.component';
import { MemoryContextComponent } from './components/memory-context.component';
import { ThinkingProcessComponent } from './components/thinking-process.component';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    ReflectionPanelComponent,
    PlanningProgressComponent,
    FeedbackSubmitComponent,
    MemoryContextComponent,
    ThinkingProcessComponent
  ],
  template: `
    <div class="task-detail" *ngIf="taskId()">
      <mat-card class="task-header">
        <mat-card-header>
          <mat-icon class="header-icon">assignment</mat-icon>
          <mat-card-title>{{ task()?.title || 'Task Details' }}</mat-card-title>
          <mat-chip [class]="getStatusClass()" class="status-chip">
            {{ task()?.status }}
          </mat-chip>
        </mat-card-header>
        <mat-card-content>
          <div class="task-meta">
            <mat-chip>
              <mat-icon>category</mat-icon>
              {{ task()?.type }}
            </mat-chip>
            <mat-chip>
              <mat-icon>signal_cellular_alt</mat-icon>
              {{ task()?.complexity }}
            </mat-chip>
            <mat-chip *ngIf="task()?.duration">
              <mat-icon>timer</mat-icon>
              {{ formatDuration(task()?.duration || 0) }}
            </mat-chip>
          </div>
          <p class="task-description">{{ task()?.description }}</p>
        </mat-card-content>
      </mat-card>

      <mat-tab-group class="agentic-tabs">
        <!-- Overview Tab -->
        <mat-tab label="Overview">
          <div class="tab-content">
            <mat-card *ngIf="task()?.executionId" class="execution-info">
              <mat-card-header>
                <mat-icon>play_circle</mat-icon>
                <mat-card-title>Execution Information</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <p>Execution ID: {{ task()?.executionId }}</p>
                <button 
                  mat-stroked-button 
                  (click)="loadReflection()"
                  *ngIf="task()?.executionId">
                  <mat-icon>psychology</mat-icon>
                  View Reflection
                </button>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <!-- Planning Tab -->
        <mat-tab label="Planning">
          <div class="tab-content">
            <app-planning-progress [taskId]="taskId()!" *ngIf="taskId()"></app-planning-progress>
          </div>
        </mat-tab>

        <!-- Reflection Tab -->
        <mat-tab label="Reflection">
          <div class="tab-content">
            <app-reflection-panel 
              [executionId]="task()?.executionId || ''" 
              *ngIf="task()?.executionId">
            </app-reflection-panel>
          </div>
        </mat-tab>

        <!-- Memory Tab -->
        <mat-tab label="Memory Context">
          <div class="tab-content">
            <app-memory-context 
              [query]="task()?.description || ''" 
              *ngIf="task()?.description">
            </app-memory-context>
          </div>
        </mat-tab>

        <!-- Feedback Tab -->
        <mat-tab label="Feedback">
          <div class="tab-content">
            <app-feedback-submit 
              [taskId]="taskId()!"
              [executionId]="task()?.executionId"
              (feedbackSubmitted)="onFeedbackSubmitted()"
              *ngIf="taskId()">
            </app-feedback-submit>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>

    <div class="loading-overlay" *ngIf="loading()">
      <mat-progress-spinner mode="indeterminate" diameter="60"></mat-progress-spinner>
    </div>
  `,
  styles: [`
    .task-detail {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    .task-header {
      margin-bottom: 24px;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .header-icon {
      color: #673ab7;
    }

    .status-chip {
      margin-left: auto;
      font-weight: 500;
    }

    .status-chip.status-completed {
      background-color: #e8f5e9;
      color: #388e3c;
    }

    .status-chip.status-running {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .status-chip.status-failed {
      background-color: #ffebee;
      color: #c62828;
    }

    .task-meta {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      margin-bottom: 16px;
    }

    .task-meta mat-chip {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .task-description {
      color: #666;
      line-height: 1.5;
    }

    .agentic-tabs {
      margin-top: 24px;
    }

    .tab-content {
      padding: 24px 0;
    }

    .execution-info {
      margin-bottom: 24px;
    }

    .loading-overlay {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 400px;
    }
  `]
})
export class TaskDetailComponent {
  private taskId = signal<string | null>(null);
  task = signal<any>(null); // TODO: Use proper Task type
  loading = signal<boolean>(false);

  @Input()
  set id(value: string) {
    this.taskId.set(value);
    if (value) {
      this.loadTask();
    }
  }

  private loadTask() {
    // TODO: Load task from service
    this.loading.set(false);
  }

  loadReflection() {
    // Trigger reflection loading
  }

  onFeedbackSubmitted() {
    // Refresh feedback or show message
  }

  getStatusClass(): string {
    const status = this.task()?.status?.toLowerCase() || '';
    return `status-${status}`;
  }

  formatDuration(ms: number): string {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  }
}

