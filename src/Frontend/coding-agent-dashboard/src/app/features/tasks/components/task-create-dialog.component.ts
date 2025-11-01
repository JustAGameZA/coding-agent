import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TaskService } from '../../../core/services/task.service';
import { NotificationService } from '../../../core/services/notifications/notification.service';
import { CreateTaskRequest } from '../../../core/models/task.models';

@Component({
  selector: 'app-task-create-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon>add_task</mat-icon>
      Create New Task
    </h2>

    <mat-dialog-content>
      <form [formGroup]="taskForm" (ngSubmit)="onSubmit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Title</mat-label>
          <mat-icon matPrefix>title</mat-icon>
          <input 
            matInput 
            formControlName="title"
            placeholder="Enter task title"
            [attr.data-testid]="'task-title-input'"
            maxlength="200"
            required>
          <mat-hint>{{ taskForm.get('title')?.value?.length || 0 }}/200 characters</mat-hint>
          <mat-error *ngIf="taskForm.get('title')?.hasError('required')">
            Title is required
          </mat-error>
          <mat-error *ngIf="taskForm.get('title')?.hasError('minlength')">
            Title must be at least 1 character
          </mat-error>
          <mat-error *ngIf="taskForm.get('title')?.hasError('maxlength')">
            Title must be at most 200 characters
          </mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Description</mat-label>
          <mat-icon matPrefix>description</mat-icon>
          <textarea 
            matInput 
            formControlName="description"
            placeholder="Describe what you want the AI to do..."
            [attr.data-testid]="'task-description-input'"
            rows="6"
            maxlength="10000"
            required></textarea>
          <mat-hint>{{ taskForm.get('description')?.value?.length || 0 }}/10,000 characters</mat-hint>
          <mat-error *ngIf="taskForm.get('description')?.hasError('required')">
            Description is required
          </mat-error>
          <mat-error *ngIf="taskForm.get('description')?.hasError('minlength')">
            Description must be at least 1 character
          </mat-error>
          <mat-error *ngIf="taskForm.get('description')?.hasError('maxlength')">
            Description must be at most 10,000 characters
          </mat-error>
        </mat-form-field>

        <div class="error-message" *ngIf="errorMessage()">
          <mat-icon>error</mat-icon>
          <span>{{ errorMessage() }}</span>
        </div>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button 
        mat-stroked-button 
        type="button"
        (click)="onCancel()"
        [disabled]="submitting()">
        Cancel
      </button>
      <button 
        mat-raised-button 
        color="primary"
        type="submit"
        (click)="onSubmit()"
        [disabled]="taskForm.invalid || submitting()"
        [attr.data-testid]="'task-create-button'">
        <mat-spinner 
          *ngIf="submitting()" 
          diameter="20" 
          class="button-spinner">
        </mat-spinner>
        <mat-icon *ngIf="!submitting()">add</mat-icon>
        <span>{{ submitting() ? 'Creating...' : 'Create Task' }}</span>
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2[mat-dialog-title] {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
    }

    h2[mat-dialog-title] mat-icon {
      color: #673ab7;
    }

    mat-dialog-content {
      min-width: 500px;
      max-width: 700px;
      padding: 24px;
    }

    form {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .full-width {
      width: 100%;
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      margin-top: 8px;
      background-color: #ffebee;
      border-radius: 4px;
      color: #c62828;
      font-size: 14px;
    }

    .error-message mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    mat-dialog-actions {
      padding: 16px 24px;
      gap: 12px;
    }

    button[type="submit"] {
      position: relative;
      min-width: 120px;
    }

    .button-spinner {
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
    }

    @media (max-width: 600px) {
      mat-dialog-content {
        min-width: auto;
        padding: 16px;
      }
    }
  `]
})
export class TaskCreateDialogComponent {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private notificationService = inject(NotificationService);
  private dialogRef = inject(MatDialogRef<TaskCreateDialogComponent>);

  taskForm: FormGroup;
  submitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  constructor() {
    this.taskForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(10000)]]
    });
  }

  onSubmit(): void {
    if (this.taskForm.invalid || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const request: CreateTaskRequest = {
      title: this.taskForm.value.title.trim(),
      description: this.taskForm.value.description.trim()
    };

    this.taskService.createTask(request).subscribe({
      next: (task) => {
        this.notificationService.success('Task created successfully!');
        this.dialogRef.close(task);
      },
      error: (error) => {
        this.errorMessage.set(error.message || 'Failed to create task. Please try again.');
        this.submitting.set(false);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}

