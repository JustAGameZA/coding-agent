import { ErrorHandler, Injectable } from '@angular/core';
import { NotificationService } from '../services/notifications/notification.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  constructor(private notify: NotificationService) {}

  handleError(error: unknown): void {
    // Basic global error surface; avoid leaking implementation details
    console.error('Global error captured', error);
    this.notify.error('An unexpected error occurred.');
  }
}
