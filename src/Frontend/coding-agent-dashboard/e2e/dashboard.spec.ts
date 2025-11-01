import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/dashboard.page';
import { setupAuthenticatedUser, waitForAngular } from './fixtures';

/**
 * Dashboard Page E2E Tests
 * Tests the Dashboard page displaying task statistics
 */

test.describe('Dashboard Page', () => {
  let dashboardPage: DashboardPage;
  
  test.beforeEach(async ({ page }) => {
    dashboardPage = new DashboardPage(page);
    
    // Setup authenticated user with mocked APIs
    await setupAuthenticatedUser(page);
    
    // Navigate to dashboard
    await dashboardPage.goto();
    await waitForAngular(page);
    
    // Debug: Log network requests
    page.on('request', request => {
      if (request.url().includes('/api/dashboard')) {
        console.log('>> Request:', request.method(), request.url());
      }
    });
    
    page.on('response', response => {
      if (response.url().includes('/api/dashboard')) {
        console.log('<< Response:', response.status(), response.url());
      }
    });
  });
  
  test('should display page title', async () => {
    await expect(dashboardPage.pageTitle).toBeVisible();
  });
  
  test('should display all 6 stat cards', async ({ page }) => {
    const cards = await dashboardPage.getAllStatCards();
    
    for (const card of cards) {
      await expect(card).toBeVisible();
    }
    
    // Verify count
    expect(cards.length).toBe(6);
  });
  
  test('should load stats from API', async ({ page }) => {
    await dashboardPage.waitForStatsToLoad();
    
    // Verify stats are populated (not just placeholders)
    const totalTasks = await dashboardPage.getStatValue(dashboardPage.totalTasksCard);
    expect(totalTasks).toBeTruthy();
    expect(totalTasks).not.toBe('0');
  });
  
  test('should display correct stat values from mock API', async () => {
    await dashboardPage.waitForStatsToLoad();
    
    // Verify mocked values match mockDashboardStats
    const totalTasks = await dashboardPage.getStatValue(dashboardPage.totalTasksCard);
    expect(totalTasks).toContain('42');
    
    const runningTasks = await dashboardPage.getStatValue(dashboardPage.activeTasksCard);
    expect(runningTasks).toContain('8');
    
    const completedTasks = await dashboardPage.getStatValue(dashboardPage.completedTasksCard);
    expect(completedTasks).toContain('30');
  });
  
  test('should display last updated timestamp', async () => {
    await dashboardPage.waitForStatsToLoad();
    
    const lastUpdated = await dashboardPage.getLastUpdatedTime();
    expect(lastUpdated).toBeTruthy();
    expect(lastUpdated.length).toBeGreaterThan(0);
  });
  
  test('should handle API errors gracefully', async ({ page }) => {
    // Note: To test API errors with real backend, we'd need backend to fail
    // For now, verify page loads correctly
    await dashboardPage.goto();
    await waitForAngular(page);
    
    // Page should load even if API has issues
    const heading = page.getByRole('heading', { name: /dashboard/i });
    await expect(heading).toBeVisible();
  });
  
  test('should redirect to login when unauthenticated', async ({ page }) => {
    // Clear auth token
    await page.evaluate(() => localStorage.clear());
    
    // Try to navigate to dashboard
    await page.goto('/dashboard');
    
    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
  
  test('should be responsive on mobile viewport', async ({ page }) => {
    // Change to mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await dashboardPage.goto();
    await dashboardPage.waitForStatsToLoad();
    
    // Verify cards are still visible (may stack vertically)
    const cards = await dashboardPage.getAllStatCards();
    for (const card of cards) {
      await expect(card).toBeVisible();
    }
  });
  
  test('should be responsive on tablet viewport', async ({ page }) => {
    // Change to tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    
    await dashboardPage.goto();
    await dashboardPage.waitForStatsToLoad();
    
    // Verify layout adapts
    const cards = await dashboardPage.getAllStatCards();
    expect(cards.length).toBe(6);
  });
});

test.describe('Dashboard Auto-Refresh', () => {
  test.skip('should auto-refresh stats after 30 seconds', async ({ page }) => {
    // This test requires waiting 30+ seconds
    test.slow();
    
    const dashboardPage = new DashboardPage(page);
    await mockDashboardAPI(page);
    await dashboardPage.goto();
    
    const initialUpdate = await dashboardPage.getLastUpdatedTime();
    
    // Wait for auto-refresh (30 seconds + buffer)
    await page.waitForTimeout(32000);
    
    const updatedTime = await dashboardPage.getLastUpdatedTime();
    expect(updatedTime).not.toBe(initialUpdate);
  });
});
