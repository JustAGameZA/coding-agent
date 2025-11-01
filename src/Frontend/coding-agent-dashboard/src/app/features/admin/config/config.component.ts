import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConfigService } from '../../../core/services/config.service';
import { NotificationService } from '../../../core/services/notifications/notification.service';
import { SystemConfig } from '../../../core/models/config.models';
import { LoadingStateComponent } from '../../../shared/components/loading-state.component';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-config',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
    MatTooltipModule,
    MatSelectModule,
    LoadingStateComponent
  ],
  template: `
    <div class="config-container">
      <div class="header">
        <h1>
          <mat-icon>settings</mat-icon>
          System Configuration
        </h1>
        <p class="subtitle">Manage system settings and feature flags</p>
      </div>

      <app-loading-state 
        *ngIf="loading()" 
        mode="spinner" 
        [size]="60"
        message="Loading configuration...">
      </app-loading-state>

      <div *ngIf="!loading() && config()" class="config-content">
        <mat-tab-group>
          <!-- Feature Flags Tab -->
          <mat-tab label="Feature Flags">
            <div class="tab-content">
              <mat-card>
                <mat-card-header>
                  <mat-icon>flag</mat-icon>
                  <mat-card-title>Feature Flags</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="featureFlagsForm" (ngSubmit)="saveFeatureFlags()">
                    <div class="form-section">
                      <h3>Legacy System</h3>
                      <mat-checkbox formControlName="useLegacyChat" [attr.data-testid]="'feature-legacy-chat'">
                        Use Legacy Chat Service
                        <mat-icon matTooltip="Route chat requests to legacy system" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                      <mat-checkbox formControlName="useLegacyOrchestration" [attr.data-testid]="'feature-legacy-orchestration'">
                        Use Legacy Orchestration Service
                        <mat-icon matTooltip="Route task execution to legacy system" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                      <mat-checkbox formControlName="useLegacyML" [attr.data-testid]="'feature-legacy-ml'">
                        Use Legacy ML Classifier
                        <mat-icon matTooltip="Route classification to legacy system" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                    </div>

                    <mat-divider></mat-divider>

                    <div class="form-section">
                      <h3>Agentic AI Features</h3>
                      <mat-checkbox formControlName="enableAgenticAI" [attr.data-testid]="'feature-agentic-ai'">
                        Enable Agentic AI
                        <mat-icon matTooltip="Enable all agentic AI capabilities" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                      <mat-checkbox formControlName="enableReflection" [attr.data-testid]="'feature-reflection'">
                        Enable Reflection & Self-Correction
                        <mat-icon matTooltip="Enable execution reflection and improvement plans" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                      <mat-checkbox formControlName="enablePlanning" [attr.data-testid]="'feature-planning'">
                        Enable Goal Decomposition & Planning
                        <mat-icon matTooltip="Enable hierarchical planning for complex tasks" class="info-icon">info</mat-icon>
                      </mat-checkbox>
                    </div>

                    <div class="actions">
                      <button 
                        mat-raised-button 
                        color="primary"
                        type="submit"
                        [disabled]="featureFlagsForm.invalid || saving()"
                        [attr.data-testid]="'save-feature-flags-button'">
                        <mat-icon>save</mat-icon>
                        Save Feature Flags
                      </button>
                      <button 
                        mat-stroked-button 
                        type="button"
                        (click)="resetFeatureFlags()"
                        [disabled]="saving()">
                        <mat-icon>refresh</mat-icon>
                        Reset
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>

          <!-- Service Endpoints Tab -->
          <mat-tab label="Service Endpoints">
            <div class="tab-content">
              <mat-card>
                <mat-card-header>
                  <mat-icon>dns</mat-icon>
                  <mat-card-title>Service Endpoints</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="endpointsForm" (ngSubmit)="saveEndpoints()">
                    <mat-form-field appearance="outline" *ngFor="let endpoint of endpointKeys">
                      <mat-label>{{ endpoint | titlecase }}</mat-label>
                      <mat-icon matPrefix>link</mat-icon>
                      <input matInput [formControlName]="endpoint" [attr.data-testid]="'endpoint-' + endpoint">
                    </mat-form-field>

                    <div class="actions">
                      <button 
                        mat-raised-button 
                        color="primary"
                        type="submit"
                        [disabled]="endpointsForm.invalid || saving()"
                        [attr.data-testid]="'save-endpoints-button'">
                        <mat-icon>save</mat-icon>
                        Save Endpoints
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>

          <!-- Rate Limits Tab -->
          <mat-tab label="Rate Limits">
            <div class="tab-content">
              <mat-card>
                <mat-card-header>
                  <mat-icon>speed</mat-icon>
                  <mat-card-title>Rate Limits</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="rateLimitsForm" (ngSubmit)="saveRateLimits()">
                    <div class="form-section">
                      <h3>Per User</h3>
                      <mat-form-field appearance="outline">
                        <mat-label>Requests Per Hour</mat-label>
                        <mat-icon matPrefix>person</mat-icon>
                        <input matInput type="number" formControlName="requestsPerHour" [attr.data-testid]="'rate-limit-user'">
                      </mat-form-field>
                    </div>

                    <div class="form-section">
                      <h3>Per IP Address</h3>
                      <mat-form-field appearance="outline">
                        <mat-label>Requests Per Minute</mat-label>
                        <mat-icon matPrefix>language</mat-icon>
                        <input matInput type="number" formControlName="requestsPerMinute" [attr.data-testid]="'rate-limit-ip'">
                      </mat-form-field>
                    </div>

                    <div class="actions">
                      <button 
                        mat-raised-button 
                        color="primary"
                        type="submit"
                        [disabled]="rateLimitsForm.invalid || saving()"
                        [attr.data-testid]="'save-rate-limits-button'">
                        <mat-icon>save</mat-icon>
                        Save Rate Limits
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>

          <!-- Model Settings Tab -->
          <mat-tab label="Model Settings">
            <div class="tab-content">
              <mat-card>
                <mat-card-header>
                  <mat-icon>psychology</mat-icon>
                  <mat-card-title>AI Model Settings</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="modelSettingsForm" (ngSubmit)="saveModelSettings()">
                    <mat-form-field appearance="outline">
                      <mat-label>Default Strategy</mat-label>
                      <mat-icon matPrefix>play_circle</mat-icon>
                      <mat-select formControlName="defaultStrategy" [attr.data-testid]="'default-strategy'">
                        <mat-option value="SingleShot">Single Shot</mat-option>
                        <mat-option value="Iterative">Iterative</mat-option>
                        <mat-option value="MultiAgent">Multi-Agent</mat-option>
                        <mat-option value="HybridExecution">Hybrid Execution</mat-option>
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Max Parallel Subagents</mat-label>
                      <mat-icon matPrefix>group_work</mat-icon>
                      <input matInput type="number" formControlName="maxParallelSubagents" min="1" max="10" [attr.data-testid]="'max-parallel-subagents'">
                    </mat-form-field>

                    <mat-checkbox formControlName="enableOllama" [attr.data-testid]="'enable-ollama'">
                      Enable Ollama (Local LLM)
                    </mat-checkbox>

                    <mat-form-field appearance="outline" *ngIf="modelSettingsForm.get('enableOllama')?.value">
                      <mat-label>Ollama Base URL</mat-label>
                      <mat-icon matPrefix>http</mat-icon>
                      <input matInput formControlName="ollamaBaseUrl" [attr.data-testid]="'ollama-base-url'">
                    </mat-form-field>

                    <div class="actions">
                      <button 
                        mat-raised-button 
                        color="primary"
                        type="submit"
                        [disabled]="modelSettingsForm.invalid || saving()"
                        [attr.data-testid]="'save-model-settings-button'">
                        <mat-icon>save</mat-icon>
                        Save Model Settings
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>

          <!-- GitHub Integration Tab -->
          <mat-tab label="GitHub Integration">
            <div class="tab-content">
              <mat-card>
                <mat-card-header>
                  <mat-icon>code</mat-icon>
                  <mat-card-title>GitHub Integration</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                  <form [formGroup]="githubForm" (ngSubmit)="saveGitHubConfig()">
                    <mat-checkbox formControlName="enabled" [attr.data-testid]="'github-enabled'">
                      Enable GitHub Integration
                    </mat-checkbox>

                    <mat-form-field appearance="outline" *ngIf="githubForm.get('enabled')?.value">
                      <mat-label>GitHub App ID</mat-label>
                      <mat-icon matPrefix>verified_user</mat-icon>
                      <input matInput formControlName="appId" [attr.data-testid]="'github-app-id'">
                    </mat-form-field>

                    <mat-form-field appearance="outline" *ngIf="githubForm.get('enabled')?.value">
                      <mat-label>Webhook Secret</mat-label>
                      <mat-icon matPrefix>lock</mat-icon>
                      <input matInput type="password" formControlName="webhookSecret" [attr.data-testid]="'github-webhook-secret'">
                      <mat-hint>Keep this secret secure</mat-hint>
                    </mat-form-field>

                    <div class="actions">
                      <button 
                        mat-raised-button 
                        color="primary"
                        type="submit"
                        [disabled]="githubForm.invalid || saving()"
                        [attr.data-testid]="'save-github-button'">
                        <mat-icon>save</mat-icon>
                        Save GitHub Config
                      </button>
                    </div>
                  </form>
                </mat-card-content>
              </mat-card>
            </div>
          </mat-tab>
        </mat-tab-group>
      </div>
    </div>
  `,
  styles: [`
    .config-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    .header {
      margin-bottom: 24px;
    }

    .header h1 {
      display: flex;
      align-items: center;
      gap: 12px;
      margin: 0 0 8px 0;
      font-size: 32px;
      font-weight: 400;
    }

    .header h1 mat-icon {
      color: #673ab7;
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .subtitle {
      margin: 0;
      color: rgba(0, 0, 0, 0.6);
      font-size: 16px;
    }

    .config-content {
      margin-top: 24px;
    }

    .tab-content {
      padding: 24px;
    }

    mat-card {
      margin-bottom: 24px;
    }

    mat-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 16px;
    }

    mat-card-header mat-icon {
      color: #673ab7;
    }

    .form-section {
      margin-bottom: 24px;
    }

    .form-section h3 {
      margin: 0 0 16px 0;
      font-size: 18px;
      font-weight: 500;
      color: #333;
    }

    .form-section mat-checkbox {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 12px;
    }

    .info-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: rgba(0, 0, 0, 0.54);
      cursor: help;
    }

    mat-form-field {
      width: 100%;
      margin-bottom: 16px;
    }

    .actions {
      display: flex;
      gap: 12px;
      margin-top: 24px;
      padding-top: 24px;
      border-top: 1px solid rgba(0, 0, 0, 0.12);
    }

    mat-divider {
      margin: 24px 0;
    }

    @media (max-width: 768px) {
      .config-container {
        padding: 16px;
      }

      .tab-content {
        padding: 16px;
      }
    }
  `]
})
export class ConfigComponent implements OnInit {
  private configService = inject(ConfigService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);

  config = signal<SystemConfig | null>(null);
  loading = signal<boolean>(true);
  saving = signal<boolean>(false);

  featureFlagsForm: FormGroup;
  endpointsForm: FormGroup;
  rateLimitsForm: FormGroup;
  modelSettingsForm: FormGroup;
  githubForm: FormGroup;

  endpointKeys: string[] = ['gateway', 'chat', 'orchestration', 'mlClassifier', 'github', 'browser', 'cicd', 'dashboard', 'ollama'];

  constructor() {
    // Initialize forms with default values
    this.featureFlagsForm = this.fb.group({
      useLegacyChat: [false],
      useLegacyOrchestration: [false],
      useLegacyML: [false],
      enableAgenticAI: [true],
      enableReflection: [true],
      enablePlanning: [true]
    });

    const endpointsGroup: { [key: string]: any } = {};
    this.endpointKeys.forEach(key => {
      endpointsGroup[key] = ['', Validators.required];
    });
    this.endpointsForm = this.fb.group(endpointsGroup);

    this.rateLimitsForm = this.fb.group({
      requestsPerHour: [1000, [Validators.required, Validators.min(1)]],
      requestsPerMinute: [100, [Validators.required, Validators.min(1)]]
    });

    this.modelSettingsForm = this.fb.group({
      defaultStrategy: ['Iterative', Validators.required],
      maxParallelSubagents: [3, [Validators.required, Validators.min(1), Validators.max(10)]],
      enableOllama: [true],
      ollamaBaseUrl: ['http://localhost:11434']
    });

    this.githubForm = this.fb.group({
      enabled: [false],
      appId: [''],
      webhookSecret: ['']
    });

    // Subscribe to enableOllama changes to show/hide URL field
    this.modelSettingsForm.get('enableOllama')?.valueChanges.subscribe(enabled => {
      const urlControl = this.modelSettingsForm.get('ollamaBaseUrl');
      if (enabled) {
        urlControl?.setValidators([Validators.required]);
      } else {
        urlControl?.clearValidators();
      }
      urlControl?.updateValueAndValidity({ emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.loadConfig();
  }

  private loadConfig(): void {
    this.loading.set(true);
    
    this.configService.getConfig().subscribe({
      next: (config) => {
        this.config.set(config);
        this.populateForms(config);
        this.loading.set(false);
      },
      error: (err) => {
        this.notificationService.error('Failed to load configuration');
        console.error('Config load error:', err);
        this.loading.set(false);
      }
    });
  }

  private populateForms(config: SystemConfig): void {
    // Feature Flags
    this.featureFlagsForm.patchValue(config.features);

    // Endpoints
    this.endpointsForm.patchValue(config.serviceEndpoints);

    // Rate Limits
    this.rateLimitsForm.patchValue({
      requestsPerHour: config.rateLimits.perUser.requestsPerHour,
      requestsPerMinute: config.rateLimits.perIP.requestsPerMinute
    });

    // Model Settings
    this.modelSettingsForm.patchValue(config.modelSettings);

    // GitHub
    this.githubForm.patchValue(config.githubIntegration);
  }

  saveFeatureFlags(): void {
    if (this.featureFlagsForm.invalid || this.saving()) return;

    this.saving.set(true);
    const features = this.featureFlagsForm.value;

    this.configService.updateFeatureFlags(features).subscribe({
      next: (updatedFeatures) => {
        this.notificationService.success('Feature flags updated successfully');
        const currentConfig = this.config();
        if (currentConfig) {
          this.config.set({ ...currentConfig, features: updatedFeatures });
        }
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to update feature flags');
        this.saving.set(false);
      }
    });
  }

  saveEndpoints(): void {
    if (this.endpointsForm.invalid || this.saving()) return;

    this.saving.set(true);
    const endpoints = this.endpointsForm.value;

    this.configService.updateConfig({ serviceEndpoints: endpoints }).subscribe({
      next: (config) => {
        this.notificationService.success('Service endpoints updated successfully');
        this.config.set(config);
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to update endpoints');
        this.saving.set(false);
      }
    });
  }

  saveRateLimits(): void {
    if (this.rateLimitsForm.invalid || this.saving()) return;

    this.saving.set(true);
    const rateLimits = {
      perUser: { requestsPerHour: this.rateLimitsForm.value.requestsPerHour },
      perIP: { requestsPerMinute: this.rateLimitsForm.value.requestsPerMinute }
    };

    this.configService.updateConfig({ rateLimits }).subscribe({
      next: (config) => {
        this.notificationService.success('Rate limits updated successfully');
        this.config.set(config);
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to update rate limits');
        this.saving.set(false);
      }
    });
  }

  saveModelSettings(): void {
    if (this.modelSettingsForm.invalid || this.saving()) return;

    this.saving.set(true);
    const modelSettings = this.modelSettingsForm.value;

    this.configService.updateConfig({ modelSettings }).subscribe({
      next: (config) => {
        this.notificationService.success('Model settings updated successfully');
        this.config.set(config);
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to update model settings');
        this.saving.set(false);
      }
    });
  }

  saveGitHubConfig(): void {
    if (this.githubForm.invalid || this.saving()) return;

    this.saving.set(true);
    const githubIntegration = this.githubForm.value;

    this.configService.updateConfig({ githubIntegration }).subscribe({
      next: (config) => {
        this.notificationService.success('GitHub integration updated successfully');
        this.config.set(config);
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.error(err.message || 'Failed to update GitHub config');
        this.saving.set(false);
      }
    });
  }

  resetFeatureFlags(): void {
    const config = this.config();
    if (config) {
      this.featureFlagsForm.patchValue(config.features);
    }
  }
}

