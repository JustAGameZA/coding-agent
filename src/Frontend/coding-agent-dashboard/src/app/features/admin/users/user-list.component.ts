import { Component, signal, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminService } from '../../../core/services/admin.service';
import { UserListItem } from '../../../core/models/admin.models';
import { UserEditDialogComponent, UserEditDialogData, UserEditDialogResult } from './user-edit-dialog.component';

/**
 * User list component for admin user management
 */
@Component({
  selector: 'app-user-list',
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule
  ],
  template: `
    <div class="user-list-container">
      <div class="header">
        <h1>User Management</h1>
        <p class="subtitle">Manage user accounts and roles</p>
      </div>

      <div class="toolbar">
        <mat-form-field appearance="outline" class="search-field">
          <mat-label>Search users</mat-label>
          <input matInput [(ngModel)]="searchText" (keyup.enter)="search()" placeholder="Username or email">
          <mat-icon matPrefix>search</mat-icon>
        </mat-form-field>
        <button mat-raised-button color="primary" (click)="search()">
          <mat-icon>search</mat-icon>
          Search
        </button>
        <button mat-button (click)="clearSearch()" *ngIf="searchText">
          <mat-icon>clear</mat-icon>
          Clear
        </button>
      </div>

      @if (loading()) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
        </div>
      } @else if (error()) {
        <div class="error-container">
          <mat-icon color="warn">error</mat-icon>
          <p>{{ error() }}</p>
          <button mat-raised-button color="primary" (click)="loadUsers()">Retry</button>
        </div>
      } @else {
        <div class="table-container">
          <table mat-table [dataSource]="users()" class="user-table">
            <!-- Username Column -->
            <ng-container matColumnDef="username">
              <th mat-header-cell *matHeaderCellDef>Username</th>
              <td mat-cell *matCellDef="let user">{{ user.username }}</td>
            </ng-container>

            <!-- Email Column -->
            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let user">{{ user.email }}</td>
            </ng-container>

            <!-- Roles Column -->
            <ng-container matColumnDef="roles">
              <th mat-header-cell *matHeaderCellDef>Roles</th>
              <td mat-cell *matCellDef="let user">
                <mat-chip-set>
                  @for (role of user.roles; track role) {
                    <mat-chip [class.admin-chip]="role === 'Admin'">{{ role }}</mat-chip>
                  }
                </mat-chip-set>
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="active">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let user">
                <mat-chip [class.active-chip]="user.isActive" [class.inactive-chip]="!user.isActive">
                  {{ user.isActive ? 'Active' : 'Inactive' }}
                </mat-chip>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let user">
                <button mat-icon-button (click)="editRoles(user)" matTooltip="Edit roles">
                  <mat-icon>edit</mat-icon>
                </button>
                @if (user.isActive) {
                  <button mat-icon-button color="warn" (click)="deactivateUser(user)" matTooltip="Deactivate">
                    <mat-icon>block</mat-icon>
                  </button>
                } @else {
                  <button mat-icon-button color="primary" (click)="activateUser(user)" matTooltip="Activate">
                    <mat-icon>check_circle</mat-icon>
                  </button>
                }
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator
            [length]="totalCount()"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 25, 50, 100]"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>
        </div>
      }
    </div>
  `,
  styles: [`
    .user-list-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header {
      margin-bottom: 24px;
    }

    h1 {
      margin: 0 0 8px 0;
      font-size: 32px;
      font-weight: 400;
    }

    .subtitle {
      margin: 0;
      color: rgba(0, 0, 0, 0.6);
      font-size: 16px;
    }

    .toolbar {
      display: flex;
      gap: 16px;
      align-items: center;
      margin-bottom: 24px;
    }

    .search-field {
      flex: 1;
      max-width: 400px;
    }

    .loading-container,
    .error-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
    }

    .error-container mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }

    .table-container {
      background: white;
      border-radius: 4px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .user-table {
      width: 100%;
    }

    .admin-chip {
      background-color: #673ab7 !important;
      color: white !important;
    }

    .active-chip {
      background-color: #4caf50 !important;
      color: white !important;
    }

    .inactive-chip {
      background-color: #9e9e9e !important;
      color: white !important;
    }

    mat-paginator {
      border-top: 1px solid rgba(0, 0, 0, 0.12);
    }
  `]
})
export class UserListComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  protected readonly users = signal<UserListItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly page = signal(0);
  protected readonly pageSize = signal(10);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected searchText = '';

  protected readonly displayedColumns = ['username', 'email', 'roles', 'active', 'actions'];

  async ngOnInit(): Promise<void> {
    await this.loadUsers();
  }

  protected async loadUsers(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await this.adminService.getUsers(
        this.page() + 1, // API uses 1-based indexing
        this.pageSize(),
        this.searchText || undefined
      );

      this.users.set(response.users);
      this.totalCount.set(response.totalCount);
    } catch (err: any) {
      this.error.set(err.message || 'Failed to load users');
      this.snackBar.open('Failed to load users', 'Close', { duration: 5000 });
    } finally {
      this.loading.set(false);
    }
  }

  protected async search(): Promise<void> {
    this.page.set(0);
    await this.loadUsers();
  }

  protected async clearSearch(): Promise<void> {
    this.searchText = '';
    this.page.set(0);
    await this.loadUsers();
  }

  protected async onPageChange(event: PageEvent): Promise<void> {
    this.page.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    await this.loadUsers();
  }

  protected editRoles(user: UserListItem): void {
    const dialogRef = this.dialog.open(UserEditDialogComponent, {
      width: '500px',
      data: { user } as UserEditDialogData
    });

    dialogRef.afterClosed().subscribe(async (result: UserEditDialogResult | undefined) => {
      if (result) {
        await this.updateRoles(user.id, result.roles);
      }
    });
  }

  private async updateRoles(userId: string, roles: string[]): Promise<void> {
    try {
      await this.adminService.updateUserRoles(userId, roles);
      this.snackBar.open('User roles updated successfully', 'Close', { duration: 3000 });
      await this.loadUsers();
    } catch (err: any) {
      this.snackBar.open(err.message || 'Failed to update roles', 'Close', { duration: 5000 });
    }
  }

  protected async deactivateUser(user: UserListItem): Promise<void> {
    try {
      await this.adminService.deactivateUser(user.id);
      this.snackBar.open(`User ${user.username} deactivated`, 'Close', { duration: 3000 });
      await this.loadUsers();
    } catch (err: any) {
      this.snackBar.open(err.message || 'Failed to deactivate user', 'Close', { duration: 5000 });
    }
  }

  protected async activateUser(user: UserListItem): Promise<void> {
    try {
      await this.adminService.activateUser(user.id);
      this.snackBar.open(`User ${user.username} activated`, 'Close', { duration: 3000 });
      await this.loadUsers();
    } catch (err: any) {
      this.snackBar.open(err.message || 'Failed to activate user', 'Close', { duration: 5000 });
    }
  }
}
