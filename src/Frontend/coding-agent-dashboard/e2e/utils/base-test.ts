import { Page, expect } from '@playwright/test';
import { assertNoErrors, detectErrors } from './error-detection';
import { ConsoleErrorTracker, createConsoleErrorTracker } from './console-error-tracker';

/**
 * Base test helper class
 * Provides common functionality for E2E tests including error detection
 */
export class BaseTestHelper {
  private consoleErrorTracker: ConsoleErrorTracker;

  constructor(protected page: Page) {
    this.consoleErrorTracker = createConsoleErrorTracker();
    // Automatically start tracking console errors
    this.consoleErrorTracker.startTracking(page);
  }

  /**
   * Gets all text content from the page for error detection
   * Override this in subclasses to provide page-specific content extraction
   */
  protected async getAllPageContent(): Promise<string[]> {
    // Default implementation: get all text content from body
    return await this.page.evaluate(() => {
      const body = document.body;
      const walker = document.createTreeWalker(
        body,
        NodeFilter.SHOW_TEXT,
        null,
        // @ts-ignore
        false
      );

      const texts: string[] = [];
      let node;
      while ((node = walker.nextNode())) {
        const text = node.textContent?.trim();
        if (text && text.length > 0) {
          texts.push(text);
        }
      }
      return texts;
    });
  }

  /**
   * Asserts that no errors are present on the page (including console errors)
   * @param context Description of the operation being tested
   */
  async assertNoErrors(context: string = 'operation'): Promise<void> {
    // Check for console errors first
    this.consoleErrorTracker.assertNoErrors(context);
    
    // Also check for page-level errors (UI errors)
    await assertNoErrors(
      () => this.getAllPageContent(),
      this.page,
      context
    );
  }

  /**
   * Asserts that no console errors occurred
   * @param context Description of the operation being tested
   */
  assertNoConsoleErrors(context: string = 'test execution'): void {
    this.consoleErrorTracker.assertNoErrors(context);
  }

  /**
   * Get console errors without throwing
   * @returns Array of console error messages
   */
  getConsoleErrors(): string[] {
    return this.consoleErrorTracker.getErrorMessages();
  }

  /**
   * Check if any console errors occurred
   * @returns true if console errors were detected
   */
  hasConsoleErrors(): boolean {
    return this.consoleErrorTracker.hasErrors();
  }

  /**
   * Detects errors on the page without throwing
   * @param durationMs Duration to check for errors
   * @param intervalMs Interval between checks
   * @returns Error detection result
   */
  async detectErrors(
    durationMs: number = 15000,
    intervalMs: number = 1000
  ): Promise<{ hasError: boolean; errorMessage: string; allErrors: string[] }> {
    return await detectErrors(
      () => this.getAllPageContent(),
      this.page,
      durationMs,
      intervalMs
    );
  }

  /**
   * Waits for a notification/snackbar message
   */
  async waitForNotification(
    message?: string,
    timeout: number = 5000
  ): Promise<void> {
    const snackbar = this.page.locator('mat-snack-bar-container');
    await snackbar.waitFor({ state: 'visible', timeout });

    if (message) {
      await expect(snackbar).toContainText(message);
    }
  }

  /**
   * Checks if an error notification is visible
   */
  async hasErrorNotification(): Promise<boolean> {
    const snackbar = this.page.locator('mat-snack-bar-container');
    const count = await snackbar.count();
    if (count === 0) return false;

    for (let i = 0; i < count; i++) {
      const text = await snackbar.nth(i).textContent();
      if (text && (
        text.includes('Error') ||
        text.includes('error') ||
        text.includes('Failed') ||
        text.includes('failed') ||
        text.includes('Exception') ||
        text.includes('exception')
      )) {
        return true;
      }
    }
    return false;
  }

  /**
   * Asserts that no error notifications are visible
   */
  async assertNoErrorNotifications(): Promise<void> {
    const hasError = await this.hasErrorNotification();
    if (hasError) {
      const snackbar = this.page.locator('mat-snack-bar-container');
      const errorText = await snackbar.first().textContent();
      throw new Error(`Error notification detected: ${errorText}`);
    }
  }

  /**
   * Waits for page to be stable (network idle, Angular ready)
   */
  async waitForPageStable(timeout: number = 10000): Promise<void> {
    await this.page.waitForLoadState('networkidle', { timeout });
    await this.page.waitForTimeout(500);
  }

  /**
   * Takes a screenshot with error context
   */
  async takeScreenshot(name: string): Promise<void> {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const filename = `test-results/${name}-${timestamp}.png`;
    await this.page.screenshot({ path: filename, fullPage: true });
  }
}

/**
 * Test wrapper that automatically checks for errors after each test step
 */
export function withErrorDetection<T extends any[]>(
  testFn: (page: Page, ...args: T) => Promise<void>
): (page: Page, ...args: T) => Promise<void> {
  return async (page: Page, ...args: T) => {
    const helper = new BaseTestHelper(page);
    
    try {
      await testFn(page, ...args);
      
      // Final error check after test completes (includes console errors)
      await helper.assertNoErrors('test completion');
    } catch (error: any) {
      // Check for console errors even if test fails
      if (helper.hasConsoleErrors()) {
        const consoleErrors = helper.getConsoleErrors();
        const consoleErrorDetails = consoleErrors.length > 1
          ? `\nConsole errors detected:\n${consoleErrors.map((e, i) => `  ${i + 1}. ${e}`).join('\n')}`
          : `\nConsole error: ${consoleErrors[0]}`;
        
        throw new Error(
          `${error.message}\n\nAdditionally, console errors were detected during test execution.${consoleErrorDetails}`
        );
      }
      
      // Check for page-level errors even if test fails
      const errorResult = await helper.detectErrors(5000, 500);
      if (errorResult.hasError && !error.message.includes('Database or processing error') && !error.message.includes('Console error')) {
        throw new Error(
          `${error.message}\n\nAdditionally, database/processing errors were detected: ${errorResult.errorMessage}`
        );
      }
      throw error;
    }
  };
}

