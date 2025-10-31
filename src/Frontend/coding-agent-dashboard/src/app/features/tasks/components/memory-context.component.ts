import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { AgenticAiService, MemoryContext } from '../../../../core/services/agentic-ai.service';

@Component({
  selector: 'app-memory-context',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatExpansionModule,
    MatProgressBarModule,
    MatTabsModule
  ],
  template: `
    <mat-card class="memory-context" *ngIf="context()">
      <mat-card-header>
        <mat-icon class="header-icon">memory</mat-icon>
        <mat-card-title>AI Memory Context</mat-card-title>
        <mat-chip class="relevance-chip">
          {{ context()?.episodicKnowledge?.length || 0 }} Episodes
          {{ context()?.semanticKnowledge?.length || 0 }} Memories
        </mat-chip>
      </mat-card-header>
      <mat-card-content>
        <mat-tab-group>
          <!-- Episodic Memory Tab -->
          <mat-tab label="Episodic Memory">
            <div class="memory-section">
              <div *ngIf="context()?.episodicKnowledge?.length; else noEpisodes" class="episodes-list">
                <div 
                  *ngFor="let episode of context()?.episodicKnowledge" 
                  class="episode-item">
                  <div class="episode-header">
                    <mat-icon class="episode-icon">{{ getEventTypeIcon(episode.eventType) }}</mat-icon>
                    <div class="episode-info">
                      <div class="episode-type">{{ episode.eventType }}</div>
                      <div class="episode-time">{{ formatDate(episode.timestamp) }}</div>
                    </div>
                    <mat-chip class="relevance-chip">
                      {{ (context()?.relevanceScores?.[episode.id] || 0) * 100 | number:'1.0-0' }}% relevant
                    </mat-chip>
                  </div>
                  <mat-expansion-panel *ngIf="episode.learnedPatterns?.length" class="patterns-panel">
                    <mat-expansion-panel-header>
                      <mat-panel-title>Learned Patterns</mat-panel-title>
                      <mat-panel-description>
                        {{ episode.learnedPatterns.length }} patterns
                      </mat-panel-description>
                    </mat-expansion-panel-header>
                    <ul class="patterns-list">
                      <li *ngFor="let pattern of episode.learnedPatterns">
                        <mat-icon>lightbulb</mat-icon>
                        {{ pattern }}
                      </li>
                    </ul>
                  </mat-expansion-panel>
                </div>
              </div>
              <ng-template #noEpisodes>
                <div class="empty-state">
                  <mat-icon>inbox</mat-icon>
                  <p>No episodic memories found</p>
                </div>
              </ng-template>
            </div>
          </mat-tab>

          <!-- Semantic Memory Tab -->
          <mat-tab label="Semantic Memory">
            <div class="memory-section">
              <div *ngIf="context()?.semanticKnowledge?.length; else noSemantic" class="semantic-list">
                <div 
                  *ngFor="let memory of context()?.semanticKnowledge" 
                  class="semantic-item">
                  <div class="semantic-header">
                    <mat-chip class="type-chip" [class]="'type-' + memory.contentType.toLowerCase()">
                      {{ memory.contentType }}
                    </mat-chip>
                    <mat-chip class="confidence-chip">
                      {{ memory.confidenceScore * 100 | number:'1.0-0' }}% confidence
                    </mat-chip>
                  </div>
                  <div class="semantic-content">
                    {{ memory.content }}
                  </div>
                  <div *ngIf="memory.metadata" class="semantic-metadata">
                    <mat-expansion-panel>
                      <mat-expansion-panel-header>
                        <mat-panel-title>Metadata</mat-panel-title>
                      </mat-expansion-panel-header>
                      <pre>{{ memory.metadata | json }}</pre>
                    </mat-expansion-panel>
                  </div>
                </div>
              </div>
              <ng-template #noSemantic>
                <div class="empty-state">
                  <mat-icon>inbox</mat-icon>
                  <p>No semantic memories found</p>
                </div>
              </ng-template>
            </div>
          </mat-tab>

          <!-- Procedures Tab -->
          <mat-tab label="Procedures">
            <div class="memory-section">
              <div *ngIf="context()?.relevantProcedures?.length; else noProcedures" class="procedures-list">
                <div 
                  *ngFor="let procedure of context()?.relevantProcedures" 
                  class="procedure-item">
                  <div class="procedure-header">
                    <mat-icon class="procedure-icon">play_circle</mat-icon>
                    <div class="procedure-info">
                      <div class="procedure-name">{{ procedure.procedureName }}</div>
                      <div class="procedure-description">{{ procedure.description }}</div>
                    </div>
                    <mat-chip class="success-rate-chip" [class]="getSuccessRateClass(procedure.successRate)">
                      {{ procedure.successRate * 100 | number:'1.0-0' }}% success
                    </mat-chip>
                  </div>
                  <div class="procedure-stats">
                    <mat-chip>
                      <mat-icon>history</mat-icon>
                      Used {{ procedure.usageCount }} times
                    </mat-chip>
                    <mat-chip *ngIf="procedure.lastUsedAt">
                      <mat-icon>schedule</mat-icon>
                      Last used {{ formatDate(procedure.lastUsedAt) }}
                    </mat-chip>
                  </div>
                </div>
              </div>
              <ng-template #noProcedures>
                <div class="empty-state">
                  <mat-icon>inbox</mat-icon>
                  <p>No relevant procedures found</p>
                </div>
              </ng-template>
            </div>
          </mat-tab>
        </mat-tab-group>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .memory-context {
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

    .relevance-chip {
      margin-left: auto;
    }

    .memory-section {
      padding: 16px 0;
    }

    .episodes-list,
    .semantic-list,
    .procedures-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .episode-item,
    .semantic-item,
    .procedure-item {
      padding: 16px;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      background-color: #fafafa;
    }

    .episode-header,
    .semantic-header,
    .procedure-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 12px;
    }

    .episode-icon {
      color: #1976d2;
    }

    .episode-info {
      flex: 1;
    }

    .episode-type {
      font-weight: 500;
      color: #333;
    }

    .episode-time {
      font-size: 12px;
      color: #666;
    }

    .patterns-panel {
      margin-top: 8px;
    }

    .patterns-list {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .patterns-list li {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 0;
    }

    .patterns-list li mat-icon {
      color: #f57c00;
    }

    .type-chip {
      background-color: #e3f2fd;
      color: #1976d2;
    }

    .confidence-chip {
      background-color: #f5f5f5;
      color: #666;
    }

    .semantic-content {
      margin: 12px 0;
      padding: 12px;
      background-color: white;
      border-radius: 4px;
      color: #333;
    }

    .procedure-icon {
      color: #388e3c;
    }

    .procedure-name {
      font-weight: 500;
      color: #333;
    }

    .procedure-description {
      font-size: 14px;
      color: #666;
      margin-top: 4px;
    }

    .success-rate-chip {
      font-weight: 500;
    }

    .success-rate-chip.high {
      background-color: #e8f5e9;
      color: #388e3c;
    }

    .success-rate-chip.medium {
      background-color: #fff3e0;
      color: #f57c00;
    }

    .success-rate-chip.low {
      background-color: #ffebee;
      color: #c62828;
    }

    .procedure-stats {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
      margin-top: 12px;
    }

    .procedure-stats mat-chip {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      color: #999;
    }

    .empty-state mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
    }
  `]
})
export class MemoryContextComponent {
  private agenticAiService = inject(AgenticAiService);
  
  @Input() query!: string;
  context = signal<MemoryContext | null>(null);
  loading = signal<boolean>(false);

  ngOnInit() {
    if (this.query) {
      this.loadContext();
    }
  }

  private loadContext() {
    this.loading.set(true);
    this.agenticAiService.getMemoryContext(this.query).subscribe({
      next: (result) => {
        this.context.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load memory context:', err);
        this.loading.set(false);
      }
    });
  }

  getEventTypeIcon(eventType: string): string {
    switch (eventType.toLowerCase()) {
      case 'task_started': return 'play_arrow';
      case 'error_occurred': return 'error';
      case 'success': return 'check_circle';
      case 'reflection': return 'psychology';
      default: return 'event';
    }
  }

  getSuccessRateClass(rate: number): string {
    if (rate >= 0.7) return 'high';
    if (rate >= 0.4) return 'medium';
    return 'low';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString();
  }
}

