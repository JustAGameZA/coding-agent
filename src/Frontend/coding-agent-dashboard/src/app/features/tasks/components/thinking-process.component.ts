import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTimelineModule } from '@angular/material/timeline';
import { AgenticAiService, ThinkingProcess, Thought } from '../../../../core/services/agentic-ai.service';

@Component({
  selector: 'app-thinking-process',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatExpansionModule,
    MatProgressBarModule
  ],
  template: `
    <mat-card class="thinking-process" *ngIf="process()">
      <mat-card-header>
        <mat-icon class="header-icon">psychology</mat-icon>
        <mat-card-title>AI Thinking Process</mat-card-title>
        <mat-chip class="status-chip" [class]="getProcessStatusClass()">
          {{ getProcessStatus() }}
        </mat-chip>
      </mat-card-header>
      <mat-card-content>
        <!-- Goal -->
        <div class="goal-section">
          <h3>
            <mat-icon>flag</mat-icon>
            Goal
          </h3>
          <p class="goal-text">{{ process()?.goal }}</p>
        </div>

        <!-- Duration -->
        <div class="duration-section">
          <mat-chip>
            <mat-icon>schedule</mat-icon>
            Duration: {{ getDuration() }}
          </mat-chip>
          <mat-chip>
            <mat-icon>list</mat-icon>
            {{ process()?.thoughts?.length || 0 }} thoughts
          </mat-chip>
          <mat-chip>
            <mat-icon>tune</mat-icon>
            {{ process()?.strategyAdjustments?.length || 0 }} strategy changes
          </mat-chip>
        </div>

        <!-- Thoughts Timeline -->
        <div class="thoughts-section">
          <h3>
            <mat-icon>timeline</mat-icon>
            Thinking Timeline
          </h3>
          <div class="thoughts-timeline">
            <div 
              *ngFor="let thought of process()?.thoughts; let i = index" 
              class="thought-item"
              [class]="getThoughtTypeClass(thought.type)">
              <div class="thought-marker">
                <mat-icon>{{ getThoughtTypeIcon(thought.type) }}</mat-icon>
              </div>
              <div class="thought-content">
                <div class="thought-header">
                  <span class="thought-type">{{ thought.type }}</span>
                  <mat-chip class="confidence-chip" [class]="getConfidenceClass(thought.confidence)">
                    {{ thought.confidence * 100 | number:'1.0-0' }}% confidence
                  </mat-chip>
                  <span class="thought-time">{{ formatTime(thought.timestamp) }}</span>
                </div>
                <div class="thought-text">{{ thought.content }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Strategy Adjustments -->
        <div *ngIf="process()?.strategyAdjustments?.length" class="strategies-section">
          <h3>
            <mat-icon>tune</mat-icon>
            Strategy Adjustments
          </h3>
          <div class="strategies-list">
            <mat-expansion-panel *ngFor="let strategy of process()?.strategyAdjustments" class="strategy-panel">
              <mat-expansion-panel-header>
                <mat-panel-title>{{ strategy.name }}</mat-panel-title>
                <mat-panel-description>
                  {{ formatDate(strategy.appliedAt) }}
                </mat-panel-description>
              </mat-expansion-panel-header>
              <div class="strategy-content">
                <p>{{ strategy.description }}</p>
                <div *ngIf="strategy.parameters" class="strategy-params">
                  <h4>Parameters:</h4>
                  <pre>{{ strategy.parameters | json }}</pre>
                </div>
              </div>
            </mat-expansion-panel>
          </div>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .thinking-process {
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

    .goal-section {
      margin: 24px 0;
      padding: 16px;
      background-color: #f5f5f5;
      border-radius: 8px;
    }

    .goal-section h3 {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
      color: #333;
    }

    .goal-text {
      font-size: 16px;
      color: #666;
      margin: 0;
    }

    .duration-section {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      margin: 16px 0;
    }

    .duration-section mat-chip {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .thoughts-section,
    .strategies-section {
      margin: 24px 0;
    }

    .thoughts-section h3,
    .strategies-section h3 {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
      color: #333;
    }

    .thoughts-timeline {
      display: flex;
      flex-direction: column;
      gap: 16px;
      position: relative;
      padding-left: 32px;
    }

    .thoughts-timeline::before {
      content: '';
      position: absolute;
      left: 16px;
      top: 0;
      bottom: 0;
      width: 2px;
      background-color: #e0e0e0;
    }

    .thought-item {
      display: flex;
      gap: 16px;
      position: relative;
    }

    .thought-marker {
      position: absolute;
      left: -24px;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: white;
      border: 2px solid #e0e0e0;
      z-index: 1;
    }

    .thought-item.thought-observation .thought-marker {
      border-color: #1976d2;
      color: #1976d2;
    }

    .thought-item.thought-hypothesis .thought-marker {
      border-color: #7b1fa2;
      color: #7b1fa2;
    }

    .thought-item.thought-decision .thought-marker {
      border-color: #388e3c;
      color: #388e3c;
    }

    .thought-item.thought-reflection .thought-marker {
      border-color: #f57c00;
      color: #f57c00;
    }

    .thought-content {
      flex: 1;
      padding: 12px;
      background-color: #fafafa;
      border-radius: 8px;
      border: 1px solid #e0e0e0;
    }

    .thought-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 8px;
    }

    .thought-type {
      font-weight: 500;
      color: #333;
    }

    .thought-time {
      margin-left: auto;
      font-size: 12px;
      color: #666;
    }

    .thought-text {
      color: #666;
      line-height: 1.5;
    }

    .confidence-chip {
      font-size: 12px;
      height: 24px;
    }

    .confidence-chip.high {
      background-color: #e8f5e9;
      color: #388e3c;
    }

    .confidence-chip.medium {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .confidence-chip.low {
      background-color: #ffebee;
      color: #c62828;
    }

    .strategies-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .strategy-panel {
      background-color: #fafafa;
    }

    .strategy-content {
      padding: 16px 0;
    }

    .strategy-params {
      margin-top: 16px;
    }

    .strategy-params h4 {
      margin-bottom: 8px;
      color: #333;
    }

    .strategy-params pre {
      background-color: white;
      padding: 12px;
      border-radius: 4px;
      overflow-x: auto;
      font-size: 12px;
    }
  `]
})
export class ThinkingProcessComponent {
  private agenticAiService = inject(AgenticAiService);
  
  @Input() processId!: string;
  process = signal<ThinkingProcess | null>(null);
  loading = signal<boolean>(false);

  ngOnInit() {
    if (this.processId) {
      this.loadProcess();
    }
  }

  private loadProcess() {
    this.loading.set(true);
    this.agenticAiService.getThinkingProcess(this.processId).subscribe({
      next: (result) => {
        this.process.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load thinking process:', err);
        this.loading.set(false);
      }
    });
  }

  getProcessStatus(): string {
    if (!this.process()?.endTime) return 'In Progress';
    return 'Completed';
  }

  getProcessStatusClass(): string {
    if (!this.process()?.endTime) return 'status-inprogress';
    return 'status-completed';
  }

  getDuration(): string {
    if (!this.process()) return 'N/A';
    const start = new Date(this.process()!.startTime);
    const end = this.process()!.endTime ? new Date(this.process()!.endTime) : new Date();
    const ms = end.getTime() - start.getTime();
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  }

  getThoughtTypeClass(type: string): string {
    return `thought-${type.toLowerCase()}`;
  }

  getThoughtTypeIcon(type: string): string {
    switch (type) {
      case 'Observation': return 'visibility';
      case 'Hypothesis': return 'science';
      case 'Decision': return 'gavel';
      case 'Reflection': return 'psychology';
      default: return 'help';
    }
  }

  getConfidenceClass(confidence: number): string {
    if (confidence >= 0.7) return 'high';
    if (confidence >= 0.4) return 'medium';
    return 'low';
  }

  formatTime(timestamp: string): string {
    return new Date(timestamp).toLocaleTimeString();
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString();
  }
}

