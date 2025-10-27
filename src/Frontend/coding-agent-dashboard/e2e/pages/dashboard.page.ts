import { Page, Locator } from '@playwright/test';

/**
 * Dashboard Page Object
 * Encapsulates selectors and actions for the Dashboard page
 */
export class DashboardPage {
  readonly page: Page;
  
  // Stats Card Selectors (matching actual component)
  readonly conversationsCard: Locator;
  readonly totalTasksCard: Locator;
  readonly completedTasksCard: Locator;
  readonly activeTasksCard: Locator;
  readonly failedTasksCard: Locator;
  readonly avgDurationCard: Locator;
  
  // Other elements
  readonly pageTitle: Locator;
  readonly lastUpdatedText: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    // Using Material Card structure with data-testid
    this.conversationsCard = page.locator('[data-testid="stat-card-conversations"]');
    this.totalTasksCard = page.locator('[data-testid="stat-card-total"]');
    this.completedTasksCard = page.locator('[data-testid="stat-card-completed"]');
    this.activeTasksCard = page.locator('[data-testid="stat-card-active"]');
    this.failedTasksCard = page.locator('[data-testid="stat-card-failed"]');
    this.avgDurationCard = page.locator('[data-testid="stat-card-duration"]');
    
    this.pageTitle = page.getByRole('heading', { name: /dashboard/i });
    this.lastUpdatedText = page.locator('[data-testid="last-updated"]');
  }
  
  async goto() {
    await this.page.goto('/dashboard');
    await this.page.waitForLoadState('networkidle');
  }
  
  async waitForStatsToLoad() {
    await this.totalTasksCard.waitFor({ state: 'visible' });
    await this.page.waitForTimeout(500); // Wait for data to populate
  }
  
  async getStatValue(cardLocator: Locator): Promise<string> {
    const valueElement = cardLocator.locator('.stat-value, .mat-card-content h2').first();
    return await valueElement.textContent() || '';
  }
  
  async getAllStatCards(): Promise<Locator[]> {
    return [
      this.conversationsCard,
      this.totalTasksCard,
      this.completedTasksCard,
      this.activeTasksCard,
      this.failedTasksCard,
      this.avgDurationCard
    ];
  }
  
  async getLastUpdatedTime(): Promise<string> {
    return await this.lastUpdatedText.textContent() || '';
  }
}
