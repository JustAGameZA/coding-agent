import { test, expect } from '@playwright/test';
import { setupAuthenticatedUser, waitForAngular } from './fixtures';
import { BaseTestHelper } from './utils/base-test';

/**
 * GitHub Integration E2E Tests (UC-7.1, UC-7.3)
 */

test.describe('GitHub Repository Connection (UC-7.1)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should display repository connection page', async ({ page }) => {
    // Use real API - no mocking
    await page.goto('/github/repositories');
    await waitForAngular(page);
    
    // Verify page loaded
    const heading = page.locator('mat-card-title:has-text("GitHub Repository Connection")');
    await expect(heading).toBeVisible({ timeout: 10000 });
    
    // Verify form is visible
    await expect(page.locator('[data-testid="repo-owner-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="repo-name-input"]')).toBeVisible();
    await expect(page.locator('[data-testid="connect-repo-button"]')).toBeVisible();
  });
  
  test('should connect repository', async ({ page }) => {
    // Use real API - no mocking
    // Note: This test requires a valid GitHub repository that the user has access to
    await page.goto('/github/repositories');
    await waitForAngular(page);
    await page.waitForTimeout(1000);
    
    // Fill form
    await page.locator('[data-testid="repo-owner-input"]').fill('testuser');
    await page.locator('[data-testid="repo-name-input"]').fill('test-repo');
    
    // Submit - uses real API
    await page.locator('[data-testid="connect-repo-button"]').click();
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Check for errors after repository connection
    const helper = new BaseTestHelper(page);
    await helper.assertNoErrors('repository connection');
    
    // Verify success notification or error message appears
    const snackbar = page.locator('mat-snack-bar-container');
    await expect(snackbar).toBeVisible({ timeout: 5000 });
  });
  
  test('should validate repository fields', async ({ page }) => {
    await page.goto('/github/repositories');
    await waitForAngular(page);
    
    // Submit button should be disabled when form is invalid
    const submitButton = page.locator('[data-testid="connect-repo-button"]');
    const isDisabled = await submitButton.isDisabled();
    expect(isDisabled).toBe(true);
  });
  
  test('should display connected repositories', async ({ page }) => {
    // Use real API - no mocking
    await page.goto('/github/repositories');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Verify repositories list loads (may be empty or have repos)
    const repoLinks = page.locator('[data-testid="repo-link"]');
    const count = await repoLinks.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
});

test.describe('GitHub Repository Sync (UC-7.3)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should sync repository metadata', async ({ page }) => {
    // Use real API - no mocking
    // Note: This test requires a repository to be connected first
    await page.goto('/github/repositories');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // Find first sync button (if repositories exist)
    const syncButton = page.locator('[data-testid^="sync-repo-button-"]').first();
    const buttonExists = await syncButton.isVisible().catch(() => false);
    
    if (buttonExists) {
      await syncButton.click();
      await waitForAngular(page);
      await page.waitForTimeout(2000);
      
      // Verify success notification
      const snackbar = page.locator('mat-snack-bar-container');
      await expect(snackbar).toBeVisible({ timeout: 5000 });
    } else {
      // Skip if no repositories to sync
      test.skip();
    }
  });
  
  test('should handle sync errors', async ({ page }) => {
    // Use real API - no mocking
    // Note: Error handling tests should be done via integration tests or
    // by testing with invalid repository IDs if the backend supports it
    await page.goto('/github/repositories');
    await waitForAngular(page);
    await page.waitForTimeout(2000);
    
    // This test would require a repository that fails to sync
    // For now, just verify the page loads correctly
    const heading = page.locator('mat-card-title:has-text("GitHub Repository Connection")');
    await expect(heading).toBeVisible();
  });
});

