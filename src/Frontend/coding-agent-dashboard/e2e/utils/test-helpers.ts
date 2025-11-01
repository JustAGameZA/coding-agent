import { Page, expect } from '@playwright/test';

/**
 * Test Helpers
 * Utility functions for E2E tests
 */

/**
 * Wait for Angular to be ready
 */
export async function waitForAngular(page: Page, timeout = 5000): Promise<void> {
  try {
    await page.waitForFunction(() => {
      return typeof (window as any).ng !== 'undefined' || 
             typeof (window as any).getAllAngularTestabilities === 'function';
    }, { timeout });
  } catch {
    // Angular might not be available in all environments, continue anyway
  }
  await page.waitForLoadState('networkidle', { timeout: 10000 });
  await page.waitForTimeout(500);
}

/**
 * Wait for an API call to complete
 */
export async function waitForAPI(
  page: Page,
  urlPattern: string | RegExp,
  timeout = 10000
): Promise<void> {
  await page.waitForResponse(
    response => {
      const url = response.url();
      if (typeof urlPattern === 'string') {
        return url.includes(urlPattern);
      }
      return urlPattern.test(url);
    },
    { timeout }
  );
}

/**
 * Wait for notification/toast message
 */
export async function waitForNotification(
  page: Page,
  message?: string,
  timeout = 5000
): Promise<void> {
  const snackbar = page.locator('mat-snack-bar-container');
  await snackbar.waitFor({ state: 'visible', timeout });
  
  if (message) {
    await expect(snackbar).toContainText(message);
  }
}

/**
 * Clear all cookies and local storage
 */
export async function clearStorage(page: Page): Promise<void> {
  await page.context().clearCookies();
  await page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
}

/**
 * Mock API response
 */
export async function mockAPIResponse(
  page: Page,
  url: string | RegExp,
  response: {
    status?: number;
    body: any;
    headers?: Record<string, string>;
  }
): Promise<void> {
  await page.route(
    url,
    async route => {
      await route.fulfill({
        status: response.status || 200,
        contentType: 'application/json',
        headers: {
          'Content-Type': 'application/json',
          ...response.headers
        },
        body: typeof response.body === 'string' 
          ? response.body 
          : JSON.stringify(response.body)
      });
    }
  );
}

/**
 * Setup authenticated session
 * Note: Use setupAuthenticatedUser from fixtures.ts instead for full API mocking
 */
export async function setupAuthenticatedSession(page: Page): Promise<void> {
  // Set auth token in localStorage
  await page.evaluate(() => {
    const token = 'mock-jwt-token-' + Date.now();
    localStorage.setItem('auth_token', token);
    
    // Mock user data
    const userData = {
      id: 'test-user-id',
      username: 'testuser',
      email: 'test@example.com',
      roles: ['User']
    };
    localStorage.setItem('user', JSON.stringify(userData));
  });
  
  await page.goto('/');
  await waitForAngular(page);
}

/**
 * Setup admin session
 * Note: Use setupAdminSession from fixtures.ts instead for full API mocking
 */
export async function setupAdminSession(page: Page): Promise<void> {
  await page.evaluate(() => {
    const token = 'mock-admin-token-' + Date.now();
    localStorage.setItem('auth_token', token);
    
    const userData = {
      id: 'admin-user-id',
      username: 'admin',
      email: 'admin@example.com',
      roles: ['Admin', 'User']
    };
    localStorage.setItem('user', JSON.stringify(userData));
  });
  
  await page.goto('/');
  await waitForAngular(page);
}

/**
 * Wait for element to be stable (not animating)
 */
export async function waitForStable(
  page: Page,
  selector: string,
  timeout = 5000
): Promise<void> {
  const element = page.locator(selector);
  let previousBbox = await element.boundingBox();
  
  await page.waitForTimeout(500);
  
  const checkStable = async (attempts = 0): Promise<void> => {
    const currentBbox = await element.boundingBox();
    
    if (
      previousBbox &&
      currentBbox &&
      previousBbox.x === currentBbox.x &&
      previousBbox.y === currentBbox.y &&
      previousBbox.width === currentBbox.width &&
      previousBbox.height === currentBbox.height
    ) {
      return;
    }
    
    if (attempts * 100 > timeout) {
      return; // Timeout reached, continue anyway
    }
    
    previousBbox = currentBbox;
    await page.waitForTimeout(100);
    return checkStable(attempts + 1);
  };
  
  await checkStable();
}

/**
 * Take screenshot with timestamp
 */
export async function takeScreenshot(
  page: Page,
  name: string
): Promise<void> {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const filename = `${name}-${timestamp}.png`;
  await page.screenshot({ path: filename, fullPage: true });
}

/**
 * Fill form with data
 */
export async function fillForm(
  page: Page,
  formData: Record<string, string>
): Promise<void> {
  for (const [key, value] of Object.entries(formData)) {
    const input = page.locator(`[formControlName="${key}"], [name="${key}"], [data-testid="${key}"]`).first();
    await input.fill(value);
    await page.waitForTimeout(100);
  }
}

/**
 * Get text content safely
 */
export async function getTextContent(
  page: Page,
  selector: string
): Promise<string | null> {
  const element = page.locator(selector);
  const count = await element.count();
  if (count === 0) return null;
  return await element.textContent();
}

/**
 * Wait for network idle with timeout
 */
export async function waitForNetworkIdle(
  page: Page,
  timeout = 10000
): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout });
}

/**
 * Scroll element into view
 */
export async function scrollIntoView(
  page: Page,
  selector: string
): Promise<void> {
  const element = page.locator(selector);
  await element.scrollIntoViewIfNeeded();
}

/**
 * Check if element is in viewport
 */
export async function isInViewport(
  page: Page,
  selector: string
): Promise<boolean> {
  return await page.evaluate((sel) => {
    const element = document.querySelector(sel);
    if (!element) return false;
    
    const rect = element.getBoundingClientRect();
    return (
      rect.top >= 0 &&
      rect.left >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
      rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  }, selector);
}

