import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { DashboardService } from '../../core/services/dashboard.service';
import { EnrichedTask } from '../../core/models/dashboard.models';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { formatDuration as formatDurationUtil } from '../../shared/utils/time.utils';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule, 
    MatTableModule, 
    MatPaginatorModule, 
    MatIconModule, 
    MatProgressSpinnerModule,
    MatChipsModule,
    MatButtonModule,
    MatTooltipModule,
    StatusChipComponent,
    AgenticBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent
  ],
  template: `
    <div class="tasks-container">
      <mat-card>
        <mat-card-header>
          <mat-icon class="header-icon">assignment</mat-icon>
          <mat-card-title>Tasks</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <app-loading-state 
            *ngIf="loading()" 
            mode="spinner" 
            size="50"
            message="Loading tasks...">
          </app-loading-state>

          <div class="error-message" *ngIf="error()">
            <mat-icon>error</mat-icon>
            <span>{{ error() }}</span>
          </div>

          <div class="table-container" *ngIf="!loading() && tasks().length > 0">
            <table mat-table [dataSource]="tasks()" class="tasks-table" [attr.data-testid]="'tasks-table'">
              <!-- Title Column -->
              <ng-container matColumnDef="title">
                <th mat-header-cell *matHeaderCellDef>Title</th>
                <td mat-cell *matCellDef="let task" [attr.data-testid]="'task-title'">
                  <div class="title-cell">
                    <a [routerLink]="['/tasks', task.id]" class="task-link">
                      {{ task.title }}
                    </a>
                    <app-agentic-badge 
                      *ngIf="hasAgenticFeatures(task.id)"
                      label="AI"
                      tooltip="Agentic AI features active">
                    </app-agentic-badge>
                  </div>
                </td>
              </ng-container>

              <!-- Type Column -->
              <ng-container matColumnDef="type">
                <th mat-header-cell *matHeaderCellDef>Type</th>
                <td mat-cell *matCellDef="let task">
                  <mat-chip [class]="'type-chip type-' + task.type.toLowerCase()" [attr.data-testid]="'task-type'">
                    {{ task.type }}
                  </mat-chip>
                </td>
              </ng-container>

              <!-- Complexity Column -->
              <ng-container matColumnDef="complexity">
                <th mat-header-cell *matHeaderCellDef>Complexity</th>
                <td mat-cell *matCellDef="let task">
                  <mat-chip [class]="'complexity-chip complexity-' + task.complexity.toLowerCase()" [attr.data-testid]="'task-complexity'">
                    {{ task.complexity }}
                  </mat-chip>
                </td>
              </ng-container>

              <!-- Status Column -->
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let task">
                  <app-status-chip 
                    [status]="task.status"
                    [agenticEnabled]="hasAgenticFeatures(task.id)"
                    [badge]="getStatusBadge(task)">
                  </app-status-chip>
                </td>
              </ng-container>

              <!-- Duration Column -->
              <ng-container matColumnDef="duration">
                <th mat-header-cell *matHeaderCellDef>Duration</th>
                <td mat-cell *matCellDef="let task" [attr.data-testid]="'task-duration'">
                  {{ task.duration ? formatDuration(task.duration) : 'N/A' }}
                </td>
              </ng-container>

              <!-- Created At Column -->
              <ng-container matColumnDef="createdAt">
                <th mat-header-cell *matHeaderCellDef>Created</th>
                <td mat-cell *matCellDef="let task" [attr.data-testid]="'task-created'">
                  {{ formatDate(task.createdAt) }}
                </td>
              </ng-container>

              <!-- PR Link Column -->
              <ng-container matColumnDef="pr">
                <th mat-header-cell *matHeaderCellDef>PR</th>
                <td mat-cell *matCellDef="let task">
                  <a *ngIf="task.pullRequestNumber" 
                     [href]="'#/pr/' + task.pullRequestNumber" 
                     class="pr-link"
                     [attr.data-testid]="'task-pr-link'">
                    <mat-icon>code</mat-icon>
                    #{{ task.pullRequestNumber }}
                  </a>
                  <span *ngIf="!task.pullRequestNumber" class="no-pr">-</span>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;" [attr.data-testid]="'task-row'"></tr>
            </table>

            <mat-paginator
              [attr.data-testid]="'tasks-paginator'"
              [length]="totalCount()"
              [pageSize]="pageSize()"
              [pageSizeOptions]="[10, 20, 50, 100]"
              [pageIndex]="currentPage() - 1"
              (page)="onPageChange($event)"
              showFirstLastButtons>
            </mat-paginator>
          </div>

          <app-empty-state 
            *ngIf="!loading() && tasks().length === 0"
            icon="assignment"
            title="No Tasks"
            message="You haven't created any tasks yet. Create your first task to get started!">
          </app-empty-state>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .tasks-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 24px;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 24px;
    }

    .header-icon {
      font-size: 28px;
      width: 28px;
      height: 28px;
    }

    .loading-overlay {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 300px;
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

    .table-container {
      overflow-x: auto;
    }

    .tasks-table {
      width: 100%;
      min-width: 800px;
    }

    .tasks-table th {
      font-weight: 600;
      color: #333;
    }

    .tasks-table td {
      padding: 12px 8px;
    }

    mat-chip {
      font-size: 12px;
      min-height: 24px;
      padding: 4px 8px;
    }

    /* Type chips */
    .type-chip { background-color: #e3f2fd; color: #1976d2; }
    .type-bugfix { background-color: #ffebee; color: #c62828; }
    .type-feature { background-color: #e8f5e9; color: #388e3c; }
    .type-refactor { background-color: #f3e5f5; color: #7b1fa2; }

    /* Complexity chips */
    .complexity-chip { background-color: #f5f5f5; color: #666; }
    .complexity-simple { background-color: #e8f5e9; color: #388e3c; }
    .complexity-medium { background-color: #fff3e0; color: #f57c00; }
    .complexity-complex { background-color: #ffebee; color: #d32f2f; }

    /* Status chips */
    .status-chip {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    .status-chip mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }
    .status-completed {
      background-color: #e8f5e9;
      color: #388e3c;
    }
    .status-failed {
      background-color: #ffebee;
      color: #c62828;
    }
    .status-running {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .pr-link {
      display: flex;
      align-items: center;
      gap: 4px;
      color: #1976d2;
      text-decoration: none;
      font-size: 14px;
    }

    .pr-link:hover {
      text-decoration: underline;
    }

    .pr-link mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
    }

    .no-pr {
      color: #999;
    }

    .title-cell {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .task-link {
      color: #333;
      text-decoration: none;
      font-weight: 500;
      transition: color 0.2s ease;

      &:hover {
        color: #673ab7;
        text-decoration: underline;
      }
    }

    mat-paginator {
      margin-top: 16px;
    }

    @media (max-width: 768px) {
      .tasks-container {
        padding: 16px;
      }

      .tasks-table {
        min-width: 600px;
      }

      .displayedColumns {
        font-size: 12px;
      }
    }
  `]
})
export class TasksComponent {
  private dashboardService = inject(DashboardService);
  private notificationService = inject(NotificationService);
  private agenticAiService = inject(AgenticAiService);

  tasks = signal<EnrichedTask[]>([]);
  loading = signal<boolean>(true);
  error = signal<string | null>(null);
  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  totalCount = signal<number>(0);
  agenticTaskIds = signal<Set<string>>(new Set()); // Track tasks with agentic AI features

  displayedColumns: string[] = ['title', 'type', 'complexity', 'status', 'duration', 'createdAt', 'pr'];

  ngOnInit() {
    this.loadTasks();
  }

  private loadTasks() {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService.getTasks(this.currentPage(), this.pageSize()).subscribe({
      next: (data: any) => {
        // Accept either array or paged response { items, totalCount }
        const items = Array.isArray(data) ? data : (data?.items ?? []);
        const total = Array.isArray(data) ? data.length : (data?.totalCount ?? items.length);
        this.tasks.set(items);
        this.totalCount.set(total);
        this.loading.set(false);
      },
      error: (err) => {
        const errorMsg = 'Failed to load tasks';
        this.error.set(errorMsg);
        this.loading.set(false);
        this.notificationService.error(errorMsg);
        console.error('Tasks load error:', err);
      }
    });
  }

  onPageChange(event: PageEvent) {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadTasks();
  }

  getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'check_circle';
      case 'failed':
        return 'error';
      case 'running':
        return 'sync';
      default:
        return 'help';
    }
  }

  // Expose shared util for template binding
  formatDuration = formatDurationUtil;

  formatDate(dateString: string): string {
    if (!dateString) return 'N/A';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return dateString;
    }
  }

  hasAgenticFeatures(taskId: string): boolean {
    // Check if task has agentic AI features (plan, reflection, etc.)
    return this.agenticTaskIds().has(taskId);
  }

  getStatusBadge(task: EnrichedTask): string | undefined {
    // Return badge text for special statuses
    if (task.status.toLowerCase() === 'running') {
      return 'AI Active';
    }
    return undefined;
  }
}
