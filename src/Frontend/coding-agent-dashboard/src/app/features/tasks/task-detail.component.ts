import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { ReflectionPanelComponent } from './components/reflection-panel.component';
import { PlanningProgressComponent } from './components/planning-progress.component';
import { FeedbackSubmitComponent } from './components/feedback-submit.component';
import { MemoryContextComponent } from './components/memory-context.component';
import { ThinkingProcessComponent } from './components/thinking-process.component';
import { DashboardService } from '../../core/services/dashboard.service';
import { TaskService } from '../../core/services/task.service';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { AgenticBadgeComponent } from '../../shared/components/agentic-badge.component';
import { StatusChipComponent } from '../../shared/components/status-chip.component';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { TaskDetailDto, ExecutionStrategy } from '../../core/models/task.models';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatTabsModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatDialogModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
    MatMenuModule,
    ReflectionPanelComponent,
    PlanningProgressComponent,
    FeedbackSubmitComponent,
    MemoryContextComponent,
    ThinkingProcessComponent,
    AgenticBadgeComponent,
    StatusChipComponent,
    LoadingStateComponent
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
          
          <!-- Action Buttons -->
          <div class="task-actions" *ngIf="task()">
            <button 
              *ngIf="canExecute()"
              mat-raised-button 
              color="primary" 
              (click)="executeTask()"
              [disabled]="executing()"
              [attr.data-testid]="'execute-task-button'">
              <mat-icon>play_arrow</mat-icon>
              Execute Task
            </button>
            
            <button 
              *ngIf="canCancel()"
              mat-stroked-button 
              color="warn" 
              (click)="cancelTask()"
              [disabled]="executing()"
              [attr.data-testid]="'cancel-task-button'">
              <mat-icon>stop</mat-icon>
              Cancel
            </button>
            
            <button 
              *ngIf="canRetry()"
              mat-stroked-button 
              color="primary" 
              (click)="retryTask()"
              [disabled]="executing()"
              [attr.data-testid]="'retry-task-button'">
              <mat-icon>refresh</mat-icon>
              Retry
            </button>
            
            <button 
              *ngIf="canExecute()"
              mat-icon-button 
              [matMenuTriggerFor]="strategyMenu"
              [disabled]="executing()"
              matTooltip="Execution Strategy"
              [attr.data-testid]="'strategy-menu-button'">
              <mat-icon>settings</mat-icon>
            </button>
            
            <mat-menu #strategyMenu="matMenu">
              <button mat-menu-item (click)="executeTaskWithStrategy('SingleShot')">
                <mat-icon>flash_on</mat-icon>
                <span>Single Shot (Fast)</span>
              </button>
              <button mat-menu-item (click)="executeTaskWithStrategy('Iterative')">
                <mat-icon>repeat</mat-icon>
                <span>Iterative (Recommended)</span>
              </button>
              <button mat-menu-item (click)="executeTaskWithStrategy('MultiAgent')">
                <mat-icon>group_work</mat-icon>
                <span>Multi-Agent (Complex)</span>
              </button>
              <button mat-menu-item (click)="executeTaskWithStrategy('HybridExecution')">
                <mat-icon>all_inclusive</mat-icon>
                <span>Hybrid (Advanced)</span>
              </button>
            </mat-menu>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-tab-group class="agentic-tabs">
        <!-- Overview Tab -->
        <mat-tab label="Overview">
          <div class="tab-content">
            <mat-card *ngIf="getCurrentExecutionId()" class="execution-info">
              <mat-card-header>
                <mat-icon>play_circle</mat-icon>
                <mat-card-title>Execution Information</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <p>Execution ID: {{ getCurrentExecutionId() }}</p>
                <button 
                  mat-stroked-button 
                  (click)="loadReflection()"
                  *ngIf="getCurrentExecutionId()">
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
              [executionId]="getCurrentExecutionId() || ''" 
              *ngIf="getCurrentExecutionId()">
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
              [executionId]="getCurrentExecutionId()"
              (feedbackSubmitted)="onFeedbackSubmitted()"
              *ngIf="taskId()">
            </app-feedback-submit>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>

    <app-loading-state 
      *ngIf="loading()" 
      mode="spinner" 
      [size]="60"
      message="Loading task details...">
    </app-loading-state>
  `,
  styles: [`
    .task-detail {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    .back-button {
      margin-bottom: 16px;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .task-header {
      margin-bottom: 24px;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .header-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .header-badges {
      display: flex;
      gap: 8px;
      align-items: center;
      flex-wrap: wrap;
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

    .task-actions {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid rgba(0, 0, 0, 0.12);
    }

    .loading-overlay {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 400px;
    }
  `]
})
export class TaskDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private taskService = inject(TaskService);
  private notificationService = inject(NotificationService);

  taskId = signal<string | null>(null);
  task = signal<TaskDetailDto | null>(null);
  loading = signal<boolean>(false);
  executing = signal<boolean>(false);
  hasAgenticFeatures = signal<boolean>(false);

  ngOnInit() {
    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.taskId.set(id);
        this.loadTask();
      }
    });
  }

  private loadTask() {
    const id = this.taskId();
    if (!id) return;

    this.loading.set(true);
    
    this.taskService.getTask(id).subscribe({
      next: (task) => {
        this.task.set(task);
        this.checkAgenticFeatures();
        this.loading.set(false);
      },
      error: (err) => {
        this.notificationService.error('Failed to load task details');
        console.error('Task load error:', err);
        this.loading.set(false);
      }
    });
  }

  private checkAgenticFeatures() {
    // TODO: Check if task has plan, reflection, etc.
    this.hasAgenticFeatures.set(true);
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

  canExecute(): boolean {
    const status = this.task()?.status?.toLowerCase();
    return status === 'pending' || status === 'failed' || status === 'cancelled';
  }

  canCancel(): boolean {
    return this.task()?.status?.toLowerCase() === 'inprogress';
  }

  canRetry(): boolean {
    return this.task()?.status?.toLowerCase() === 'failed';
  }

  executeTask(strategy?: ExecutionStrategy): void {
    const id = this.taskId();
    if (!id) return;

    this.executing.set(true);
    
    this.taskService.executeTask(id, strategy ? { strategy } : undefined).subscribe({
      next: (response) => {
        this.notificationService.success(`Task execution started with ${response.strategy} strategy`);
        // Reload task to show updated status
        this.loadTask();
        this.executing.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to execute task');
        this.executing.set(false);
      }
    });
  }

  executeTaskWithStrategy(strategy: ExecutionStrategy): void {
    this.executeTask(strategy);
  }

  cancelTask(): void {
    const id = this.taskId();
    if (!id) return;

    this.executing.set(true);
    
    this.taskService.cancelTask(id).subscribe({
      next: () => {
        this.notificationService.success('Task cancelled successfully');
        this.loadTask();
        this.executing.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to cancel task');
        this.executing.set(false);
      }
    });
  }

  retryTask(): void {
    const id = this.taskId();
    if (!id) return;

    this.executing.set(true);
    
    this.taskService.retryTask(id).subscribe({
      next: (response) => {
        this.notificationService.success(`Task retry started with ${response.strategy} strategy`);
        this.loadTask();
        this.executing.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to retry task');
        this.executing.set(false);
      }
    });
  }

  getCurrentExecutionId(): string | undefined {
    const executions = this.task()?.executions;
    if (executions && executions.length > 0) {
      // Return the most recent execution (last in array or first with Running status)
      const runningExecution = executions.find(e => e.status === 'Running');
      return runningExecution?.id || executions[executions.length - 1]?.id;
    }
    return undefined;
  }
}

