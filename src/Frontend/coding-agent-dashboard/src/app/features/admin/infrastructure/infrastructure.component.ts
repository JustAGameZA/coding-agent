import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { InfrastructureLink } from '../../../core/models/admin.models';

/**
 * Infrastructure component displays links to observability and infrastructure tools
 */
@Component({
  selector: 'app-infrastructure',
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="infrastructure-container">
      <h1>Infrastructure & Observability</h1>
      <p class="subtitle">Access monitoring and infrastructure tools</p>

      <div class="infrastructure-grid">
        @for (link of infrastructureLinks(); track link.name) {
          <mat-card class="infrastructure-card">
            <mat-card-header>
              <mat-icon mat-card-avatar [class]="'icon-' + link.icon">{{ link.icon }}</mat-icon>
              <mat-card-title>{{ link.name }}</mat-card-title>
              <mat-card-subtitle>{{ link.description }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <p class="link-url">{{ link.url }}</p>
            </mat-card-content>
            <mat-card-actions>
              <a [href]="link.url" target="_blank" rel="noopener noreferrer" mat-raised-button color="primary">
                <mat-icon>open_in_new</mat-icon>
                Open
              </a>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .infrastructure-container {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
    }

    h1 {
      margin: 0 0 8px 0;
      font-size: 32px;
      font-weight: 400;
    }

    .subtitle {
      margin: 0 0 32px 0;
      color: rgba(0, 0, 0, 0.6);
      font-size: 16px;
    }

    .infrastructure-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 24px;
    }

    .infrastructure-card {
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .infrastructure-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.2);
    }

    mat-card-header {
      margin-bottom: 16px;
    }

    mat-icon[mat-card-avatar] {
      width: 40px;
      height: 40px;
      font-size: 40px;
      line-height: 40px;
    }

    .icon-monitoring { color: #ff6b6b; }
    .icon-search { color: #4ecdc4; }
    .icon-timeline { color: #45b7d1; }
    .icon-analytics { color: #f39c12; }
    .icon-router { color: #9b59b6; }

    .link-url {
      font-family: monospace;
      font-size: 12px;
      color: rgba(0, 0, 0, 0.5);
      margin: 8px 0;
    }

    mat-card-actions {
      padding: 8px 16px 16px;
      margin: 0;
    }

    mat-card-actions a {
      width: 100%;
    }

    mat-card-actions mat-icon {
      margin-right: 8px;
    }
  `]
})
export class InfrastructureComponent {
  protected readonly infrastructureLinks = signal<InfrastructureLink[]>([
    {
      name: 'Grafana',
      url: 'http://localhost:3000',
      icon: 'monitoring',
      description: 'Metrics visualization and dashboards'
    },
    {
      name: 'Seq',
      url: 'http://localhost:5341',
      icon: 'search',
      description: 'Structured logging and log search'
    },
    {
      name: 'Jaeger',
      url: 'http://localhost:16686',
      icon: 'timeline',
      description: 'Distributed tracing and performance'
    },
    {
      name: 'Prometheus',
      url: 'http://localhost:9090',
      icon: 'analytics',
      description: 'Metrics collection and alerting'
    },
    {
      name: 'RabbitMQ',
      url: 'http://localhost:15672',
      icon: 'router',
      description: 'Message broker management'
    }
  ]);
}
