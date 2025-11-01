import { Page } from '@playwright/test';
import { BaseTestHelper } from './base-test';

/**
 * Helper function to wrap test operations with automatic error detection
 * Use this to ensure errors are detected after any operation
 */
export async function withErrorCheck<T>(
  page: Page,
  operation: () => Promise<T>,
  context: string = 'operation'
): Promise<T> {
  const helper = new BaseTestHelper(page);
  
  try {
    const result = await operation();
    
    // Check for errors after operation
    await helper.assertNoErrors(context);
    
    return result;
  } catch (error: any) {
    // Check for errors even if operation fails
    const errorResult = await helper.detectErrors(5000, 500);
    if (errorResult.hasError && !error.message?.includes('Database or processing error')) {
      throw new Error(
        `${error.message}\n\nAdditionally, database/processing errors were detected: ${errorResult.errorMessage}`
      );
    }
    throw error;
  }
}

/**
 * Test helper that adds error detection to common operations
 */
export class TestWithErrors extends BaseTestHelper {
  /**
   * Click with error detection
   */
  async click(selector: string, context?: string): Promise<void> {
    await withErrorCheck(
      this.page,
      async () => {
        const element = this.page.locator(selector);
        await element.click();
      },
      context || `click on ${selector}`
    );
  }

  /**
   * Fill input with error detection
   */
  async fill(selector: string, value: string, context?: string): Promise<void> {
    await withErrorCheck(
      this.page,
      async () => {
        const input = this.page.locator(selector);
        await input.fill(value);
      },
      context || `fill ${selector}`
    );
  }

  /**
   * Submit form with error detection
   */
  async submitForm(selector: string = 'form', context?: string): Promise<void> {
    await withErrorCheck(
      this.page,
      async () => {
        const form = this.page.locator(selector);
        await form.locator('button[type="submit"]').click();
      },
      context || 'form submission'
    );
  }

  /**
   * Wait for API call with error detection
   */
  async waitForAPI(
    urlPattern: string | RegExp,
    context?: string
  ): Promise<void> {
    await withErrorCheck(
      this.page,
      async () => {
        await this.page.waitForResponse(
          response => {
            const url = response.url();
            if (typeof urlPattern === 'string') {
              return url.includes(urlPattern);
            }
            return urlPattern.test(url);
          },
          { timeout: 10000 }
        );
      },
      context || 'API call'
    );
  }
}


