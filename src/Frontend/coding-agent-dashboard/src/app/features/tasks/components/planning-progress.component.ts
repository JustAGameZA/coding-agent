import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatExpansionModule } from '@angular/material/expansion';
import { AgenticAiService, Plan, PlanStep } from '../../../core/services/agentic-ai.service';

@Component({
  selector: 'app-planning-progress',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatStepperModule,
    MatExpansionModule
  ],
  template: `
    <mat-card class="planning-progress" *ngIf="plan()">
      <mat-card-header>
        <mat-icon class="header-icon">route</mat-icon>
        <mat-card-title>Execution Plan</mat-card-title>
        <mat-chip [class]="getStatusClass()" class="status-chip">
          {{ plan()?.status }}
        </mat-chip>
      </mat-card-header>
      <mat-card-content>
        <div class="plan-info">
          <p class="goal">{{ plan()?.goal }}</p>
          <p class="description">{{ plan()?.description }}</p>
          <div class="plan-meta">
            <mat-chip>
              <mat-icon>schedule</mat-icon>
              {{ plan()?.estimatedTotalEffort }}
            </mat-chip>
            <mat-chip>
              <mat-icon>list</mat-icon>
              {{ plan()?.subTasks?.length }} steps
            </mat-chip>
          </div>
        </div>

        <!-- Progress Bar -->
        <div class="progress-section">
          <div class="progress-header">
            <span>Overall Progress</span>
            <span class="progress-percent">{{ getProgressPercent() }}%</span>
          </div>
          <mat-progress-bar 
            mode="determinate" 
            [value]="getProgressPercent()"
            [class]="getStatusClass()">
          </mat-progress-bar>
        </div>

        <!-- Plan Steps -->
        <div class="steps-section">
          <h3>Plan Steps</h3>
          <div class="steps-list">
            <div 
              *ngFor="let step of plan()?.subTasks; let i = index" 
              class="step-item"
              [class]="getStepClass(step.status)">
              <div class="step-header">
                <mat-icon class="step-icon">{{ getStepIcon(step.status) }}</mat-icon>
                <div class="step-info">
                  <div class="step-title">{{ step.description }}</div>
                  <div class="step-meta">
                    <mat-chip class="effort-chip" [class]="'effort-' + step.estimatedEffort">
                      {{ step.estimatedEffort }} effort
                    </mat-chip>
                    <mat-chip *ngIf="step.dependencies?.length" class="deps-chip">
                      <mat-icon>link</mat-icon>
                      {{ step.dependencies.length }} dependencies
                    </mat-chip>
                  </div>
                </div>
                <mat-chip class="step-status-chip" [class]="'status-' + step.status.toLowerCase()">
                  {{ step.status }}
                </mat-chip>
              </div>
              <div *ngIf="step.result" class="step-result">
                <mat-icon [class]="step.result.success ? 'success-icon' : 'error-icon'">
                  {{ step.result.success ? 'check_circle' : 'error' }}
                </mat-icon>
                <span>{{ step.result.success ? 'Completed' : 'Failed' }}</span>
                <span *ngIf="step.result.error" class="error-text">{{ step.result.error }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Risks -->
        <div *ngIf="plan()?.risks?.length" class="risks-section">
          <h3>
            <mat-icon class="warning-icon">warning</mat-icon>
            Identified Risks
          </h3>
          <ul class="risks-list">
            <li *ngFor="let risk of plan()?.risks">{{ risk }}</li>
          </ul>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .planning-progress {
      margin: 16px 0;
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

    .status-chip.status-inprogress {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .status-chip.status-failed {
      background-color: #ffebee;
      color: #c62828;
    }

    .plan-info {
      margin: 16px 0;
    }

    .goal {
      font-size: 18px;
      font-weight: 600;
      margin-bottom: 8px;
      color: #333;
    }

    .description {
      color: #666;
      margin-bottom: 16px;
    }

    .plan-meta {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .plan-meta mat-chip {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .progress-section {
      margin: 24px 0;
    }

    .progress-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
      font-weight: 500;
    }

    .progress-percent {
      font-size: 18px;
      font-weight: 600;
    }

    .steps-section {
      margin: 24px 0;
    }

    .steps-section h3 {
      margin-bottom: 16px;
      color: #333;
    }

    .steps-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .step-item {
      padding: 16px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background-color: #fafafa;
    }

    .step-item.step-pending {
      border-left: 4px solid #9e9e9e;
    }

    .step-item.step-inprogress {
      border-left: 4px solid #f57c00;
      background-color: #fff3e0;
    }

    .step-item.step-completed {
      border-left: 4px solid #388e3c;
      background-color: #e8f5e9;
    }

    .step-item.step-failed {
      border-left: 4px solid #c62828;
      background-color: #ffebee;
    }

    .step-header {
      display: flex;
      align-items: flex-start;
      gap: 12px;
    }

    .step-icon {
      margin-top: 4px;
    }

    .step-info {
      flex: 1;
    }

    .step-title {
      font-weight: 500;
      margin-bottom: 8px;
      color: #333;
    }

    .step-meta {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }

    .effort-chip.effort-low {
      background-color: #e8f5e9;
      color: #388e3c;
    }

    .effort-chip.effort-medium {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .effort-chip.effort-high {
      background-color: #ffebee;
      color: #c62828;
    }

    .deps-chip {
      background-color: #e3f2fd;
      color: #1976d2;
    }

    .step-status-chip {
      font-weight: 500;
    }

    .step-result {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px solid #e0e0e0;
    }

    .success-icon {
      color: #388e3c;
    }

    .error-icon {
      color: #c62828;
    }

    .error-text {
      color: #c62828;
      font-style: italic;
    }

    .risks-section {
      margin-top: 24px;
      padding: 16px;
      background-color: #fff3e0;
      border-radius: 8px;
    }

    .risks-section h3 {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
      color: #f57c00;
    }

    .warning-icon {
      color: #f57c00;
    }

    .risks-list {
      margin: 0;
      padding-left: 24px;
    }

    .risks-list li {
      margin-bottom: 8px;
      color: #666;
    }
  `]
})
export class PlanningProgressComponent {
  private agenticAiService = inject(AgenticAiService);
  
  @Input() taskId!: string;
  plan = signal<Plan | null>(null);
  loading = signal<boolean>(false);

  ngOnInit() {
    if (this.taskId) {
      this.loadPlan();
    }
  }

  private loadPlan() {
    this.loading.set(true);
    this.agenticAiService.getPlan(this.taskId).subscribe({
      next: (result: Plan) => {
        this.plan.set(result);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Failed to load plan:', err);
        this.loading.set(false);
      }
    });
  }

  getProgressPercent(): number {
    if (!this.plan()?.subTasks?.length) return 0;
    const completed = this.plan()!.subTasks?.filter((s: PlanStep) => s.status === 'Completed').length || 0;
    return Math.round((completed / this.plan()!.subTasks.length) * 100);
  }

  getStatusClass(): string {
    const status = this.plan()?.status?.toLowerCase() || '';
    return `status-${status}`;
  }

  getStepClass(status: string): string {
    return `step-${status.toLowerCase()}`;
  }

  getStepIcon(status: string): string {
    switch (status) {
      case 'Completed': return 'check_circle';
      case 'Failed': return 'error';
      case 'InProgress': return 'sync';
      case 'Skipped': return 'skip_next';
      default: return 'radio_button_unchecked';
    }
  }
}

