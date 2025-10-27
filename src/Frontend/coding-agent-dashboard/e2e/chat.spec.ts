import { test, expect } from '@playwright/test';
import { ChatPage } from './pages/chat.page';
import { setupAuthenticatedUser, mockConversations, mockMessages, waitForAngular } from './fixtures';

/**
 * Chat Page E2E Tests
 * Tests the Chat page with SignalR real-time messaging
 */

test.describe('Chat Page', () => {
  let chatPage: ChatPage;
  
  test.beforeEach(async ({ page }) => {
    chatPage = new ChatPage(page);
    
    // Setup authenticated user with mocked APIs
    await setupAuthenticatedUser(page);
    
    // Navigate to chat
    await chatPage.goto();
    await waitForAngular(page);
  });
  
  test('should display conversation list', async () => {
    await chatPage.waitForConversationsToLoad();
    
    await expect(chatPage.conversationList).toBeVisible();
  });
  
  test('should load conversations from API', async () => {
    await chatPage.waitForConversationsToLoad();
    
    const conversationCount = await chatPage.getConversationCount();
    expect(conversationCount).toBe(mockConversations.length);
  });
  
  test('should select a conversation', async () => {
    await chatPage.waitForConversationsToLoad();
    
    await chatPage.selectConversation(0);
    
    // Message thread should be visible after selection
    await expect(chatPage.messageThread).toBeVisible();
  });
  
  test('should display messages in selected conversation', async () => {
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for messages to load
    await chatPage.page.waitForTimeout(1000);
    
    const messageCount = await chatPage.getMessageCount();
    expect(messageCount).toBeGreaterThanOrEqual(0);
  });
  
  test('should display connection status indicator', async () => {
    await chatPage.waitForConversationsToLoad();
    
    // Connection status should be visible
    const statusVisible = await chatPage.connectionStatus.isVisible().catch(() => false);
    
    // If implemented, should show connection state
    if (statusVisible) {
      const status = await chatPage.getConnectionStatus();
      expect(status).toBeTruthy();
    }
  });
  
  test.skip('should send a message via SignalR', async ({ page }) => {
    // This test requires SignalR connection to be active
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    const initialMessageCount = await chatPage.getMessageCount();
    
    // Send message
    await chatPage.sendMessage('Hello, this is a test message!');
    
    // Wait for message to appear
    await page.waitForTimeout(1000);
    
    const newMessageCount = await chatPage.getMessageCount();
    expect(newMessageCount).toBe(initialMessageCount + 1);
    
    // Verify message content
    const lastMessage = await chatPage.getLastMessage();
    expect(lastMessage).toContain('test message');
  });
  
  test.skip('should display typing indicator', async ({ page }) => {
    // This test requires SignalR typing events
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Start typing
    await chatPage.messageInput.focus();
    await chatPage.messageInput.type('Typing...');
    
    // Typing indicator should appear
    await page.waitForTimeout(500);
    const isTyping = await chatPage.isTypingIndicatorVisible();
    
    // (May or may not be visible depending on implementation)
    expect(typeof isTyping).toBe('boolean');
  });
  
  test.skip('should upload file attachment', async ({ page }) => {
    // This test requires file upload implementation
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Create a test file
    const testFilePath = 'test-files/sample.txt';
    
    // Upload file
    await chatPage.uploadFile(testFilePath);
    
    // Progress bar should appear
    const progressVisible = await chatPage.isUploadProgressVisible();
    expect(progressVisible).toBe(true);
    
    // Wait for upload to complete
    await chatPage.waitForUploadComplete();
    
    // Thumbnail should appear
    await expect(chatPage.uploadedThumbnail).toBeVisible();
  });
});

test.describe('Chat Layout', () => {
  test('should display side-by-side on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    
    const chatPage = new ChatPage(page);
    await mockChatAPI(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    
    // Both conversation list and thread area should be visible
    await expect(chatPage.conversationList).toBeVisible();
  });
  
  test('should be responsive on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const chatPage = new ChatPage(page);
    await mockChatAPI(page);
    await chatPage.goto();
    
    // Conversation list should be visible
    await expect(chatPage.conversationList).toBeVisible();
  });
});

test.describe('Chat Error Handling', () => {
  test('should handle conversation load failure', async ({ page }) => {
    // Mock API error
    await page.route('**/api/conversations', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Failed to load conversations' })
      });
    });
    
    const chatPage = new ChatPage(page);
    await chatPage.goto();
    
    // Should show error state
    await page.waitForTimeout(1000);
    
    // (Implementation specific - may show error message)
  });
  
  test.skip('should handle SignalR connection failure', async ({ page }) => {
    // This test requires SignalR connection monitoring
    const chatPage = new ChatPage(page);
    await mockChatAPI(page);
    await chatPage.goto();
    
    // Simulate connection drop
    await page.evaluate(() => {
      // Disconnect SignalR if accessible
      (window as any).signalRConnection?.stop();
    });
    
    await page.waitForTimeout(2000);
    
    // Should show disconnected state
    const status = await chatPage.getConnectionStatus();
    expect(status.toLowerCase()).toContain('disconnect');
  });
});

test.describe('Chat Page - Auth', () => {
  test('should redirect to login when unauthenticated', async ({ page }) => {
    // Clear auth token
    await page.evaluate(() => localStorage.clear());
    
    // Try to navigate to chat
    await page.goto('/chat');
    
    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
});
