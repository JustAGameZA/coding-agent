import { Page, Locator } from '@playwright/test';

/**
 * Chat Page Object
 * Encapsulates selectors and actions for the Chat page
 */
export class ChatPage {
  readonly page: Page;
  
  // Conversation list
  readonly conversationList: Locator;
  readonly conversationItems: Locator;
  
  // Chat thread
  readonly messageThread: Locator;
  readonly messages: Locator;
  
  // Message input
  readonly messageInput: Locator;
  readonly sendButton: Locator;
  readonly attachFileButton: Locator;
  readonly fileInput: Locator;
  
  // Status indicators
  readonly connectionStatus: Locator;
  readonly typingIndicator: Locator;
  readonly onlineCount: Locator;
  
  // File upload
  readonly uploadProgress: Locator;
  readonly uploadedThumbnail: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    // Conversation list
    this.conversationList = page.locator('[data-testid="conversation-list"]');
    this.conversationItems = page.locator('[data-testid="conversation-item"]');
    
    // Chat thread
    this.messageThread = page.locator('[data-testid="chat-thread"]');
    this.messages = page.locator('.message, .message-bubble');
    
    // Input area
    this.messageInput = page.locator('textarea[placeholder*="message"], input[placeholder*="message"]');
    this.sendButton = page.getByRole('button', { name: /send/i });
    this.attachFileButton = page.getByRole('button', { name: /attach/i });
    this.fileInput = page.locator('input[type="file"]');
    
    // Status
    this.connectionStatus = page.locator('[data-testid="connection-status"]');
    this.typingIndicator = page.locator('[data-testid="typing-indicator"]');
    this.onlineCount = page.locator('[data-testid="online-count"]');
    
    // File upload
    this.uploadProgress = page.locator('mat-progress-bar');
    this.uploadedThumbnail = page.locator('.attachment-thumbnail, img[alt*="attachment"]');
  }
  
  async goto() {
    await this.page.goto('/chat');
    await this.page.waitForLoadState('networkidle');
  }
  
  async waitForConversationsToLoad() {
    await this.conversationList.waitFor({ state: 'visible' });
    await this.page.waitForTimeout(500);
  }
  
  async getConversationCount(): Promise<number> {
    // Wait for loading spinner to disappear
    const spinner = this.page.locator('app-conversation-list mat-spinner');
    if (await spinner.isVisible().catch(() => false)) {
      await spinner.waitFor({ state: 'hidden', timeout: 5000 });
    }
    
    // Wait for mat-nav-list to appear (means loading complete)
    await this.page.locator('app-conversation-list mat-nav-list').waitFor({ timeout: 5000 });
    
    return await this.conversationItems.count();
  }
  
  async selectConversation(index: number) {
    await this.conversationItems.nth(index).click();
    await this.page.waitForTimeout(500);
  }
  
  async selectConversationByTitle(title: string) {
    const conversation = this.conversationItems.filter({ hasText: title });
    await conversation.click();
    await this.page.waitForTimeout(500);
  }
  
  async getMessageCount(): Promise<number> {
    return await this.messages.count();
  }
  
  async getLastMessage(): Promise<string> {
    const lastMsg = this.messages.last();
    return await lastMsg.textContent() || '';
  }
  
  async sendMessage(text: string) {
    await this.messageInput.fill(text);
    await this.sendButton.click();
    await this.page.waitForTimeout(500);
  }
  
  async uploadFile(filePath: string) {
    await this.fileInput.setInputFiles(filePath);
    await this.page.waitForTimeout(1000); // Wait for upload
  }
  
  async isUploadProgressVisible(): Promise<boolean> {
    return await this.uploadProgress.isVisible();
  }
  
  async waitForUploadComplete() {
    await this.uploadProgress.waitFor({ state: 'hidden', timeout: 10000 });
  }
  
  async getConnectionStatus(): Promise<string> {
    return await this.connectionStatus.textContent() || '';
  }
  
  async isTypingIndicatorVisible(): Promise<boolean> {
    return await this.typingIndicator.isVisible();
  }
}
