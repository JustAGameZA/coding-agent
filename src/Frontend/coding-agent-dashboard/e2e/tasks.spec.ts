import { test, expect } from '@playwright/test';
import { TasksPage } from './pages/tasks.page';
import { setupAuthenticatedUser, mockTasks, waitForAngular } from './fixtures';

/**
 * Tasks Page E2E Tests
 * Tests the Tasks page with table, pagination, and filtering
 */

test.describe('Tasks Page', () => {
  let tasksPage: TasksPage;
  
  test.beforeEach(async ({ page }) => {
    tasksPage = new TasksPage(page);
    
    // Setup authenticated user with mocked APIs
    await setupAuthenticatedUser(page);
    
    // Navigate to tasks
    await tasksPage.goto();
    await waitForAngular(page);
  });
  
  test('should display tasks table', async () => {
    await tasksPage.waitForTableToLoad();
    
    await expect(tasksPage.table).toBeVisible();
  });
  
  test('should display table headers', async () => {
    await tasksPage.waitForTableToLoad();
    
    const headers = await tasksPage.getColumnHeaders();
    expect(headers.length).toBeGreaterThan(0);
    
    // Verify essential columns exist
    const headerText = headers.join(' ').toLowerCase();
    expect(headerText).toContain('title');
    expect(headerText).toContain('status');
  });
  
  test('should load tasks from API', async () => {
    await tasksPage.waitForTableToLoad();
    
    const rowCount = await tasksPage.getRowCount();
    expect(rowCount).toBe(mockTasks.length);
  });
  
  test('should display task data correctly', async () => {
    await tasksPage.waitForTableToLoad();
    
    // Get first row data
    const firstRow = await tasksPage.getRowData(0);
    expect(firstRow).toBeTruthy();
    
    // Should contain task information
    const rowText = Object.values(firstRow).join(' ');
    expect(rowText.length).toBeGreaterThan(0);
  });
  
  test('should display status chips with colors', async () => {
    await tasksPage.waitForTableToLoad();
    
    // Get status chip for first task
    const statusChip = await tasksPage.getStatusChip(0);
    await expect(statusChip).toBeVisible();
  });
  
  test('should display PR links for completed tasks', async () => {
    await tasksPage.waitForTableToLoad();
    
    // First mock task has a PR URL - component uses internal route format
    const prLink = await tasksPage.getPRLink(0);
    expect(prLink).toBeTruthy();
    expect(prLink).toMatch(/#\/pr\/\d+/);
  });
  
  test('should not display PR links for tasks without PRs', async () => {
    await tasksPage.waitForTableToLoad();
    
    // Third mock task has no PR
    const prLink = await tasksPage.getPRLink(2);
    expect(prLink).toBeNull();
  });
  
  test('should handle empty state when no tasks', async ({ page }) => {
    // Mock empty response - return array directly
    await page.route('**/api/dashboard/tasks*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]) // Empty array, not paginated object
      });
    });
    
    const emptyTasksPage = new TasksPage(page);
    await emptyTasksPage.goto();
    await waitForAngular(page);
    await page.waitForTimeout(1000); // Wait for Angular to process empty state
    
    // Should show empty state message
    const emptyState = page.locator('.empty-state');
    await expect(emptyState).toBeVisible();
    expect(await emptyState.textContent()).toContain('No tasks found');
  });
});

test.describe('Tasks Pagination', () => {
  test.skip('should display paginator', async ({ page }) => {
    // This test requires more mock data
    const tasksPage = new TasksPage(page);
    await mockTasksAPI(page);
    await tasksPage.goto();
    await tasksPage.waitForTableToLoad();
    
    await expect(tasksPage.paginator).toBeVisible();
  });
  
  test.skip('should navigate to next page', async ({ page }) => {
    // This test requires pagination setup
    const tasksPage = new TasksPage(page);
    await mockTasksAPI(page);
    await tasksPage.goto();
    await tasksPage.waitForTableToLoad();
    
    const initialRowCount = await tasksPage.getRowCount();
    
    await tasksPage.goToNextPage();
    
    // Verify page changed (implementation specific)
    const newRowCount = await tasksPage.getRowCount();
    expect(newRowCount).toBeGreaterThanOrEqual(0);
  });
});

test.describe('Tasks Responsive Layout', () => {
  test('should display table on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const tasksPage = new TasksPage(page);
    await mockTasksAPI(page);
    await tasksPage.goto();
    await tasksPage.waitForTableToLoad();
    
    await expect(tasksPage.table).toBeVisible();
  });
  
  test('should be scrollable on small screens', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const tasksPage = new TasksPage(page);
    await setupAuthenticatedUser(page);
    await tasksPage.goto();
    await tasksPage.waitForTableToLoad();
    
    // Table should be present (may scroll horizontally)
    const rowCount = await tasksPage.getRowCount();
    expect(rowCount).toBeGreaterThan(0);
  });
});

test.describe('Tasks Page - Auth', () => {
  test('should redirect to login when unauthenticated', async ({ page }) => {
    // Clear auth token
    await page.evaluate(() => localStorage.clear());
    
    // Try to navigate to tasks
    await page.goto('/tasks');
    
    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
});
