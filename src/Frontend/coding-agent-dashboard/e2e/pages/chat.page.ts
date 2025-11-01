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
    
    // Conversation list - try multiple selectors
    this.conversationList = page.locator('[data-testid="conversation-list"], app-conversation-list, app-conversation-list mat-nav-list');
    this.conversationItems = page.locator('[data-testid="conversation-item"], app-conversation-list mat-list-item');
    
    // Chat thread
    this.messageThread = page.locator('[data-testid="chat-thread"], mat-list[role="list"], app-message');
    this.messages = page.locator('[data-testid="chat-thread"] mat-list-item, app-message, mat-list-item[role="listitem"], .message, .message-bubble, mat-list mat-list-item');
    
      // Input area - try multiple selectors for message input (exclude file input)
      this.messageInput = page.locator('app-message-input input:not([type="file"]), app-message-input mat-form-field input, input[placeholder*="Type a message"]:not([type="file"]), textarea[placeholder*="message"]');
      this.sendButton = page.getByRole('button', { name: /send/i }).or(page.locator('app-message-input button:has-text("Send")'));
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
    // Wait for the conversation list component to be present in DOM
    // It might be loading (spinner) or have loaded (nav-list), either is fine
    try {
      // First, wait for the component to exist
      await this.page.locator('app-conversation-list').waitFor({ state: 'attached', timeout: 15000 });
      
      // Then wait for either spinner (loading) or nav-list (loaded) to be visible
      const spinner = this.page.locator('app-conversation-list mat-spinner');
      const navList = this.page.locator('app-conversation-list mat-nav-list');
      
      try {
        // Wait for either spinner or nav-list to appear
        await Promise.race([
          spinner.waitFor({ state: 'visible', timeout: 5000 }),
          navList.waitFor({ state: 'visible', timeout: 5000 })
        ]);
        
        // If spinner is visible, wait for it to disappear
        if (await spinner.isVisible().catch(() => false)) {
          await spinner.waitFor({ state: 'hidden', timeout: 10000 });
        }
      } catch {
        // If neither appears, the component might still be loading - continue anyway
      }
      
      // Give a moment for data to stabilize
      await this.page.waitForTimeout(1000);
    } catch (error) {
      // Log error but don't fail - component might not be visible yet
      console.warn('Conversation list loading timeout:', error);
    }
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
    // Wait for chat thread to be visible
    await this.messageThread.waitFor({ state: 'attached', timeout: 5000 }).catch(() => {});
    
    // Try multiple selectors for messages
    const selectors = [
      '[data-testid="chat-thread"] mat-list-item',
      'app-chat-thread mat-list-item',
      'mat-list mat-list-item',
      'mat-list-item[role="listitem"]'
    ];
    
    for (const selector of selectors) {
      const messages = this.page.locator(selector);
      const count = await messages.count();
      if (count > 0) return count;
    }
    
    return 0;
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
        // Try multiple selectors to find messages
        const selectors = [
          'app-message',
          'mat-list-item',
          '.message',
          '.message-bubble',
          '[data-testid="chat-thread"] mat-list-item',
          'mat-list mat-list-item'
        ];
        
        for (const selector of selectors) {
          const messages = Array.from(document.querySelectorAll(selector));
          if (messages.some(msg => msg.textContent?.includes(searchContent))) {
            return true;
          }
        }
        return false;
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

  /**
   * Wait for agent typing indicator to appear
   */
  async waitForTypingIndicator(timeoutMs: number = 5000): Promise<void> {
    const typingIndicator = this.page.locator('[data-testid="agent-typing"]');
    await typingIndicator.waitFor({ state: 'visible', timeout: timeoutMs }).catch(() => {
      // Typing indicator might not always appear - that's okay
    });
  }

  /**
   * Wait for agent typing indicator to disappear
   */
  async waitForTypingIndicatorToDisappear(timeoutMs: number = 10000): Promise<void> {
    const typingIndicator = this.page.locator('[data-testid="agent-typing"]');
    await typingIndicator.waitFor({ state: 'hidden', timeout: timeoutMs }).catch(() => {
      // Typing indicator might not always appear - that's okay
    });
  }

  /**
   * Get all messages as an array of text content
   */
  async getAllMessages(): Promise<string[]> {
    // Try multiple selectors for messages
    const selectors = [
      '[data-testid="chat-thread"] mat-list-item',
      'app-chat-thread mat-list-item',
      'mat-list mat-list-item',
      'mat-list-item[role="listitem"]'
    ];
    
    for (const selector of selectors) {
      const messages = this.page.locator(selector);
      const count = await messages.count();
      if (count > 0) {
        const all = await messages.all();
        return Promise.all(all.map(msg => msg.textContent() || ''));
      }
    }
    
    return [];
  }

  /**
   * Check if a specific message content exists in the thread
   */
  async hasMessage(content: string): Promise<boolean> {
    const messages = await this.getAllMessages();
    return messages.some(msg => msg.toLowerCase().includes(content.toLowerCase()));
  }

  /**
   * Send message and wait for response
   */
  async sendMessageAndWaitForResponse(message: string, timeoutMs: number = 10000): Promise<void> {
    const initialMessageCount = await this.getMessageCount();
    
    await this.sendMessage(message);
    
    // Wait for at least one new message (user message + potentially agent response)
    await this.page.waitForFunction(
      (initialCount) => {
        const messages = document.querySelectorAll('.message, .message-bubble, mat-list-item');
        return messages.length > initialCount;
      },
      initialMessageCount,
      { timeout: timeoutMs }
    );
  }

  /**
   * Clear conversation input
   */
  async clearInput(): Promise<void> {
    await this.messageInput.clear();
  }

  /**
   * Get all text content from the chat page for error detection
   */
  async getAllPageContent(): Promise<string[]> {
    // Get all messages
    const messages = await this.getAllMessages();
    
    // Get error notifications
    const errorNotifications = this.page.locator('[role="alert"], mat-snack-bar-container, .error, mat-error');
    const notificationCount = await errorNotifications.count();
    const notifications: string[] = [];
    
    for (let i = 0; i < notificationCount; i++) {
      const text = await errorNotifications.nth(i).textContent();
      if (text) notifications.push(text);
    }
    
    return [...messages, ...notifications];
  }
}
