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
  
  async getTypingIndicatorText(): Promise<string> {
    return await this.typingIndicator.textContent() || '';
  }
  
  async getOnlineCount(): Promise<number> {
    const text = await this.onlineCount.textContent();
    const match = text?.match(/\d+/);
    return match ? parseInt(match[0], 10) : 0;
  }
  
  async waitForMessage(content: string, timeoutMs: number = 5000): Promise<void> {
    await this.page.waitForFunction(
      (searchContent) => {
        const messages = Array.from(document.querySelectorAll('.message, .message-bubble'));
        return messages.some(msg => msg.textContent?.includes(searchContent));
      },
      content,
      { timeout: timeoutMs }
    );
  }
  
  async getMessageByContent(content: string): Promise<string> {
    const message = this.messages.filter({ hasText: content }).first();
    return await message.textContent() || '';
  }
  
  async typeMessage(text: string): Promise<void> {
    await this.messageInput.fill(text);
  }
  
  async pressEnterInMessageInput(): Promise<void> {
    await this.messageInput.press('Enter');
  }
  
  async isMessageInputEmpty(): Promise<boolean> {
    const value = await this.messageInput.inputValue();
    return value.trim() === '';
  }
  
  async waitForConnectionStatus(status: 'Connected' | 'Disconnected' | 'Reconnecting', timeoutMs: number = 5000): Promise<void> {
    await this.page.waitForFunction(
      (expectedStatus) => {
        const statusEl = document.querySelector('[data-testid="connection-status"]');
        if (!statusEl) return false;
        
        const hasWifi = statusEl.querySelector('mat-icon')?.textContent?.includes('wifi');
        const hasWifiOff = statusEl.querySelector('mat-icon')?.textContent?.includes('wifi_off');
        
        if (expectedStatus === 'Connected') return hasWifi && !hasWifiOff;
        if (expectedStatus === 'Disconnected') return hasWifiOff;
        if (expectedStatus === 'Reconnecting') {
          return statusEl.classList.contains('reconnecting');
        }
        return false;
      },
      status,
      { timeout: timeoutMs }
    );
  }
  
  async getReconnectingMessage(): Promise<string | null> {
    const reconnectEl = this.page.locator('.reconnect');
    if (await reconnectEl.isVisible()) {
      return await reconnectEl.textContent();
    }
    return null;
  }
}
