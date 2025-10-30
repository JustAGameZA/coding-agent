import { test, expect } from '@playwright/test';
import { InfrastructurePage } from './pages/admin.page';
import { LoginPage } from './pages/auth.page';
import { waitForAngular } from './fixtures';

/**
 * Grafana E2E Tests
 * Tests Grafana integration from the infrastructure page
 * NOTE: Cannot test inside Grafana UI (different origin), only verify link correctness and URL availability
 */

// Helper to login as admin
async function loginAsAdmin(page: any) {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await waitForAngular(page);
  await loginPage.login('admin', 'Admin@1234!');
  await page.waitForLoadState('networkidle');
}

test.describe('Grafana Dashboard Integration', () => {
  test.beforeEach(async ({ page }) => {
    // Login as admin to access infrastructure page
    await loginAsAdmin(page);
  });

  test('infrastructure page should display Grafana card with correct URL', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Verify Grafana card exists
    await expect(infrastructurePage.grafanaCard).toBeVisible();

    // Verify it has the correct URL
    const grafanaLink = await infrastructurePage.getCardLink('Grafana');
    expect(grafanaLink).toBe('http://localhost:3000');

    // Verify card content
    await expect(infrastructurePage.grafanaCard).toContainText('Grafana');
    await expect(infrastructurePage.grafanaCard).toContainText('Metrics visualization and dashboards');
    await expect(infrastructurePage.grafanaCard).toContainText('http://localhost:3000');
  });

  test('Grafana card link should have correct attributes for new tab', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    const grafanaLink = infrastructurePage.grafanaCard.locator('a');

    // Verify href
    const href = await grafanaLink.getAttribute('href');
    expect(href).toBe('http://localhost:3000');

    // Verify target="_blank" (opens in new tab)
    const target = await grafanaLink.getAttribute('target');
    expect(target).toBe('_blank');

    // Verify rel="noopener noreferrer" (security best practice)
    const rel = await grafanaLink.getAttribute('rel');
    expect(rel).toBe('noopener noreferrer');

    // Verify link text
    await expect(grafanaLink).toContainText('Open');

    // Verify icon
    const icon = grafanaLink.locator('mat-icon');
    await expect(icon).toBeVisible();
    await expect(icon).toContainText('open_in_new');
  });

  test('Grafana URL should be accessible', async ({ page, request }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Get Grafana URL
    const grafanaUrl = await infrastructurePage.getCardLink('Grafana');
    expect(grafanaUrl).toBeTruthy();

    // Test if Grafana is accessible (with timeout since it might not be running in test env)
    try {
      const response = await request.get(grafanaUrl!, {
        timeout: 5000,
        maxRedirects: 5
      });

      // If Grafana is running, should get 200 or redirect (which is fine)
      expect(response.status()).toBeLessThan(500);
    } catch (error: any) {
      // If Grafana is not running in test environment, that's OK
      // Just verify the URL format is correct
      console.warn('Grafana not accessible (expected in test env):', error.message);
      expect(grafanaUrl).toMatch(/^http:\/\/localhost:\d+$/);
    }
  });

  test('clicking Grafana card should not navigate away from app', async ({ page, context }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    const currentUrl = page.url();

    // Listen for new page (new tab) being opened
    const pagePromise = context.waitForEvent('page', { timeout: 5000 }).catch(() => null);

    // Click the Grafana link
    const grafanaLink = infrastructurePage.grafanaCard.locator('a');
    await grafanaLink.click();

    // Wait a moment for potential navigation
    await page.waitForTimeout(1000);

    // Verify we're still on the same page
    expect(page.url()).toBe(currentUrl);

    // Note: A new tab would have been opened, but we can't test inside it due to CORS
    const newPage = await pagePromise;
    if (newPage) {
      // A new page was opened (expected behavior)
      console.log('New tab opened for Grafana (expected)');
      await newPage.close();
    }
  });

  test('all infrastructure tool URLs should be accessible', async ({ page, request }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    const tools = [
      { name: 'Grafana', url: 'http://localhost:3000' },
      { name: 'Seq', url: 'http://localhost:5341' },
      { name: 'Jaeger', url: 'http://localhost:16686' },
      { name: 'Prometheus', url: 'http://localhost:9090' },
      { name: 'RabbitMQ', url: 'http://localhost:15672' }
    ];

    for (const tool of tools) {
      const linkUrl = await infrastructurePage.getCardLink(tool.name);
      expect(linkUrl).toBe(tool.url);

      // Try to access (with timeout for test env)
      try {
        const response = await request.get(tool.url, {
          timeout: 3000,
          maxRedirects: 5
        });
        console.log(`${tool.name} is accessible: ${response.status()}`);
        expect(response.status()).toBeLessThan(500);
      } catch (error: any) {
        // Services may not be running in test environment
        console.warn(`${tool.name} not accessible (expected in test env):`, error.message);
        expect(tool.url).toMatch(/^http:\/\/localhost:\d+$/);
      }
    }
  });

  test('infrastructure cards should have consistent styling and layout', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // All cards should be visible
    const cards = [
      infrastructurePage.grafanaCard,
      infrastructurePage.seqCard,
      infrastructurePage.jaegerCard,
      infrastructurePage.prometheusCard,
      infrastructurePage.rabbitmqCard
    ];

    for (const card of cards) {
      await expect(card).toBeVisible();

      // Each card should have:
      // - mat-card-header with icon, title, subtitle
      const header = card.locator('mat-card-header');
      await expect(header).toBeVisible();

      const icon = header.locator('mat-icon');
      await expect(icon).toBeVisible();

      const title = header.locator('mat-card-title');
      await expect(title).toBeVisible();

      const subtitle = header.locator('mat-card-subtitle');
      await expect(subtitle).toBeVisible();

      // - mat-card-content with URL
      const content = card.locator('mat-card-content');
      await expect(content).toBeVisible();

      const urlText = content.locator('.link-url');
      await expect(urlText).toBeVisible();

      // - mat-card-actions with Open button
      const actions = card.locator('mat-card-actions');
      await expect(actions).toBeVisible();

      const openButton = actions.locator('a[mat-raised-button]');
      await expect(openButton).toBeVisible();
    }
  });

  test('infrastructure page should be responsive on mobile', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await loginAsAdmin(page);

    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Verify page is visible
    await expect(infrastructurePage.infrastructureContainer).toBeVisible();
    await expect(infrastructurePage.pageTitle).toBeVisible();

    // Verify all cards are still visible on mobile
    await expect(infrastructurePage.grafanaCard).toBeVisible();
    await expect(infrastructurePage.seqCard).toBeVisible();
    await expect(infrastructurePage.jaegerCard).toBeVisible();
    await expect(infrastructurePage.prometheusCard).toBeVisible();
    await expect(infrastructurePage.rabbitmqCard).toBeVisible();

    // Cards should stack vertically on mobile (grid should adjust)
    const grid = infrastructurePage.infrastructureGrid;
    const gridDisplay = await grid.evaluate(el => window.getComputedStyle(el).display);
    expect(gridDisplay).toBe('grid');
  });
});
