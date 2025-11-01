import { Page, Locator } from '@playwright/test';

/**
 * Task Detail Page Object
 * Encapsulates selectors and actions for the Task Detail page
 */
export class TaskDetailPage {
  readonly page: Page;
  
  readonly taskHeader: Locator;
  readonly taskTitle: Locator;
  readonly taskStatus: Locator;
  readonly executeButton: Locator;
  readonly cancelButton: Locator;
  readonly retryButton: Locator;
  readonly strategyMenuButton: Locator;
  readonly strategyMenu: Locator;
  readonly overviewTab: Locator;
  readonly planningTab: Locator;
  readonly reflectionTab: Locator;
  readonly memoryTab: Locator;
  readonly feedbackTab: Locator;

  constructor(page: Page) {
    this.page = page;
    
    this.taskHeader = page.locator('.task-header');
    this.taskTitle = page.locator('mat-card-title');
    this.taskStatus = page.locator('.status-chip');
    this.executeButton = page.locator('[data-testid="execute-task-button"]');
    this.cancelButton = page.locator('[data-testid="cancel-task-button"]');
    this.retryButton = page.locator('[data-testid="retry-task-button"]');
    this.strategyMenuButton = page.locator('[data-testid="strategy-menu-button"]');
    this.strategyMenu = page.locator('mat-menu');
    this.overviewTab = page.locator('mat-tab:has-text("Overview")');
    this.planningTab = page.locator('mat-tab:has-text("Planning")');
    this.reflectionTab = page.locator('mat-tab:has-text("Reflection")');
    this.memoryTab = page.locator('mat-tab:has-text("Memory Context")');
    this.feedbackTab = page.locator('mat-tab:has-text("Feedback")');
  }
  
  async goto(taskId: string) {
    await this.page.goto(`/tasks/${taskId}`);
  }
  
  async waitForPageLoad() {
    await this.taskHeader.waitFor({ state: 'visible', timeout: 10000 });
  }
  
  async executeTask() {
    await this.executeButton.click();
  }
  
  async executeWithStrategy(strategy: 'SingleShot' | 'Iterative' | 'MultiAgent' | 'HybridExecution') {
    await this.strategyMenuButton.click();
    await this.page.waitForTimeout(300);
    await this.page.locator(`button:has-text("${strategy}")`).click();
  }
  
  async cancelTask() {
    await this.cancelButton.click();
  }
  
  async retryTask() {
    await this.retryButton.click();
  }
  
  async switchToTab(tabName: 'Overview' | 'Planning' | 'Reflection' | 'Memory Context' | 'Feedback') {
    await this.page.locator(`mat-tab:has-text("${tabName}")`).click();
    await this.page.waitForTimeout(300);
  }
  
  async getTaskStatus(): Promise<string | null> {
    return await this.taskStatus.textContent();
  }
  
  async isExecuteButtonVisible(): Promise<boolean> {
    return await this.executeButton.isVisible().catch(() => false);
  }
  
  async isCancelButtonVisible(): Promise<boolean> {
    return await this.cancelButton.isVisible().catch(() => false);
  }
  
  async isRetryButtonVisible(): Promise<boolean> {
    return await this.retryButton.isVisible().catch(() => false);
  }
}

