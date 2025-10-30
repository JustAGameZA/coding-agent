import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { UserListItem } from '../../../core/models/admin.models';

export interface UserEditDialogData {
  user: UserListItem;
}

export interface UserEditDialogResult {
  roles: string[];
}

/**
 * Dialog for editing user roles
 */
@Component({
  selector: 'app-user-edit-dialog',
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title>Edit User Roles</h2>
    
    <mat-dialog-content>
      <div class="dialog-content">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Username</mat-label>
          <input matInput [value]="data.user.username" readonly>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Email</mat-label>
          <input matInput [value]="data.user.email" readonly>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Roles</mat-label>
          <mat-select [(ngModel)]="selectedRoles" multiple>
            <mat-option value="User">User</mat-option>
            <mat-option value="Admin">Admin</mat-option>
          </mat-select>
          <mat-hint>Select one or more roles</mat-hint>
        </mat-form-field>
      </div>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="!hasChanges()">
        Save Changes
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-content {
      min-width: 400px;
      padding: 16px 0;
    }

    .full-width {
      width: 100%;
      margin-bottom: 16px;
    }

    mat-dialog-actions {
      padding: 16px 0 0;
    }
  `]
})
export class UserEditDialogComponent {
  protected readonly dialogRef = inject(MatDialogRef<UserEditDialogComponent>);
  protected readonly data = inject<UserEditDialogData>(MAT_DIALOG_DATA);

  protected selectedRoles: string[] = [...(this.data.user.roles || [])];

  protected hasChanges(): boolean {
    const original = [...(this.data.user.roles || [])].sort();
    const current = [...this.selectedRoles].sort();
    return JSON.stringify(original) !== JSON.stringify(current);
  }

  protected cancel(): void {
    this.dialogRef.close();
  }

  protected save(): void {
    this.dialogRef.close({ roles: this.selectedRoles } as UserEditDialogResult);
  }
}
