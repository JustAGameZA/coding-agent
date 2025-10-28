import { Page, Locator } from '@playwright/test';

/**
 * Tasks Page Object
 * Encapsulates selectors and actions for the Tasks page
 */
export class TasksPage {
  readonly page: Page;
  
  // Table elements
  readonly table: Locator;
  readonly tableRows: Locator;
  readonly tableHeaders: Locator;
  
  // Pagination
  readonly paginator: Locator;
  readonly nextPageButton: Locator;
  readonly prevPageButton: Locator;
  readonly pageSizeSelect: Locator;
  
  // Empty state
  readonly emptyState: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    this.table = page.locator('[data-testid="tasks-table"]');
    this.tableRows = page.locator('[data-testid="task-row"]');
    this.tableHeaders = page.locator('thead th, mat-header-row mat-header-cell');
    
    this.paginator = page.locator('[data-testid="tasks-paginator"]');
    this.nextPageButton = page.locator('button[aria-label="Next page"]');
    this.prevPageButton = page.locator('button[aria-label="Previous page"]');
    this.pageSizeSelect = page.locator('mat-select[aria-label*="Items per page"]');
    
    this.emptyState = page.getByText(/no tasks found/i);
  }
  
  async goto() {
    await this.page.goto('/tasks');
    await this.page.waitForLoadState('networkidle');
  }
  
  async waitForTableToLoad() {
    await this.table.waitFor({ state: 'visible' });
    await this.page.waitForTimeout(500);
  }
  
  async getRowCount(): Promise<number> {
    return await this.tableRows.count();
  }
  
  async getColumnHeaders(): Promise<string[]> {
    const headers = await this.tableHeaders.allTextContents();
    return headers.map(h => h.trim());
  }
  
  async getRowData(rowIndex: number): Promise<Record<string, string>> {
    const row = this.tableRows.nth(rowIndex);
    const cells = row.locator('td, mat-cell');
    const cellCount = await cells.count();
    
    const data: Record<string, string> = {};
    for (let i = 0; i < cellCount; i++) {
      const cellText = await cells.nth(i).textContent();
      data[`col${i}`] = cellText?.trim() || '';
    }
    
    return data;
  }
  
  async getStatusChip(rowIndex: number): Promise<Locator> {
    const row = this.tableRows.nth(rowIndex);
    return row.locator('[data-testid="task-status"]');
  }
  
  async getPRLink(rowIndex: number): Promise<string | null> {
    const row = this.tableRows.nth(rowIndex);
    const link = row.locator('[data-testid="task-pr-link"]');
    
    if (await link.count() > 0) {
      return await link.getAttribute('href');
    }
    return null;
  }
  
  async goToNextPage() {
    await this.nextPageButton.click();
    await this.page.waitForTimeout(500);
  }
  
  async goToPrevPage() {
    await this.prevPageButton.click();
    await this.page.waitForTimeout(500);
  }
  
  async changePageSize(size: number) {
    await this.pageSizeSelect.click();
    await this.page.locator(`mat-option[value="${size}"]`).click();
    await this.page.waitForTimeout(500);
  }
  
  async isEmptyStateVisible(): Promise<boolean> {
    return await this.emptyState.isVisible();
  }
}
