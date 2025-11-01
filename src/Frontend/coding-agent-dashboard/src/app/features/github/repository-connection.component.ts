import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NotificationService } from '../../core/services/notifications/notification.service';
import { LoadingStateComponent } from '../../shared/components/loading-state.component';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface Repository {
  id: string;
  owner: string;
  name: string;
  fullName: string;
  isPrivate: boolean;
  defaultBranch: string;
  connectedAt?: string;
}

interface ConnectRepositoryRequest {
  owner: string;
  name: string;
}

@Component({
  selector: 'app-repository-connection',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    LoadingStateComponent
  ],
  template: `
    <div class="repository-container">
      <mat-card>
        <mat-card-header>
          <mat-icon class="header-icon">code</mat-icon>
          <mat-card-title>GitHub Repository Connection</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <!-- Connect Repository Form -->
          <form [formGroup]="connectForm" (ngSubmit)="connectRepository()" class="connect-form">
            <div class="form-row">
              <mat-form-field appearance="outline">
                <mat-label>Repository Owner</mat-label>
                <mat-icon matPrefix>person</mat-icon>
                <input 
                  matInput 
                  formControlName="owner"
                  placeholder="username or organization"
                  [attr.data-testid]="'repo-owner-input'"
                  required>
                <mat-error *ngIf="connectForm.get('owner')?.hasError('required')">
                  Owner is required
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Repository Name</mat-label>
                <mat-icon matPrefix>folder</mat-icon>
                <input 
                  matInput 
                  formControlName="name"
                  placeholder="repository-name"
                  [attr.data-testid]="'repo-name-input'"
                  required>
                <mat-error *ngIf="connectForm.get('name')?.hasError('required')">
                  Repository name is required
                </mat-error>
              </mat-form-field>

              <button 
                mat-raised-button 
                color="primary"
                type="submit"
                [disabled]="connectForm.invalid || connecting()"
                [attr.data-testid]="'connect-repo-button'">
                <mat-spinner *ngIf="connecting()" diameter="20" class="button-spinner"></mat-spinner>
                <mat-icon *ngIf="!connecting()">add</mat-icon>
                <span>{{ connecting() ? 'Connecting...' : 'Connect' }}</span>
              </button>
            </div>
          </form>

          <!-- Error Message -->
          <div class="error-message" *ngIf="errorMessage()">
            <mat-icon>error</mat-icon>
            <span>{{ errorMessage() }}</span>
          </div>

          <!-- Connected Repositories -->
          <div class="repositories-section">
            <h3>Connected Repositories</h3>
            <app-loading-state 
              *ngIf="loading()" 
              mode="spinner" 
              [size]="40"
              message="Loading repositories...">
            </app-loading-state>

            <div *ngIf="!loading() && repositories().length > 0">
              <table mat-table [dataSource]="repositories()" class="repositories-table">
                <ng-container matColumnDef="fullName">
                  <th mat-header-cell *matHeaderCellDef>Repository</th>
                  <td mat-cell *matCellDef="let repo">
                    <div class="repo-info">
                      <mat-icon>code</mat-icon>
                      <a [href]="'https://github.com/' + repo.fullName" target="_blank" [attr.data-testid]="'repo-link'">
                        {{ repo.fullName }}
                      </a>
                      <mat-icon *ngIf="repo.isPrivate" matTooltip="Private repository">lock</mat-icon>
                    </div>
                  </td>
                </ng-container>

                <ng-container matColumnDef="defaultBranch">
                  <th mat-header-cell *matHeaderCellDef>Default Branch</th>
                  <td mat-cell *matCellDef="let repo">{{ repo.defaultBranch }}</td>
                </ng-container>

                <ng-container matColumnDef="connectedAt">
                  <th mat-header-cell *matHeaderCellDef>Connected</th>
                  <td mat-cell *matCellDef="let repo">
                    {{ formatDate(repo.connectedAt) }}
                  </td>
                </ng-container>

                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef>Actions</th>
                  <td mat-cell *matCellDef="let repo">
                    <button 
                      mat-icon-button 
                      (click)="syncRepository(repo.id)"
                      [disabled]="isSyncing(repo.id)"
                      [attr.data-testid]="'sync-repo-button-' + repo.id"
                      matTooltip="Sync metadata">
                      <mat-icon>refresh</mat-icon>
                    </button>
                    <button 
                      mat-icon-button 
                      color="warn"
                      (click)="disconnectRepository(repo.id)"
                      [attr.data-testid]="'disconnect-repo-button-' + repo.id"
                      matTooltip="Disconnect">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
              </table>
            </div>

            <div *ngIf="!loading() && repositories().length === 0" class="empty-state">
              <mat-icon>folder_off</mat-icon>
              <p>No repositories connected</p>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .repository-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 24px;
    }

    .header-icon {
      color: #673ab7;
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .connect-form {
      margin-bottom: 32px;
    }

    .form-row {
      display: flex;
      gap: 16px;
      align-items: flex-start;
    }

    .form-row mat-form-field {
      flex: 1;
    }

    .form-row button {
      margin-top: 8px;
      position: relative;
      min-width: 120px;
    }

    .button-spinner {
      position: absolute;
      left: 50%;
      top: 50%;
      transform: translate(-50%, -50%);
    }

    .error-message {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px;
      margin-bottom: 16px;
      background-color: #ffebee;
      border-radius: 4px;
      color: #c62828;
    }

    .repositories-section {
      margin-top: 32px;
    }

    .repositories-section h3 {
      margin-bottom: 16px;
      font-size: 20px;
      font-weight: 500;
    }

    .repositories-table {
      width: 100%;
    }

    .repo-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .repo-info a {
      color: #673ab7;
      text-decoration: none;
    }

    .repo-info a:hover {
      text-decoration: underline;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      color: rgba(0, 0, 0, 0.54);
    }

    .empty-state mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
    }

    @media (max-width: 768px) {
      .form-row {
        flex-direction: column;
      }

      .repository-container {
        padding: 16px;
      }
    }
  `]
})
export class RepositoryConnectionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private notificationService = inject(NotificationService);
  private baseUrl = environment.apiUrl || 'http://localhost:5000';

  connectForm: FormGroup;
  repositories = signal<Repository[]>([]);
  loading = signal<boolean>(false);
  connecting = signal<boolean>(false);
  syncing = signal<Set<string>>(new Set());
  errorMessage = signal<string | null>(null);

  isSyncing(repoId: string): boolean {
    return this.syncing().has(repoId);
  }

  displayedColumns: string[] = ['fullName', 'defaultBranch', 'connectedAt', 'actions'];

  constructor() {
    this.connectForm = this.fb.group({
      owner: ['', [Validators.required]],
      name: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.loadRepositories();
  }

  private loadRepositories(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.http.get<Repository[]>(`${this.baseUrl}/api/github/repositories`).subscribe({
      next: (repos) => {
        this.repositories.set(repos);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.message || 'Failed to load repositories');
        this.loading.set(false);
      }
    });
  }

  connectRepository(): void {
    if (this.connectForm.invalid || this.connecting()) {
      return;
    }

    this.connecting.set(true);
    this.errorMessage.set(null);

    const request: ConnectRepositoryRequest = {
      owner: this.connectForm.value.owner.trim(),
      name: this.connectForm.value.name.trim()
    };

    this.http.post<Repository>(`${this.baseUrl}/api/github/repositories/connect`, request).subscribe({
      next: (repo) => {
        this.notificationService.success(`Connected to ${repo.fullName}`);
        this.connectForm.reset();
        this.connecting.set(false);
        this.loadRepositories();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message || err.message || 'Failed to connect repository');
        this.connecting.set(false);
      }
    });
  }

  syncRepository(repoId: string): void {
    const syncingSet = this.syncing();
    syncingSet.add(repoId);
    this.syncing.set(new Set(syncingSet));

    this.http.post(`${this.baseUrl}/api/github/repositories/${repoId}/sync`, {}).subscribe({
      next: () => {
        this.notificationService.success('Repository metadata synced successfully');
        syncingSet.delete(repoId);
        this.syncing.set(new Set(syncingSet));
        this.loadRepositories();
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to sync repository');
        syncingSet.delete(repoId);
        this.syncing.set(new Set(syncingSet));
      }
    });
  }

  disconnectRepository(repoId: string): void {
    if (!confirm('Are you sure you want to disconnect this repository?')) {
      return;
    }

    this.http.delete(`${this.baseUrl}/api/github/repositories/${repoId}`).subscribe({
      next: () => {
        this.notificationService.success('Repository disconnected successfully');
        this.loadRepositories();
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to disconnect repository');
      }
    });
  }

  formatDate(dateString?: string): string {
    if (!dateString) return 'N/A';
    try {
      return new Date(dateString).toLocaleDateString();
    } catch {
      return dateString;
    }
  }
}

