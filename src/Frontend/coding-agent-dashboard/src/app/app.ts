import { Component, signal, inject, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatMenuModule
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);

  protected readonly title = signal('Coding Agent Dashboard');
  protected readonly version = signal('v2.0.0');
  protected readonly sidenavOpened = signal(true);
  
  // Authentication state
  protected readonly isAuthenticated = computed(() => this.authService.isAuthenticated());
  protected readonly currentUser = this.authService.currentUser;
  protected readonly isAdmin = computed(() => {
    const user = this.authService.currentUser();
    return user?.roles?.includes('Admin') ?? false;
  });

  toggleSidenav(): void {
    this.sidenavOpened.update(value => !value);
  }

  logout(): void {
    this.authService.logout();
  }
}
