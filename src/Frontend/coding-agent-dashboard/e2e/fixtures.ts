import { Page } from '@playwright/test';

/**
 * Test Fixtures and Helpers
 * Provides mock data and utility functions for E2E tests
 */

// Mock Dashboard Stats (must match DashboardStats interface)
export const mockDashboardStats = {
  totalConversations: 15,
  totalMessages: 342,
  totalTasks: 42,
  completedTasks: 30,
  failedTasks: 4,
  runningTasks: 8,
  averageTaskDuration: 3.5, // seconds
  lastUpdated: new Date().toISOString()
};

// Mock Tasks
export const mockTasks = [
  {
    id: '550e8400-e29b-41d4-a716-446655440001',
    title: 'Implement user authentication',
    type: 'Feature',
    complexity: 'Medium',
    status: 'Completed',
    duration: 2.5,
    createdAt: '2025-10-27T10:00:00Z',
    completedAt: '2025-10-27T12:30:00Z',
    pullRequestNumber: 123
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440002',
    title: 'Fix memory leak in chat service',
    type: 'BugFix',
    complexity: 'Simple',
    status: 'InProgress',
    duration: null,
    createdAt: '2025-10-27T14:00:00Z',
    completedAt: null,
    pullRequestNumber: null
  },
  {
    id: '550e8400-e29b-41d4-a716-446655440003',
    title: 'Refactor orchestration engine',
    type: 'Refactor',
    complexity: 'Complex',
    status: 'Failed',
    duration: 5.2,
    createdAt: '2025-10-27T08:00:00Z',
    completedAt: '2025-10-27T13:12:00Z',
    pullRequestNumber: null
  }
];

// Mock Conversations
export const mockConversations = [
  {
    id: '660e8400-e29b-41d4-a716-446655440001',
    title: 'Project Planning',
    lastMessage: 'Let\'s discuss the architecture',
    unreadCount: 2,
    updatedAt: '2025-10-27T15:30:00Z'
  },
  {
    id: '660e8400-e29b-41d4-a716-446655440002',
    title: 'Bug Investigation',
    lastMessage: 'Found the root cause',
    unreadCount: 0,
    updatedAt: '2025-10-27T14:00:00Z'
  }
];

// Mock Messages
export const mockMessages = [
  {
    id: 'msg-001',
    conversationId: '660e8400-e29b-41d4-a716-446655440001',
    content: 'Hello! How can I help you today?',
    role: 'Assistant',
    sentAt: '2025-10-27T15:00:00Z',
    attachments: []
  },
  {
    id: 'msg-002',
    conversationId: '660e8400-e29b-41d4-a716-446655440001',
    content: 'I need help with the authentication module',
    role: 'User',
    sentAt: '2025-10-27T15:05:00Z',
    attachments: []
  }
];

/**
 * Mock API responses for isolated testing
 */
export async function mockDashboardAPI(page: Page) {
  // Mock Dashboard BFF stats endpoint (via Gateway)
  await page.route('**/api/dashboard/stats', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockDashboardStats)
    });
  });
}

export async function mockTasksAPI(page: Page) {
  // Mock Dashboard BFF tasks endpoint (via Gateway) - returns array directly, not paginated response
  await page.route('**/api/dashboard/tasks*', async route => {
    console.log('MOCKING TASKS API:', route.request().url());
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockTasks) // Return array directly, not wrapped in pagination object
    });
  });
}

export async function mockChatAPI(page: Page) {
  // Mock conversations list - return array directly
  await page.route('**/api/conversations*', async route => {
    console.log('MOCKING CHAT API:', route.request().url());
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockConversations) // Return array directly
    });
  });
  
  // Mock messages for a conversation - return PagedResponse format
  await page.route('**/api/conversations/*/messages*', async route => {
    console.log('MOCKING MESSAGES API:', route.request().url());
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: mockMessages,
        nextCursor: null
      })
    });
  });
}

export async function mockAPIError(page: Page, statusCode: number = 500) {
  await page.route('**/api/**', async route => {
    await route.fulfill({
      status: statusCode,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'Internal Server Error' })
    });
  });
}

/**
 * Wait for Angular to be ready
 */
export async function waitForAngular(page: Page) {
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500); // Small buffer for Angular to stabilize
}

/**
 * Helper to wait for API call and return response
 */
export async function waitForAPICall(page: Page, urlPattern: string) {
  return await page.waitForResponse(response => 
    response.url().includes(urlPattern) && response.status() === 200
  );
}

// Mock Users for Auth Testing
// Using timestamp to ensure unique users for each test run
export const mockUsers = {
  validUser: {
    username: `e2euser_${Date.now()}`,
    email: `e2euser_${Date.now()}@example.com`,
    password: 'E2ETest@1234!'
  },
  adminUser: {
    username: `e2eadmin_${Date.now()}`,
    email: `e2eadmin_${Date.now()}@example.com`,
    password: 'E2EAdmin@1234!'
  },
  invalidPassword: {
    username: `e2euser_${Date.now()}`,
    password: 'WrongPassword123!'
  }
};

// Mock JWT Token (Base64 encoded)
export const mockJwtToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsImlkIjoiMTIzNDU2NzgtYWJjZC00ZWZnLWhpamstbG1ub3BxcnN0dXYiLCJ1c2VybmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0dXNlckBleGFtcGxlLmNvbSIsInJvbGVzIjpbIlVzZXIiXSwiZXhwIjo5OTk5OTk5OTk5LCJpYXQiOjE2OTg0MzUyMDB9.dummySignature';

export const mockRefreshToken = 'refresh_token_mock_value_12345';

// Mock Auth Response
export const mockLoginResponse = {
  accessToken: mockJwtToken,
  refreshToken: mockRefreshToken,
  expiresIn: 3600,
  tokenType: 'Bearer',
  user: {
    id: '12345678-abcd-4efg-hijk-lmnopqrstuv',
    username: 'testuser',
    email: 'testuser@example.com',
    roles: ['User']
  }
};

/**
 * Mock Auth API responses for isolated testing
 */
export async function mockAuthAPI(page: Page) {
  // Mock successful login
  await page.route('**/api/auth/login', async route => {
    const request = route.request();
    const postData = request.postDataJSON();
    
    console.log('MOCKING AUTH LOGIN:', postData);
    
    // Check credentials
    if (postData.username === mockUsers.validUser.username && 
        postData.password === mockUsers.validUser.password) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockLoginResponse)
      });
    } else if (postData.username === mockUsers.adminUser.username && 
               postData.password === mockUsers.adminUser.password) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ...mockLoginResponse,
          user: {
            id: '87654321-dcba-4fed-ihgk-vwxyzabcdefg',
            username: 'admin',
            email: 'admin@example.com',
            roles: ['Admin']
          }
        })
      });
    } else {
      // Invalid credentials
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ 
          message: 'Invalid username or password',
          error: 'Unauthorized'
        })
      });
    }
  });
  
  // Mock successful registration
  await page.route('**/api/auth/register', async route => {
    const request = route.request();
    const postData = request.postDataJSON();
    
    console.log('MOCKING AUTH REGISTER:', postData);
    
    // Check for duplicate username/email
    if (postData.username === 'testuser' || postData.email === 'testuser@example.com') {
      await route.fulfill({
        status: 409,
        contentType: 'application/json',
        body: JSON.stringify({ 
          message: 'Username or email already exists',
          error: 'Conflict'
        })
      });
    } else {
      // Success - return login response
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: mockJwtToken,
          refreshToken: mockRefreshToken,
          expiresIn: 3600,
          tokenType: 'Bearer',
          user: {
            id: postData.username + '-id-mock',
            username: postData.username,
            email: postData.email,
            roles: ['User']
          }
        })
      });
    }
  });
  
  // Mock token refresh
  await page.route('**/api/auth/refresh', async route => {
    const request = route.request();
    const postData = request.postDataJSON();
    
    console.log('MOCKING AUTH REFRESH:', postData);
    
    if (postData.refreshToken === mockRefreshToken) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accessToken: mockJwtToken,
          refreshToken: mockRefreshToken,
          expiresIn: 3600,
          tokenType: 'Bearer'
        })
      });
    } else {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ 
          message: 'Invalid refresh token',
          error: 'Unauthorized'
        })
      });
    }
  });
  
  // Mock /me endpoint
  await page.route('**/api/auth/me', async route => {
    const authHeader = route.request().headers()['authorization'];
    
    if (authHeader && authHeader.includes(mockJwtToken)) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockLoginResponse.user)
      });
    } else {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ 
          message: 'Unauthorized',
          error: 'Unauthorized'
        })
      });
    }
  });
}

/**
 * Setup authenticated user for tests that require login
 * Sets token in localStorage and mocks API responses
 */
export async function setupAuthenticatedUser(page: Page) {
  // Mock auth API
  await mockAuthAPI(page);
  
  // Set auth token in localStorage
  await page.addInitScript((token) => {
    localStorage.setItem('auth_token', token);
  }, mockJwtToken);
  
  // Also mock the API endpoints that require auth
  await mockDashboardAPI(page);
  await mockTasksAPI(page);
  await mockChatAPI(page);
}

// Mock SignalR negotiate response
export const mockSignalRNegotiateResponse = {
  connectionId: 'mock-connection-id-12345',
  connectionToken: 'mock-connection-token-67890',
  negotiateVersion: 1,
  availableTransports: [
    {
      transport: 'WebSockets',
      transferFormats: ['Text', 'Binary']
    }
  ]
};

// Mock SignalR messages
export const mockSignalRMessages = {
  receiveMessage: (conversationId: string, content: string, role: 'User' | 'Assistant' = 'Assistant') => ({
    id: `msg-${Date.now()}`,
    conversationId,
    content,
    role,
    sentAt: new Date().toISOString(),
    attachments: []
  }),
  userTyping: (conversationId: string, userId: string, isTyping: boolean) => ({
    conversationId,
    userId,
    username: 'Alice',
    isTyping
  }),
  userOnline: (userId: string, username: string = 'Alice') => ({
    userId,
    username,
    status: 'Online'
  }),
  userOffline: (userId: string) => ({
    userId,
    status: 'Offline'
  })
};

/**
 * Mock SignalR WebSocket connection
 * Intercepts negotiate endpoint and WebSocket upgrade
 */
export async function mockSignalRConnection(page: Page, options?: {
  simulateFailure?: boolean;
  delayMs?: number;
}) {
  const { simulateFailure = false, delayMs = 0 } = options || {};
  
  // Mock SignalR negotiate endpoint
  await page.route('**/hubs/chat/negotiate**', async route => {
    console.log('MOCKING SignalR negotiate');
    
    if (simulateFailure) {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Connection failed' })
      });
      return;
    }
    
    if (delayMs > 0) {
      await new Promise(resolve => setTimeout(resolve, delayMs));
    }
    
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockSignalRNegotiateResponse)
    });
  });
  
  // Inject mock SignalR WebSocket into the page
  await page.addInitScript(() => {
    // Store original WebSocket
    const OriginalWebSocket = window.WebSocket;
    
    // Create mock WebSocket class
    class MockWebSocket {
      url: string;
      readyState: number = 0; // CONNECTING
      onopen: ((event: Event) => void) | null = null;
      onclose: ((event: CloseEvent) => void) | null = null;
      onmessage: ((event: MessageEvent) => void) | null = null;
      onerror: ((event: Event) => void) | null = null;
      
      private messageHandlers: Set<Function> = new Set();
      
      static CONNECTING = 0;
      static OPEN = 1;
      static CLOSING = 2;
      static CLOSED = 3;
      
      constructor(url: string) {
        this.url = url;
        console.log('[MockWebSocket] Created for:', url);
        
        // Store instance globally for test access
        (window as any).__mockWebSocket = this;
        
        // Simulate successful connection after 100ms
        setTimeout(() => {
          this.readyState = 1; // OPEN
          if (this.onopen) {
            this.onopen(new Event('open'));
          }
          console.log('[MockWebSocket] Connected');
        }, 100);
      }
      
      send(data: string) {
        console.log('[MockWebSocket] Send:', data);
        try {
          const parsed = JSON.parse(data);
          // Store sent messages for test verification
          if (!(window as any).__signalRSentMessages) {
            (window as any).__signalRSentMessages = [];
          }
          (window as any).__signalRSentMessages.push(parsed);
        } catch (e) {
          console.log('[MockWebSocket] Non-JSON message:', data);
        }
      }
      
      close() {
        console.log('[MockWebSocket] Close requested');
        this.readyState = 3; // CLOSED
        if (this.onclose) {
          this.onclose(new CloseEvent('close'));
        }
      }
      
      // Helper method for tests to simulate incoming messages
      simulateMessage(data: any) {
        if (this.onmessage) {
          const messageData = typeof data === 'string' ? data : JSON.stringify(data);
          this.onmessage(new MessageEvent('message', { data: messageData }));
        }
      }
    }
    
    // Replace WebSocket with mock
    (window as any).WebSocket = MockWebSocket;
    console.log('[Test] WebSocket mocked successfully');
  });
}

/**
 * Simulate incoming SignalR message
 */
export async function simulateSignalRMessage(page: Page, method: string, ...args: any[]) {
  await page.evaluate(({ method, args }) => {
    const ws = (window as any).__mockWebSocket;
    if (!ws) {
      throw new Error('MockWebSocket not initialized');
    }
    
    // SignalR message format: {"type":1,"target":"MethodName","arguments":[...]}
    const signalRMessage = {
      type: 1, // Invocation message
      target: method,
      arguments: args
    };
    
    ws.simulateMessage(JSON.stringify(signalRMessage) + '\x1e');
    console.log('[Test] Simulated SignalR message:', method, args);
  }, { method, args });
}

/**
 * Get messages sent via SignalR from the page
 */
export async function getSignalRSentMessages(page: Page): Promise<any[]> {
  return await page.evaluate(() => {
    return (window as any).__signalRSentMessages || [];
  });
}

/**
 * Clear SignalR sent messages
 */
export async function clearSignalRSentMessages(page: Page) {
  await page.evaluate(() => {
    (window as any).__signalRSentMessages = [];
  });
}

/**
 * Simulate SignalR connection drop
 */
export async function simulateSignalRDisconnect(page: Page) {
  await page.evaluate(() => {
    const ws = (window as any).__mockWebSocket;
    if (ws && ws.onclose) {
      ws.readyState = 3; // CLOSED
      ws.onclose(new CloseEvent('close'));
      console.log('[Test] Simulated disconnect');
    }
  });
}

/**
 * Simulate SignalR reconnection
 */
export async function simulateSignalRReconnect(page: Page) {
  await page.evaluate(() => {
    const ws = (window as any).__mockWebSocket;
    if (ws && ws.onopen) {
      ws.readyState = 1; // OPEN
      ws.onopen(new Event('open'));
      console.log('[Test] Simulated reconnect');
    }
  });
}
