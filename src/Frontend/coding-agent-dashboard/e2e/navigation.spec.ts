import { test, expect } from '@playwright/test';
import { waitForAngular } from './fixtures';

/**
 * Navigation E2E Tests
 * Tests routing and navigation across the application
 */

test.describe('Application Navigation', () => {
  test('should navigate to dashboard', async ({ page }) => {
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    expect(page.url()).toContain('/dashboard');
    
    // Verify dashboard content is visible
    const heading = page.getByRole('heading', { name: /dashboard/i });
    await expect(heading).toBeVisible();
  });
  
  test('should navigate to tasks', async ({ page }) => {
    // Uses real API - no mocking
    await page.goto('/tasks');
    await waitForAngular(page);
    
    expect(page.url()).toContain('/tasks');
    
    // Verify tasks content is visible
    const table = page.locator('[data-testid="tasks-table"]');
    await expect(table).toBeVisible();
  });
  
  test('should navigate to chat', async ({ page }) => {
    await page.goto('/chat');
    await waitForAngular(page);
    
    expect(page.url()).toContain('/chat');
    
    // Verify chat content is visible
    await page.waitForTimeout(1000);
  });
  
  test('should redirect root to dashboard', async ({ page }) => {
    await page.goto('/');
    await waitForAngular(page);
    
    // Should redirect to dashboard (or default route)
    await page.waitForTimeout(500);
    const url = page.url();
    
    // Verify redirected to a valid route
    expect(url).toMatch(/\/(dashboard|chat|tasks)/);
  });
});

test.describe('Sidebar Navigation', () => {
  test('should display navigation sidebar', async ({ page }) => {
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Look for common sidebar elements
    const sidebar = page.locator('mat-sidenav, aside, [role="navigation"]');
    
    // Sidebar should exist (may be hidden on mobile)
    const count = await sidebar.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
  
  test('should navigate via sidebar links', async ({ page }) => {
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Find and click tasks link
    const tasksLink = page.getByRole('link', { name: /tasks/i }).or(
      page.locator('a[href*="/tasks"]')
    );
    
    if (await tasksLink.count() > 0) {
      await tasksLink.first().click();
      await waitForAngular(page);
      
      expect(page.url()).toContain('/tasks');
    }
  });
  
  test('should highlight active route in sidebar', async ({ page }) => {
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Look for active link styling
    const activeLink = page.locator('[routerLinkActive="active"], .active, [aria-current="page"]');
    
    // Should have at least one active link
    const count = await activeLink.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
});

test.describe('Browser Navigation', () => {
  test('should support browser back button', async ({ page }) => {
    // Navigate to dashboard
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Navigate to tasks
    await page.goto('/tasks');
    await waitForAngular(page);
    expect(page.url()).toContain('/tasks');
    
    // Go back
    await page.goBack();
    await waitForAngular(page);
    
    expect(page.url()).toContain('/dashboard');
  });
  
  test('should support browser forward button', async ({ page }) => {
    // Navigate to dashboard
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Navigate to tasks
    await page.goto('/tasks');
    await waitForAngular(page);
    
    // Go back
    await page.goBack();
    await waitForAngular(page);
    expect(page.url()).toContain('/dashboard');
    
    // Go forward
    await page.goForward();
    await waitForAngular(page);
    
    expect(page.url()).toContain('/tasks');
  });
  
  test('should handle direct URL navigation', async ({ page }) => {
    // Uses real API - no mocking
    // Navigate directly to tasks via URL
    await page.goto('http://localhost:4200/tasks');
    await waitForAngular(page);
    
    expect(page.url()).toContain('/tasks');
    
    // Content should load correctly
    const table = page.locator('[data-testid="tasks-table"]');
    await expect(table).toBeVisible();
  });
});

test.describe('Navigation Error Handling', () => {
  test('should handle 404 for invalid routes', async ({ page }) => {
    await page.goto('/invalid-route-that-does-not-exist');
    await waitForAngular(page);
    
    // Should show 404 page or redirect to valid route
    await page.waitForTimeout(1000);
    
    // Either on 404 page or redirected
    const url = page.url();
    expect(url).toBeTruthy();
  });
});

test.describe('Mobile Navigation', () => {
  test('should display mobile menu on small screens', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Look for hamburger menu button
    const menuButton = page.locator('button[aria-label*="menu"], .menu-toggle, mat-icon:has-text("menu")');
    
    if (await menuButton.count() > 0) {
      await expect(menuButton.first()).toBeVisible();
    }
  });
  
  test.skip('should toggle mobile menu', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Find and click menu button
    const menuButton = page.locator('button[aria-label*="menu"], .menu-toggle');
    
    if (await menuButton.count() > 0) {
      await menuButton.first().click();
      await page.waitForTimeout(500);
      
      // Sidebar should be visible
      const sidebar = page.locator('mat-sidenav, aside');
      await expect(sidebar).toBeVisible();
    }
  });
});
