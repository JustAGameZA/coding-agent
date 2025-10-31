import { Component, Input, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AgenticAiService, ReflectionResult } from '../../../../core/services/agentic-ai.service';

@Component({
  selector: 'app-reflection-panel',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatChipsModule, MatExpansionModule, MatProgressBarModule],
  template: `
    <mat-card class="reflection-panel" *ngIf="reflection()">
      <mat-card-header>
        <mat-icon class="header-icon">psychology</mat-icon>
        <mat-card-title>AI Reflection</mat-card-title>
        <mat-chip [class]="getConfidenceClass()" class="confidence-chip">
          {{ (reflection()?.confidenceScore || 0) * 100 | number:'1.0-0' }}% Confidence
        </mat-chip>
      </mat-card-header>
      <mat-card-content>
        <div class="reflection-sections">
          <!-- Strengths -->
          <mat-expansion-panel *ngIf="reflection()?.strengths?.length" class="strength-panel">
            <mat-expansion-panel-header>
              <mat-panel-title>
                <mat-icon class="success-icon">check_circle</mat-icon>
                Strengths
              </mat-panel-title>
              <mat-panel-description>
                {{ reflection()?.strengths?.length }} items
              </mat-panel-description>
            </mat-expansion-panel-header>
            <ul class="reflection-list">
              <li *ngFor="let strength of reflection()?.strengths">
                <mat-icon>arrow_right</mat-icon>
                {{ strength }}
              </li>
            </ul>
          </mat-expansion-panel>

          <!-- Weaknesses -->
          <mat-expansion-panel *ngIf="reflection()?.weaknesses?.length" class="weakness-panel">
            <mat-expansion-panel-header>
              <mat-panel-title>
                <mat-icon class="warning-icon">warning</mat-icon>
                Areas for Improvement
              </mat-panel-title>
              <mat-panel-description>
                {{ reflection()?.weaknesses?.length }} items
              </mat-panel-description>
            </mat-expansion-panel-header>
            <ul class="reflection-list">
              <li *ngFor="let weakness of reflection()?.weaknesses">
                <mat-icon>arrow_right</mat-icon>
                {{ weakness }}
              </li>
            </ul>
          </mat-expansion-panel>

          <!-- Key Lessons -->
          <mat-expansion-panel *ngIf="reflection()?.keyLessons?.length" class="lessons-panel">
            <mat-expansion-panel-header>
              <mat-panel-title>
                <mat-icon class="info-icon">school</mat-icon>
                Key Lessons Learned
              </mat-panel-title>
              <mat-panel-description>
                {{ reflection()?.keyLessons?.length }} lessons
              </mat-panel-description>
            </mat-expansion-panel-header>
            <ul class="reflection-list">
              <li *ngFor="let lesson of reflection()?.keyLessons">
                <mat-icon>lightbulb</mat-icon>
                {{ lesson }}
              </li>
            </ul>
          </mat-expansion-panel>

          <!-- Improvement Suggestions -->
          <mat-expansion-panel *ngIf="reflection()?.improvementSuggestions?.length" class="suggestions-panel">
            <mat-expansion-panel-header>
              <mat-panel-title>
                <mat-icon class="suggestion-icon">auto_fix_high</mat-icon>
                Improvement Suggestions
              </mat-panel-title>
              <mat-panel-description>
                {{ reflection()?.improvementSuggestions?.length }} suggestions
              </mat-panel-description>
            </mat-expansion-panel-header>
            <ul class="reflection-list">
              <li *ngFor="let suggestion of reflection()?.improvementSuggestions">
                <mat-icon>trending_up</mat-icon>
                {{ suggestion }}
              </li>
            </ul>
          </mat-expansion-panel>
        </div>

        <!-- Confidence Bar -->
        <div class="confidence-bar">
          <mat-progress-bar 
            mode="determinate" 
            [value]="(reflection()?.confidenceScore || 0) * 100"
            [class]="getConfidenceBarClass()">
          </mat-progress-bar>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .reflection-panel {
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

    .confidence-chip {
      margin-left: auto;
      font-weight: 500;
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

    .reflection-sections {
      margin-top: 16px;
    }

    .reflection-sections mat-expansion-panel {
      margin-bottom: 8px;
    }

    .success-icon {
      color: #388e3c;
    }

    .warning-icon {
      color: #f57c00;
    }

    .info-icon {
      color: #1976d2;
    }

    .suggestion-icon {
      color: #7b1fa2;
    }

    .reflection-list {
      list-style: none;
      padding: 0;
      margin: 0;
    }

    .reflection-list li {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      padding: 8px 0;
      border-bottom: 1px solid #eee;
    }

    .reflection-list li:last-child {
      border-bottom: none;
    }

    .reflection-list li mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      margin-top: 2px;
      color: #666;
    }

    .confidence-bar {
      margin-top: 16px;
    }

    .confidence-bar mat-progress-bar {
      height: 8px;
      border-radius: 4px;
    }

    .confidence-bar mat-progress-bar.high {
      ::ng-deep .mdc-linear-progress__buffer {
        background-color: #e8f5e9;
      }
      ::ng-deep .mdc-linear-progress__bar-inner {
        background-color: #388e3c;
      }
    }

    .confidence-bar mat-progress-bar.medium {
      ::ng-deep .mdc-linear-progress__buffer {
        background-color: #fff3e0;
      }
      ::ng-deep .mdc-linear-progress__bar-inner {
        background-color: #f57c00;
      }
    }

    .confidence-bar mat-progress-bar.low {
      ::ng-deep .mdc-linear-progress__buffer {
        background-color: #ffebee;
      }
      ::ng-deep .mdc-linear-progress__bar-inner {
        background-color: #c62828;
      }
    }
  `]
})
export class ReflectionPanelComponent {
  private agenticAiService = inject(AgenticAiService);
  
  @Input() executionId!: string;
  reflection = signal<ReflectionResult | null>(null);
  loading = signal<boolean>(false);

  ngOnInit() {
    if (this.executionId) {
      this.loadReflection();
    }
  }

  private loadReflection() {
    this.loading.set(true);
    this.agenticAiService.getReflection(this.executionId).subscribe({
      next: (result) => {
        this.reflection.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load reflection:', err);
        this.loading.set(false);
      }
    });
  }

  getConfidenceClass(): string {
    const score = this.reflection()?.confidenceScore || 0;
    if (score >= 0.7) return 'high';
    if (score >= 0.4) return 'medium';
    return 'low';
  }

  getConfidenceBarClass(): string {
    return this.getConfidenceClass();
  }
}


