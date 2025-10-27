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
export const mockUsers = {
  validUser: {
    username: 'testuser',
    email: 'testuser@example.com',
    password: 'Test@1234'
  },
  adminUser: {
    username: 'admin',
    email: 'admin@example.com',
    password: 'Admin@1234'
  },
  invalidPassword: {
    username: 'testuser',
    password: 'WrongPassword123!'
  }
};

// Mock JWT Token (Base64 encoded)
export const mockJwtToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsImlkIjoiMTIzNDU2NzgtYWJjZC00ZWZnLWhpamstbG1ub3BxcnN0dXYiLCJ1c2VybmFtZSI6InRlc3R1c2VyIiwiZW1haWwiOiJ0ZXN0dXNlckBleGFtcGxlLmNvbSIsInJvbGVzIjpbIlVzZXIiXSwiZXhwIjo5OTk5OTk5OTk5LCJpYXQiOjE2OTg0MzUyMDB9.dummySignature';

export const mockRefreshToken = 'refresh_token_mock_value_12345';

// Mock Auth Response
export const mockLoginResponse = {
  token: mockJwtToken,
  refreshToken: mockRefreshToken,
  expiresIn: 3600,
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
          token: mockJwtToken,
          refreshToken: mockRefreshToken,
          expiresIn: 3600,
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
          token: mockJwtToken,
          refreshToken: mockRefreshToken,
          expiresIn: 3600
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
