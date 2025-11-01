import { Page, Locator } from '@playwright/test';

/**
 * Task Create Dialog Page Object
 * Encapsulates selectors and actions for the Task Create Dialog
 */
export class TaskCreateDialogPage {
  readonly page: Page;
  
  readonly dialog: Locator;
  readonly titleInput: Locator;
  readonly descriptionInput: Locator;
  readonly createButton: Locator;
  readonly cancelButton: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    
    this.dialog = page.locator('mat-dialog-container');
    this.titleInput = page.locator('[data-testid="task-title-input"]');
    this.descriptionInput = page.locator('[data-testid="task-description-input"]');
    this.createButton = page.locator('[data-testid="task-create-button"]');
    this.cancelButton = page.locator('button:has-text("Cancel")');
    this.errorMessage = page.locator('.error-message');
  }
  
  async waitForDialog() {
    await this.dialog.waitFor({ state: 'visible', timeout: 5000 });
  }
  
  async fillForm(title: string, description: string) {
    await this.titleInput.fill(title);
    await this.descriptionInput.fill(description);
  }
  
  async submit() {
    await this.createButton.click();
  }
  
  async cancel() {
    await this.cancelButton.click();
  }
  
  async isCreateButtonDisabled(): Promise<boolean> {
    return await this.createButton.isDisabled();
  }
  
  async getErrorMessage(): Promise<string | null> {
    const isVisible = await this.errorMessage.isVisible().catch(() => false);
    if (isVisible) {
      return await this.errorMessage.textContent();
    }
    return null;
  }
}

