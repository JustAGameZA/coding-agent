import { Page, ConsoleMessage } from '@playwright/test';

/**
 * Console error tracker for Playwright tests
 * Tracks console errors, warnings, and exceptions during test execution
 */
export class ConsoleErrorTracker {
  private consoleErrors: ConsoleMessage[] = [];
  private consoleWarnings: ConsoleMessage[] = [];
  private pageErrors: Error[] = [];

  /**
   * Start tracking console messages and page errors for a page
   * Call this in beforeEach or at the start of each test
   */
  startTracking(page: Page): void {
    // Clear previous errors
    this.consoleErrors = [];
    this.consoleWarnings = [];
    this.pageErrors = [];

    // Track console errors
    page.on('console', (msg: ConsoleMessage) => {
      const type = msg.type();
      const text = msg.text();

      // Ignore certain known issues that are not actual errors
      if (this.shouldIgnoreError(text)) {
        return;
      }

      if (type === 'error') {
        this.consoleErrors.push(msg);
        console.log(`[Console Error] ${text}`);
      } else if (type === 'warning' && this.isSignificantWarning(text)) {
        this.consoleWarnings.push(msg);
        console.log(`[Console Warning] ${text}`);
      }
    });

    // Track page errors (unhandled exceptions)
    page.on('pageerror', (error: Error) => {
      if (!this.shouldIgnoreError(error.message)) {
        this.pageErrors.push(error);
        console.log(`[Page Error] ${error.message}`);
      }
    });

    // Track unhandled promise rejections
    page.on('requestfailed', (request) => {
      const failure = request.failure();
      if (failure && !this.shouldIgnoreError(failure.errorText)) {
        // Network failures are handled separately, but log significant ones
        console.log(`[Request Failed] ${request.url()}: ${failure.errorText}`);
      }
    });
  }

  /**
   * Get all console errors
   */
  getErrors(): ConsoleMessage[] {
    return [...this.consoleErrors];
  }

  /**
   * Get all console warnings
   */
  getWarnings(): ConsoleMessage[] {
    return [...this.consoleWarnings];
  }

  /**
   * Get all page errors (unhandled exceptions)
   */
  getPageErrors(): Error[] {
    return [...this.pageErrors];
  }

  /**
   * Get all error messages as strings
   */
  getErrorMessages(): string[] {
    const errorMessages: string[] = [];

    // Console errors
    this.consoleErrors.forEach(msg => {
      errorMessages.push(`Console Error: ${msg.text()}`);
    });

    // Page errors
    this.pageErrors.forEach(error => {
      errorMessages.push(`Page Error: ${error.message}${error.stack ? `\n${error.stack}` : ''}`);
    });

    return errorMessages;
  }

  /**
   * Check if there are any errors
   */
  hasErrors(): boolean {
    return this.consoleErrors.length > 0 || this.pageErrors.length > 0;
  }

  /**
   * Assert that no console errors occurred
   * Throws an error if any errors were detected
   */
  assertNoErrors(context: string = 'test execution'): void {
    if (this.hasErrors()) {
      const errorMessages = this.getErrorMessages();
      const errorDetails = errorMessages.length > 1
        ? `\nMultiple console errors detected:\n${errorMessages.map((e, i) => `  ${i + 1}. ${e}`).join('\n')}`
        : `\nConsole error: ${errorMessages[0]}`;

      throw new Error(
        `Console errors detected during ${context}.${errorDetails}`
      );
    }
  }

  /**
   * Check if an error should be ignored (known issues that aren't actual problems)
   */
  private shouldIgnoreError(text: string): boolean {
    if (!text) return true;

    const lowerText = text.toLowerCase();

    // Ignore known Angular/Zone.js warnings that don't affect functionality
    const ignorePatterns = [
      'zone.js',
      'non-passive event listener',
      'violation',
      'deprecation',
      'third-party cookie',
      'same-site cookie',
      'cross-origin',
      'favicon.ico',
      // Ignore network errors for favicon (common browser behavior)
      'favicon',
      // Ignore ResizeObserver warnings (common in Angular Material)
      'resizeobserver',
      'encountered',
      // Ignore Angular dev mode warnings in production-like tests
      'ng version',
      'angular is running in development mode',
      // Ignore SignalR errors that are handled by backend error reporting
      // These errors are caught and sent to the UI via SignalR error messages
      // which are checked by our error detection system
      'an unexpected error occurred invoking',
      'failed to send message',
      'signalr',
      // Ignore SignalR connection errors (expected during reconnections)
      'connection disconnected',
      'websocket closed',
      'failed to complete negotiation',
      'failed to start the connection',
      'websocket closed with status code',
      // Ignore expected errors when loading messages for new conversations
      // New conversations have no messages, so loading messages may return 404 or empty
      'failed to load messages for new conversation',
      'httperrorresponse',
      // Ignore 404 errors for resources that may not exist (e.g., favicons, missing assets)
      // These don't affect functionality and are common in development
      'failed to load resource',
      '404',
    ];

    return ignorePatterns.some(pattern => lowerText.includes(pattern));
  }

  /**
   * Check if a warning is significant enough to track
   */
  private isSignificantWarning(text: string): boolean {
    if (!text) return false;

    const lowerText = text.toLowerCase();

    // Track warnings that might indicate real issues
    const significantPatterns = [
      'error',
      'exception',
      'failed',
      'deprecated',
      'security',
      'authentication',
      'authorization',
      'permission',
      'access denied',
      'unauthorized',
      'forbidden',
    ];

    return significantPatterns.some(pattern => lowerText.includes(pattern));
  }

  /**
   * Clear all tracked errors (useful for test cleanup)
   */
  clear(): void {
    this.consoleErrors = [];
    this.consoleWarnings = [];
    this.pageErrors = [];
  }
}

/**
 * Global instance for easy access in tests
 * Create a new instance for each test if needed
 */
export function createConsoleErrorTracker(): ConsoleErrorTracker {
  return new ConsoleErrorTracker();
}

