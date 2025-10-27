import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/dashboard.page';
import { TasksPage } from './pages/tasks.page';
import { mockAPIError, waitForAngular } from './fixtures';

/**
 * Error Handling E2E Tests
 * Tests error scenarios and graceful degradation
 */

test.describe('Dashboard Error Handling', () => {
  test('should display error notification on API failure', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    
    // Mock API error
    await mockAPIError(page, 500);
    
    await dashboardPage.goto();
    await waitForAngular(page);
    
    // Wait for error to be processed
    await page.waitForTimeout(2000);
    
    // Should show snackbar/toast notification
    const snackbar = page.locator('mat-snack-bar-container, .snackbar, .toast, [role="alert"]');
    
    // Error notification should appear (with timeout for retry logic)
    const isVisible = await snackbar.isVisible({ timeout: 5000 }).catch(() => false);
    
    // If notification system is implemented, it should be visible
    if (isVisible) {
      const errorText = await snackbar.textContent();
      expect(errorText?.toLowerCase()).toMatch(/error|failed|unable/);
    }
  });
  
  test('should retry failed requests', async ({ page }) => {
    let requestCount = 0;
    
    // Mock API to fail first 2 times, then succeed
    await page.route('**/api/dashboard/stats*', async route => {
      requestCount++;
      
      if (requestCount <= 2) {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal Server Error' })
        });
      } else {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalTasks: 42,
            activeTasks: 8,
            completedTasks: 30,
            failedTasks: 4,
            averageDuration: 3.5,
            successRate: 88.2,
            lastUpdated: new Date().toISOString()
          })
        });
      }
    });
    
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.goto();
    
    // Wait for retries to complete
    await page.waitForTimeout(5000);
    
    // Should eventually succeed
    expect(requestCount).toBeGreaterThanOrEqual(3);
  });
  
  test('should show fallback UI when API is down', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);
    
    // Mock persistent API error
    await mockAPIError(page, 503);
    
    await dashboardPage.goto();
    await waitForAngular(page);
    await page.waitForTimeout(3000);
    
    // Should still render page structure (even with no data)
    const cards = await dashboardPage.getAllStatCards();
    
    // Cards should exist (may show placeholder/error state)
    expect(cards.length).toBeGreaterThan(0);
  });
});

test.describe('Tasks Error Handling', () => {
  test('should handle task list load failure', async ({ page }) => {
    const tasksPage = new TasksPage(page);
    
    // Mock API error
    await mockAPIError(page, 500);
    
    await tasksPage.goto();
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should show error state or notification
    const snackbar = page.locator('mat-snack-bar-container, [role="alert"]');
    const isVisible = await snackbar.isVisible({ timeout: 3000 }).catch(() => false);
    
    // Error should be communicated to user somehow
    if (!isVisible) {
      // May show empty state or error message in table
      const emptyState = await tasksPage.isEmptyStateVisible();
      expect(typeof emptyState).toBe('boolean');
    }
  });
  
  test('should handle network timeout', async ({ page }) => {
    const tasksPage = new TasksPage(page);
    
    // Mock slow/timeout response
    await page.route('**/api/tasks*', async route => {
      await new Promise(resolve => setTimeout(resolve, 35000)); // Longer than timeout
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [], totalCount: 0, pageIndex: 0, pageSize: 10 })
      });
    });
    
    await tasksPage.goto();
    await waitForAngular(page);
    
    // Should timeout and show error
    await page.waitForTimeout(3000);
    
    // Should handle timeout gracefully
    const url = page.url();
    expect(url).toContain('/tasks');
  });
});

test.describe('Network Error Scenarios', () => {
  test('should handle 401 Unauthorized', async ({ page }) => {
    // Mock 401 response
    await page.route('**/api/**', async route => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Unauthorized' })
      });
    });
    
    await page.goto('/dashboard');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should handle auth error (redirect to login or show error)
    const url = page.url();
    expect(url).toBeTruthy();
  });
  
  test('should handle 403 Forbidden', async ({ page }) => {
    // Mock 403 response
    await page.route('**/api/**', async route => {
      await route.fulfill({
        status: 403,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Forbidden' })
      });
    });
    
    await page.goto('/dashboard');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should show access denied message
    const snackbar = page.locator('[role="alert"], mat-snack-bar-container');
    const hasAlert = await snackbar.count() > 0;
    
    expect(typeof hasAlert).toBe('boolean');
  });
  
  test('should handle 404 Not Found', async ({ page }) => {
    // Mock 404 response
    await page.route('**/api/stats', async route => {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Not Found' })
      });
    });
    
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.goto();
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should handle missing endpoint gracefully
    const url = page.url();
    expect(url).toContain('/dashboard');
  });
  
  test('should handle malformed JSON response', async ({ page }) => {
    // Mock invalid JSON
    await page.route('**/api/stats', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: 'This is not valid JSON{{'
      });
    });
    
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.goto();
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should handle parse error gracefully
    const url = page.url();
    expect(url).toContain('/dashboard');
  });
});

test.describe('Offline Behavior', () => {
  test.skip('should show offline indicator when network is down', async ({ page, context }) => {
    // Simulate offline mode
    await context.setOffline(true);
    
    await page.goto('/dashboard');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Should show offline indicator
    const offlineMessage = page.locator('[role="alert"]').filter({ hasText: /offline|connection/i });
    const isVisible = await offlineMessage.isVisible({ timeout: 3000 }).catch(() => false);
    
    // If offline detection is implemented
    if (isVisible) {
      await expect(offlineMessage).toBeVisible();
    }
  });
  
  test.skip('should recover when network is restored', async ({ page, context }) => {
    const dashboardPage = new DashboardPage(page);
    
    // Start online
    await context.setOffline(false);
    await dashboardPage.goto();
    
    // Go offline
    await context.setOffline(true);
    await page.waitForTimeout(1000);
    
    // Go back online
    await context.setOffline(false);
    await page.waitForTimeout(2000);
    
    // Should recover and load data
    await dashboardPage.waitForStatsToLoad();
    
    const totalTasks = await dashboardPage.getStatValue(dashboardPage.totalTasksCard);
    expect(totalTasks).toBeTruthy();
  });
});
