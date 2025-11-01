import { Page } from '@playwright/test';
import { setupConsoleErrorTracking } from '../fixtures';
import type { ConsoleErrorTracker } from './console-error-tracker';

/**
 * Global console error tracking for all tests
 * Use this in beforeEach/afterEach hooks to automatically track and check console errors
 */

let consoleTracker: ConsoleErrorTracker | null = null;

/**
 * Setup console error tracking at the start of a test
 * Call this in beforeEach
 */
export function setupTestConsoleErrors(page: Page): void {
  consoleTracker = setupConsoleErrorTracking(page);
}

/**
 * Assert no console errors occurred during the test
 * Call this in afterEach
 */
export function assertTestConsoleErrors(context: string = 'test execution'): void {
  if (consoleTracker) {
    consoleTracker.assertNoErrors(context);
  }
}

/**
 * Clear console error tracker (useful for cleanup)
 */
export function clearConsoleErrorTracker(): void {
  if (consoleTracker) {
    consoleTracker.clear();
    consoleTracker = null;
  }
}

/**
 * Get console errors without throwing
 * Useful for debugging
 */
export function getTestConsoleErrors(): string[] {
  return consoleTracker?.getErrorMessages() || [];
}

