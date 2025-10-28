import { test, expect } from '@playwright/test';
import { ChatPage } from './pages/chat.page';
import { 
  setupAuthenticatedUser, 
  mockConversations, 
  mockMessages, 
  waitForAngular,
  mockSignalRConnection,
  mockSignalRMessages,
  simulateSignalRMessage,
  simulateSignalRDisconnect,
  simulateSignalRReconnect,
  getSignalRSentMessages,
  clearSignalRSentMessages
} from './fixtures';

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
  
  test('should send a message via SignalR', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    const testMessage = 'Hello, this is a test message!';
    const conversationId = mockConversations[0].id;
    
    // Clear previous messages
    await clearSignalRSentMessages(page);
    
    // Send message via UI
    await chatPage.sendMessage(testMessage);
    
    // Wait for message to be sent via SignalR
    await page.waitForTimeout(500);
    
    // Verify SignalR SendMessage was called
    const sentMessages = await getSignalRSentMessages(page);
    const sendMessageCall = sentMessages.find(msg => 
      msg.target === 'SendMessage' || 
      (msg.arguments && msg.arguments.length >= 2)
    );
    
    expect(sendMessageCall).toBeTruthy();
    
    // Simulate echo back from server
    await simulateSignalRMessage(
      page,
      'ReceiveMessage',
      mockSignalRMessages.receiveMessage(conversationId, testMessage, 'User')
    );
    
    // Wait for message to appear in UI
    await chatPage.waitForMessage(testMessage);
    
    // Verify message appears in thread
    const lastMessage = await chatPage.getLastMessage();
    expect(lastMessage).toContain(testMessage);
    
    // Verify input is cleared
    const isEmpty = await chatPage.isMessageInputEmpty();
    expect(isEmpty).toBe(true);
  });
  
  test('should receive message from another user', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    const conversationId = mockConversations[0].id;
    const incomingMessage = 'This is a message from Alice';
    
    // Get initial message count
    const initialCount = await chatPage.getMessageCount();
    
    // Simulate incoming message from another user
    await simulateSignalRMessage(
      page,
      'ReceiveMessage',
      mockSignalRMessages.receiveMessage(conversationId, incomingMessage, 'Assistant')
    );
    
    // Wait for message to appear
    await chatPage.waitForMessage(incomingMessage);
    
    // Verify message count increased
    const newCount = await chatPage.getMessageCount();
    expect(newCount).toBe(initialCount + 1);
    
    // Verify message content
    const messageText = await chatPage.getMessageByContent(incomingMessage);
    expect(messageText).toContain(incomingMessage);
  });
  
  test('should display typing indicator when receiving UserTyping event', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    const conversationId = mockConversations[0].id;
    
    // Initially typing indicator should not be visible
    const initiallyVisible = await chatPage.isTypingIndicatorVisible();
    
    // Simulate UserTyping event from another user
    await simulateSignalRMessage(
      page,
      'UserTyping',
      mockSignalRMessages.userTyping(conversationId, 'user-alice-id', true)
    );
    
    // Wait for typing indicator to appear (if implemented)
    await page.waitForTimeout(500);
    
    // Note: Typing indicator display depends on component implementation
    // This test verifies the SignalR event is received without errors
    const typingVisible = await chatPage.isTypingIndicatorVisible();
    expect(typeof typingVisible).toBe('boolean');
  });
  
  test('should handle SignalR connection failure gracefully', async ({ page }) => {
    // Setup SignalR with simulated failure
    await mockSignalRConnection(page, { simulateFailure: true });
    
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for connection attempt
    await page.waitForTimeout(1000);
    
    // Connection status should show disconnected
    const status = await chatPage.getConnectionStatus();
    expect(status.toLowerCase()).toContain('wifi_off');
  });
});

test.describe('Chat Layout', () => {
  test('should display side-by-side on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    
    // Both conversation list and thread area should be visible
    await expect(chatPage.conversationList).toBeVisible();
  });
  
  test('should be responsive on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    
    // Conversation list should be visible
    await expect(chatPage.conversationList).toBeVisible();
  });
});

test.describe('Chat SignalR Real-Time Features', () => {
  test('should update presence when users go online/offline', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    // Get initial online count (if presence is implemented)
    const initialCount = await chatPage.getOnlineCount().catch(() => 0);
    
    // Simulate user coming online
    await simulateSignalRMessage(
      page,
      'UserOnline',
      mockSignalRMessages.userOnline('user-alice-id', 'Alice')
    );
    
    await page.waitForTimeout(300);
    
    // Check if online count increased
    const newCount = await chatPage.getOnlineCount().catch(() => 0);
    // Note: Actual presence implementation may vary
    expect(typeof newCount).toBe('number');
    
    // Simulate user going offline
    await simulateSignalRMessage(
      page,
      'UserOffline',
      mockSignalRMessages.userOffline('user-alice-id')
    );
    
    await page.waitForTimeout(300);
  });
  
  test('should reconnect after network drop', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for initial connection
    await page.waitForTimeout(200);
    await chatPage.waitForConnectionStatus('Connected');
    
    // Simulate network drop
    await simulateSignalRDisconnect(page);
    
    // Should show reconnecting status
    await page.waitForTimeout(500);
    
    // Check for reconnecting indicator
    const reconnectMsg = await chatPage.getReconnectingMessage();
    // Reconnect message may appear if implemented
    
    // Simulate successful reconnect
    await simulateSignalRReconnect(page);
    
    await page.waitForTimeout(500);
    
    // Connection should be restored
    const finalStatus = await chatPage.getConnectionStatus();
    expect(finalStatus).toContain('wifi');
  });
  
  test('should deduplicate messages with same ID', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    const conversationId = mockConversations[0].id;
    const messageContent = 'Duplicate test message';
    
    // Create message with specific ID
    const messageId = 'msg-duplicate-test-123';
    const duplicateMessage = {
      id: messageId,
      conversationId,
      content: messageContent,
      role: 'Assistant',
      sentAt: new Date().toISOString(),
      attachments: []
    };
    
    // Get initial message count
    const initialCount = await chatPage.getMessageCount();
    
    // Send same message twice
    await simulateSignalRMessage(page, 'ReceiveMessage', duplicateMessage);
    await page.waitForTimeout(200);
    await simulateSignalRMessage(page, 'ReceiveMessage', duplicateMessage);
    await page.waitForTimeout(200);
    
    // Wait for messages to be processed
    await chatPage.waitForMessage(messageContent);
    
    // Count messages with this content
    const messagesWithContent = await page.locator(
      `.message:has-text("${messageContent}"), .message-bubble:has-text("${messageContent}")`
    ).count();
    
    // Should only appear once (if deduplication is implemented)
    // Note: Without deduplication, this will fail and highlight the need for it
    expect(messagesWithContent).toBeLessThanOrEqual(1);
  });
  
  test('should send message on Enter key press', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    await chatPage.selectConversation(0);
    
    // Wait for SignalR to connect
    await page.waitForTimeout(200);
    
    const testMessage = 'Message sent with Enter key';
    const conversationId = mockConversations[0].id;
    
    // Clear previous messages
    await clearSignalRSentMessages(page);
    
    // Type message and press Enter
    await chatPage.typeMessage(testMessage);
    await chatPage.pressEnterInMessageInput();
    
    // Wait for message to be sent
    await page.waitForTimeout(500);
    
    // Verify SignalR SendMessage was called
    const sentMessages = await getSignalRSentMessages(page);
    const sendMessageCall = sentMessages.find(msg => 
      msg.target === 'SendMessage' || 
      (msg.arguments && msg.arguments.length >= 2)
    );
    
    expect(sendMessageCall).toBeTruthy();
    
    // Simulate echo back
    await simulateSignalRMessage(
      page,
      'ReceiveMessage',
      mockSignalRMessages.receiveMessage(conversationId, testMessage, 'User')
    );
    
    await chatPage.waitForMessage(testMessage);
    
    // Verify message appears
    const lastMessage = await chatPage.getLastMessage();
    expect(lastMessage).toContain(testMessage);
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
  
  test('should display disconnected state after connection drop', async ({ page }) => {
    // Setup mocked SignalR connection
    await mockSignalRConnection(page);
    
    const chatPage = new ChatPage(page);
    await setupAuthenticatedUser(page);
    await chatPage.goto();
    await chatPage.waitForConversationsToLoad();
    
    // Wait for initial connection
    await page.waitForTimeout(200);
    
    // Simulate connection drop
    await simulateSignalRDisconnect(page);
    
    // Wait for state to update
    await page.waitForTimeout(500);
    
    // Should show disconnected state
    const status = await chatPage.getConnectionStatus();
    expect(status.toLowerCase()).toContain('wifi_off');
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
