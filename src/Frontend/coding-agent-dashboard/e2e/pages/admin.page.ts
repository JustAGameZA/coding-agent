import { Page, Locator } from '@playwright/test';

/**
 * Infrastructure Page Object
 * Encapsulates selectors and actions for the Admin Infrastructure page
 */
export class InfrastructurePage {
  readonly page: Page;
  
  // Container
  readonly infrastructureContainer: Locator;
  readonly pageTitle: Locator;
  readonly subtitle: Locator;
  
  // Infrastructure cards
  readonly infrastructureGrid: Locator;
  readonly grafanaCard: Locator;
  readonly seqCard: Locator;
  readonly jaegerCard: Locator;
  readonly prometheusCard: Locator;
  readonly rabbitmqCard: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    this.infrastructureContainer = page.locator('.infrastructure-container');
    this.pageTitle = page.locator('h1').filter({ hasText: 'Infrastructure & Observability' });
    this.subtitle = page.locator('.subtitle');
    
    this.infrastructureGrid = page.locator('.infrastructure-grid');
    this.grafanaCard = page.locator('mat-card').filter({ hasText: 'Grafana' });
    this.seqCard = page.locator('mat-card').filter({ hasText: 'Seq' });
    this.jaegerCard = page.locator('mat-card').filter({ hasText: 'Jaeger' });
    this.prometheusCard = page.locator('mat-card').filter({ hasText: 'Prometheus' });
    this.rabbitmqCard = page.locator('mat-card').filter({ hasText: 'RabbitMQ' });
  }
  
  async goto() {
    await this.page.goto('/admin/infrastructure');
    await this.page.waitForLoadState('networkidle');
  }
  
  async getCardLink(cardName: string): Promise<string | null> {
    const card = this.page.locator('mat-card').filter({ hasText: cardName });
    const link = card.locator('a');
    return await link.getAttribute('href');
  }
  
  async getCardCount(): Promise<number> {
    return await this.page.locator('mat-card').count();
  }
  
  async clickCardLink(cardName: string): Promise<void> {
    const card = this.page.locator('mat-card').filter({ hasText: cardName });
    await card.locator('a').click();
  }
}

/**
 * User Management Page Object
 * Encapsulates selectors and actions for the Admin User Management page
 */
export class UserManagementPage {
  readonly page: Page;
  
  // Container
  readonly userListContainer: Locator;
  readonly pageTitle: Locator;
  readonly subtitle: Locator;
  
  // Search toolbar
  readonly searchField: Locator;
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly clearButton: Locator;
  
  // Table
  readonly userTable: Locator;
  readonly tableRows: Locator;
  readonly loadingSpinner: Locator;
  readonly errorContainer: Locator;
  
  // Paginator
  readonly paginator: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    this.userListContainer = page.locator('.user-list-container');
    this.pageTitle = page.locator('h1').filter({ hasText: 'User Management' });
    this.subtitle = page.locator('.subtitle');
    
    this.searchField = page.locator('.search-field');
    this.searchInput = page.locator('input[placeholder="Username or email"]');
    this.searchButton = page.locator('button').filter({ hasText: 'Search' });
    this.clearButton = page.locator('button').filter({ hasText: 'Clear' });
    
    this.userTable = page.locator('.user-table');
    this.tableRows = page.locator('tr.mat-mdc-row');
    this.loadingSpinner = page.locator('mat-spinner');
    this.errorContainer = page.locator('.error-container');
    
    this.paginator = page.locator('mat-paginator');
  }
  
  async goto() {
    await this.page.goto('/admin/users');
    await this.page.waitForLoadState('networkidle');
  }
  
  async waitForTableLoad() {
    // Wait for loading spinner to disappear
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
      // Spinner might not appear if load is instant
    });
    
    // Wait for table to be visible
    await this.userTable.waitFor({ state: 'visible', timeout: 5000 });
  }
  
  async search(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.searchButton.click();
    await this.waitForTableLoad();
  }
  
  async clearSearch(): Promise<void> {
    await this.clearButton.click();
    await this.waitForTableLoad();
  }
  
  async getUserCount(): Promise<number> {
    return await this.tableRows.count();
  }
  
  async getUserByUsername(username: string): Promise<Locator | null> {
    const row = this.tableRows.filter({ hasText: username });
    const count = await row.count();
    return count > 0 ? row.first() : null;
  }
  
  async clickEditRoles(username: string): Promise<void> {
    const row = await this.getUserByUsername(username);
    if (!row) throw new Error(`User ${username} not found`);
    
    const editButton = row.locator('button[matTooltip="Edit roles"]');
    await editButton.click();
  }
  
  async clickActivate(username: string): Promise<void> {
    const row = await this.getUserByUsername(username);
    if (!row) throw new Error(`User ${username} not found`);
    
    const activateButton = row.locator('button[matTooltip="Activate"]');
    await activateButton.click();
  }
  
  async clickDeactivate(username: string): Promise<void> {
    const row = await this.getUserByUsername(username);
    if (!row) throw new Error(`User ${username} not found`);
    
    const deactivateButton = row.locator('button[matTooltip="Deactivate"]');
    await deactivateButton.click();
  }
  
  async getUserStatus(username: string): Promise<'Active' | 'Inactive' | null> {
    const row = await this.getUserByUsername(username);
    if (!row) return null;
    
    const statusChip = row.locator('mat-chip').filter({ hasText: /Active|Inactive/ });
    const text = await statusChip.textContent();
    return text?.trim() as 'Active' | 'Inactive' | null;
  }
  
  async getUserRoles(username: string): Promise<string[]> {
    const row = await this.getUserByUsername(username);
    if (!row) return [];
    
    const roleChips = row.locator('mat-chip-set mat-chip');
    const count = await roleChips.count();
    const roles: string[] = [];
    
    for (let i = 0; i < count; i++) {
      const text = await roleChips.nth(i).textContent();
      if (text) roles.push(text.trim());
    }
    
    return roles;
  }
}

/**
 * User Edit Dialog Page Object
 * Encapsulates selectors and actions for the User Role Edit dialog
 */
export class UserEditDialogPage {
  readonly page: Page;
  
  readonly dialog: Locator;
  readonly dialogTitle: Locator;
  readonly adminCheckbox: Locator;
  readonly userCheckbox: Locator;
  readonly cancelButton: Locator;
  readonly saveButton: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    this.dialog = page.locator('mat-dialog-container');
    this.dialogTitle = this.dialog.locator('h2');
    this.adminCheckbox = this.dialog.locator('mat-checkbox').filter({ hasText: 'Admin' });
    this.userCheckbox = this.dialog.locator('mat-checkbox').filter({ hasText: 'User' });
    this.cancelButton = this.dialog.locator('button').filter({ hasText: 'Cancel' });
    this.saveButton = this.dialog.locator('button').filter({ hasText: 'Save' });
  }
  
  async waitForDialog() {
    await this.dialog.waitFor({ state: 'visible', timeout: 5000 });
  }
  
  async isAdminChecked(): Promise<boolean> {
    const input = this.adminCheckbox.locator('input');
    return await input.isChecked();
  }
  
  async isUserChecked(): Promise<boolean> {
    const input = this.userCheckbox.locator('input');
    return await input.isChecked();
  }
  
  async toggleAdmin(): Promise<void> {
    await this.adminCheckbox.click();
  }
  
  async toggleUser(): Promise<void> {
    await this.userCheckbox.click();
  }
  
  async save(): Promise<void> {
    await this.saveButton.click();
    await this.dialog.waitFor({ state: 'hidden', timeout: 5000 });
  }
  
  async cancel(): Promise<void> {
    await this.cancelButton.click();
    await this.dialog.waitFor({ state: 'hidden', timeout: 5000 });
  }
}
