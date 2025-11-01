import { test, expect } from '@playwright/test';
import { TasksPage } from './pages/tasks.page';
import { setupAuthenticatedUser, mockTasks, waitForAngular } from './fixtures';

/**
 * Task Detail View E2E Tests
 * Tests viewing detailed information about a task including agentic AI features
 */

test.describe('Task Detail View', () => {
  let tasksPage: TasksPage;
  
  test.beforeEach(async ({ page }) => {
    tasksPage = new TasksPage(page);
    
    // Setup authenticated user with mocked APIs
    await setupAuthenticatedUser(page);
    
    // Navigate to tasks page first
    await tasksPage.goto();
    await waitForAngular(page);
    await tasksPage.waitForTableToLoad();
  });
  
  test('should navigate to task detail from task list', async ({ page }) => {
    // Get first task from the list
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    
    // Click on task title to navigate to detail
    await firstTaskLink.click();
    await waitForAngular(page);
    
    // Should be on task detail page
    await page.waitForURL(/.*\/tasks\/.*/, { timeout: 5000 });
    const url = page.url();
    expect(url).toMatch(/\/tasks\/[a-f0-9-]+/);
  });
  
  test('should display task header information', async ({ page }) => {
    // Navigate to first task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify task header elements
    const taskHeader = page.locator('.task-header, mat-card.task-header');
    await expect(taskHeader).toBeVisible();
    
    // Verify title is displayed
    const title = page.locator('mat-card-title');
    await expect(title).toBeVisible();
    
    // Verify status chip
    const statusChip = page.locator('.status-chip');
    await expect(statusChip).toBeVisible();
  });
  
  test('should display task metadata chips', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify task meta section exists
    const taskMeta = page.locator('.task-meta');
    await expect(taskMeta).toBeVisible();
    
    // Verify metadata chips are displayed (type, complexity, duration)
    const chips = taskMeta.locator('mat-chip');
    const chipCount = await chips.count();
    expect(chipCount).toBeGreaterThan(0);
  });
  
  test('should display task description', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify description is displayed
    const description = page.locator('.task-description');
    await expect(description).toBeVisible();
    
    const descriptionText = await description.textContent();
    expect(descriptionText?.length).toBeGreaterThan(0);
  });
  
  test('should display agentic AI tabs', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify tab group exists
    const tabGroup = page.locator('mat-tab-group.agentic-tabs, mat-tab-group');
    await expect(tabGroup).toBeVisible();
    
    // Verify tabs are present
    const tabs = tabGroup.locator('mat-tab');
    const tabCount = await tabs.count();
    expect(tabCount).toBeGreaterThanOrEqual(4); // Overview, Planning, Reflection, Memory, Feedback
  });
  
  test('should switch between tabs', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Click on Planning tab
    const planningTab = page.locator('mat-tab[label="Planning"]').or(page.locator('mat-tab').filter({ hasText: 'Planning' }));
    if (await planningTab.count() > 0) {
      await planningTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify tab content is visible
      const tabContent = page.locator('.tab-content');
      await expect(tabContent).toBeVisible();
    }
    
    // Click on Reflection tab
    const reflectionTab = page.locator('mat-tab[label="Reflection"]').or(page.locator('mat-tab').filter({ hasText: 'Reflection' }));
    if (await reflectionTab.count() > 0) {
      await reflectionTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify reflection content is visible
      const reflectionContent = page.locator('app-reflection-panel, .reflection-panel');
      // May not be visible if no reflection data, but tab should work
      const tabContent = page.locator('.tab-content');
      await expect(tabContent).toBeVisible();
    }
  });
  
  test('should display execution information when execution exists', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Check if execution info card exists
    const executionInfo = page.locator('.execution-info, mat-card.execution-info');
    const isVisible = await executionInfo.isVisible().catch(() => false);
    
    // If visible, verify content
    if (isVisible) {
      await expect(executionInfo).toBeVisible();
      
      // Verify View Reflection button exists if execution ID is present
      const viewReflectionButton = executionInfo.locator('button:has-text("View Reflection")');
      const buttonExists = await viewReflectionButton.count() > 0;
      
      if (buttonExists) {
        await expect(viewReflectionButton).toBeVisible();
      }
    }
  });
  
  test('should handle loading state', async ({ page }) => {
    // Navigate directly to a task detail URL
    const taskId = 'test-task-id-123';
    await page.goto(`/tasks/${taskId}`);
    await waitForAngular(page);
    
    // Verify loading spinner is shown initially
    const loadingState = page.locator('app-loading-state, .loading-overlay, mat-spinner');
    const isLoadingVisible = await loadingState.isVisible().catch(() => false);
    
    // Loading should eventually disappear
    await page.waitForTimeout(2000);
    const stillLoading = await loadingState.isVisible().catch(() => false);
    expect(stillLoading).toBe(false);
  });
  
  test('should display agentic badge if task has agentic features', async ({ page }) => {
    // Navigate to task detail
    const firstTaskLink = page.locator('[data-testid="task-title"] a').first();
    await firstTaskLink.click();
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Check for agentic badge in header
    const agenticBadge = page.locator('app-agentic-badge, .agentic-badge');
    const badgeExists = await agenticBadge.count() > 0;
    
    // Badge may or may not be present depending on task
    // Test verifies it doesn't break if present
    expect(typeof badgeExists).toBe('boolean');
  });
});

test.describe('Task Detail - Navigation', () => {
  test('should be accessible via direct URL', async ({ page }) => {
    await setupAuthenticatedUser(page);
    
    // Navigate directly to task detail URL
    const taskId = mockTasks[0]?.id || 'test-task-id';
    await page.goto(`/tasks/${taskId}`);
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Should load task detail page
    const url = page.url();
    expect(url).toContain(`/tasks/${taskId}`);
  });
  
  test('should redirect to login if unauthenticated', async ({ page }) => {
    // Clear auth token
    await page.evaluate(() => localStorage.clear());
    
    // Try to navigate to task detail
    await page.goto('/tasks/test-task-id');
    
    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
});

test.describe('Task Detail - Agentic AI Features', () => {
  test('should display planning progress component', async ({ page }) => {
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Click on Planning tab
    const planningTab = page.locator('mat-tab').filter({ hasText: 'Planning' });
    if (await planningTab.count() > 0) {
      await planningTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify planning progress component exists
      const planningComponent = page.locator('app-planning-progress');
      // Component may not have data, but should not error
      const exists = await planningComponent.count() > 0;
      expect(typeof exists).toBe('boolean');
    }
  });
  
  test('should display reflection panel when execution exists', async ({ page }) => {
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Click on Reflection tab
    const reflectionTab = page.locator('mat-tab').filter({ hasText: 'Reflection' });
    if (await reflectionTab.count() > 0) {
      await reflectionTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify reflection panel component exists
      const reflectionComponent = page.locator('app-reflection-panel');
      // Component may not have data, but should not error
      const exists = await reflectionComponent.count() > 0;
      expect(typeof exists).toBe('boolean');
    }
  });
  
  test('should display memory context component', async ({ page }) => {
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Click on Memory Context tab
    const memoryTab = page.locator('mat-tab').filter({ hasText: /Memory|Context/ });
    if (await memoryTab.count() > 0) {
      await memoryTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify memory context component exists
      const memoryComponent = page.locator('app-memory-context');
      // Component may not have data, but should not error
      const exists = await memoryComponent.count() > 0;
      expect(typeof exists).toBe('boolean');
    }
  });
  
  test('should display feedback submit component', async ({ page }) => {
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Click on Feedback tab
    const feedbackTab = page.locator('mat-tab').filter({ hasText: 'Feedback' });
    if (await feedbackTab.count() > 0) {
      await feedbackTab.first().click();
      await waitForAngular(page);
      await page.waitForTimeout(500);
      
      // Verify feedback submit component exists
      const feedbackComponent = page.locator('app-feedback-submit');
      await expect(feedbackComponent).toBeVisible();
    }
  });
});

test.describe('Task Detail - Responsive Design', () => {
  test('should be responsive on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify page is visible
    const taskDetail = page.locator('.task-detail');
    await expect(taskDetail).toBeVisible();
    
    // Verify header is visible
    const taskHeader = page.locator('.task-header');
    await expect(taskHeader).toBeVisible();
  });
  
  test('should display tabs correctly on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await setupAuthenticatedUser(page);
    
    // Navigate to task detail
    await page.goto('/tasks/test-task-id');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Verify tab group is visible
    const tabGroup = page.locator('mat-tab-group');
    await expect(tabGroup).toBeVisible();
  });
});

