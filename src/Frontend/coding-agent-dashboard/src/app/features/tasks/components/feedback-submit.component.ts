import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { AgenticAiService, FeedbackRequest } from '../../../core/services/agentic-ai.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-feedback-submit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSliderModule,
    MatChipsModule
  ],
  template: `
    <mat-card class="feedback-card">
      <mat-card-header>
        <mat-icon class="header-icon">feedback</mat-icon>
        <mat-card-title>Provide Feedback</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="feedbackForm" (ngSubmit)="onSubmit()">
          <!-- Feedback Type -->
          <mat-form-field appearance="outline">
            <mat-label>Feedback Type</mat-label>
            <mat-select formControlName="type" required>
              <mat-option value="Positive">
                <mat-icon class="success-icon">thumb_up</mat-icon>
                Positive
              </mat-option>
              <mat-option value="Negative">
                <mat-icon class="error-icon">thumb_down</mat-icon>
                Negative
              </mat-option>
              <mat-option value="Neutral">
                <mat-icon>thumbs_up_down</mat-icon>
                Neutral
              </mat-option>
            </mat-select>
            <mat-icon matPrefix>rate_review</mat-icon>
          </mat-form-field>

          <!-- Rating Slider -->
          <div class="rating-section">
            <label>Rating: {{ feedbackForm.get('rating')?.value | number:'1.1-1' }}</label>
            <mat-slider
              formControlName="rating"
              min="0"
              max="1"
              step="0.1"
              discrete
              [displayWith]="formatRating">
              <input matSliderThumb>
            </mat-slider>
            <div class="rating-labels">
              <span>Poor (0.0)</span>
              <span>Excellent (1.0)</span>
            </div>
          </div>

          <!-- Reason -->
          <mat-form-field appearance="outline">
            <mat-label>Reason (optional)</mat-label>
            <textarea 
              matInput 
              formControlName="reason" 
              rows="4"
              placeholder="Describe why you gave this feedback...">
            </textarea>
            <mat-icon matPrefix>description</mat-icon>
          </mat-form-field>

          <!-- Submit Button -->
          <div class="actions">
            <button 
              mat-raised-button 
              color="primary" 
              type="submit"
              [disabled]="feedbackForm.invalid || submitting()">
              <mat-icon>send</mat-icon>
              Submit Feedback
            </button>
            <button 
              mat-stroked-button 
              type="button"
              (click)="onCancel()"
              [disabled]="submitting()">
              Cancel
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .feedback-card {
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

    form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .rating-section {
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding: 16px;
      background-color: #f5f5f5;
      border-radius: 8px;
    }

    .rating-section label {
      font-weight: 500;
      color: #333;
    }

    .rating-labels {
      display: flex;
      justify-content: space-between;
      font-size: 12px;
      color: #666;
    }

    .success-icon {
      color: #388e3c;
    }

    .error-icon {
      color: #c62828;
    }

    .actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      margin-top: 8px;
    }

    mat-form-field {
      width: 100%;
    }
  `]
})
export class FeedbackSubmitComponent {
  private fb = inject(FormBuilder);
  private agenticAiService = inject(AgenticAiService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  @Input() taskId!: string;
  @Input() executionId?: string;
  @Output() feedbackSubmitted = new EventEmitter<void>();

  feedbackForm: FormGroup;
  submitting = signal<boolean>(false);

  constructor() {
    this.feedbackForm = this.fb.group({
      type: ['Positive', Validators.required],
      rating: [0.8, [Validators.required, Validators.min(0), Validators.max(1)]],
      reason: ['']
    });
  }

  onSubmit() {
    if (this.feedbackForm.invalid) return;

    // Get user ID - for now use a default, will be enhanced with proper auth
    const userId = '00000000-0000-0000-0000-000000000001';

    this.submitting.set(true);

    const feedback: FeedbackRequest = {
      taskId: this.taskId,
      executionId: this.executionId,
      userId: userId,
      type: this.feedbackForm.value.type,
      rating: this.feedbackForm.value.rating,
      reason: this.feedbackForm.value.reason || undefined
    };

    this.agenticAiService.submitFeedback(feedback).subscribe({
      next: () => {
        this.snackBar.open('Feedback submitted successfully!', 'Close', { duration: 3000 });
        this.feedbackForm.reset({
          type: 'Positive',
          rating: 0.8,
          reason: ''
        });
        this.feedbackSubmitted.emit();
        this.submitting.set(false);
      },
      error: (err: any) => {
        console.error('Failed to submit feedback:', err);
        this.snackBar.open('Failed to submit feedback. Please try again.', 'Close', { duration: 3000 });
        this.submitting.set(false);
      }
    });
  }

  onCancel() {
    this.feedbackForm.reset({
      type: 'Positive',
      rating: 0.8,
      reason: ''
    });
  }

  formatRating(value: number): string {
    return value.toFixed(1);
  }
}

