import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private snack: MatSnackBar) {}

  info(message: string, duration = 2500) {
    this.snack.open(message, 'OK', { duration });
  }

  success(message: string, duration = 3000) {
    this.snack.open(message, 'OK', { duration, panelClass: ['snack-success'] });
  }

  error(message: string, duration = 4000) {
    this.snack.open(message, 'Dismiss', { duration, panelClass: ['snack-error'] });
  }
}
