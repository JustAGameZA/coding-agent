import { Page } from '@playwright/test';

/**
 * Error detection patterns that indicate database or processing errors
 */
const ERROR_PATTERNS = [
  '❌',
  'Error',
  'error',
  'exception',
  'database',
  'failed',
  'Failed',
  'Exception',
  'Database'
];

/**
 * Checks if a message contains an error pattern
 */
export function isErrorMessage(message: string): boolean {
  if (!message || typeof message !== 'string') return false;
  const lowerMessage = message.toLowerCase();
  return ERROR_PATTERNS.some(pattern => message.includes(pattern) || lowerMessage.includes(pattern.toLowerCase()));
}

/**
 * Extracts error messages from an array of messages
 */
export function extractErrorMessages(messages: string[]): string[] {
  return messages.filter(msg => isErrorMessage(msg));
}

/**
 * Checks if a page contains error messages by looking at common error elements
 */
export async function checkForPageErrors(page: Page): Promise<string[]> {
  const errors: string[] = [];

  // Check for error notifications/toasts
  const errorNotifications = page.locator(
    '[role="alert"]:has-text("error"), ' +
    '[role="alert"]:has-text("Error"), ' +
    '.error, .error-message, ' +
    '[class*="error"], ' +
    '[class*="Error"], ' +
    'mat-error, ' +
    '.mat-error'
  );
  const notificationCount = await errorNotifications.count();
  for (let i = 0; i < notificationCount; i++) {
    const text = await errorNotifications.nth(i).textContent();
    if (text && isErrorMessage(text)) {
      errors.push(text);
    }
  }

  // Check console for errors
  const consoleMessages = await page.evaluate(() => {
    // @ts-ignore - accessing window.console for error checking
    return window.consoleErrors || [];
  }).catch(() => []);

  if (Array.isArray(consoleMessages)) {
    errors.push(...consoleMessages.filter(isErrorMessage));
  }

  return errors;
}

/**
 * Continuously checks for error messages in the UI for a specified duration
 * @param getMessages Function that returns all messages from the page
 * @param page Playwright Page instance
 * @param durationMs Duration to check (default: 15000ms)
 * @param intervalMs Interval between checks (default: 1000ms)
 * @returns Object with hasError boolean and errorMessage string
 */
export async function detectErrors(
  getMessages: () => Promise<string[]>,
  page: Page,
  durationMs: number = 15000,
  intervalMs: number = 1000
): Promise<{ hasError: boolean; errorMessage: string; allErrors: string[] }> {
  const iterations = Math.ceil(durationMs / intervalMs);
  let allErrors: string[] = [];

  for (let i = 0; i < iterations; i++) {
    // Get messages from the page
    const messages = await getMessages().catch(() => []);
    const errorMsgs = extractErrorMessages(messages);
    
    if (errorMsgs.length > 0) {
      allErrors.push(...errorMsgs);
    }

    // Also check page-level errors
    const pageErrors = await checkForPageErrors(page);
    if (pageErrors.length > 0) {
      allErrors.push(...pageErrors);
    }

    // If we found errors, stop checking
    if (allErrors.length > 0) {
      break;
    }

    // Wait before next check
    if (i < iterations - 1) {
      await page.waitForTimeout(intervalMs);
    }
  }

  const uniqueErrors = [...new Set(allErrors)];
  const hasError = uniqueErrors.length > 0;
  const errorMessage = uniqueErrors[0] || '';

  return { hasError, errorMessage, allErrors: uniqueErrors };
}

/**
 * Assert helper that throws an error if database or processing errors are detected
 */
export async function assertNoErrors(
  getMessages: () => Promise<string[]>,
  page: Page,
  context: string = 'operation'
): Promise<void> {
  const result = await detectErrors(getMessages, page);
  
  if (result.hasError) {
    const errorDetails = result.allErrors.length > 1
      ? `\nMultiple errors detected:\n${result.allErrors.map((e, i) => `  ${i + 1}. ${e}`).join('\n')}`
      : `\nError: ${result.errorMessage}`;
    
    throw new Error(
      `Database or processing error occurred during ${context} and was sent to client.${errorDetails}`
    );
  }
}

/**
 * Waits for a specific error message to appear
 */
export async function waitForErrorMessage(
  getMessages: () => Promise<string[]>,
  page: Page,
  expectedError: string | RegExp,
  timeoutMs: number = 10000
): Promise<boolean> {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeoutMs) {
    const messages = await getMessages().catch(() => []);
    const errorMsgs = extractErrorMessages(messages);
    
    const found = errorMsgs.some(msg => {
      if (typeof expectedError === 'string') {
        return msg.includes(expectedError);
      } else {
        return expectedError.test(msg);
      }
    });
    
    if (found) {
      return true;
    }
    
    await page.waitForTimeout(500);
  }
  
  return false;
}


