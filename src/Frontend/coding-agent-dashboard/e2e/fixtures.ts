import { Page } from '@playwright/test';
import { createConsoleErrorTracker } from './utils/console-error-tracker';
import type { ConsoleErrorTracker } from './utils/console-error-tracker';

/**
 * Test Fixtures and Helpers
 * Provides mock data and utility functions for E2E tests
 */

// setupAuthenticatedUser and setupAdminSession are defined below in this file

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
 * NOTE: All mock API functions have been removed.
 * Tests now use real backend services.
 * 
 * Mock data constants are kept below for reference/comparison purposes only.
 * They are NOT used to mock API responses.
 */

/**
 * Wait for Angular to be ready
 */
export async function waitForAngular(page: Page) {
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500); // Small buffer for Angular to stabilize
}

/**
 * Setup console error tracking for a test
 * Returns a tracker that should be checked at the end of the test
 * Usage:
 *   const consoleTracker = setupConsoleErrorTracking(page);
 *   // ... test code ...
 *   consoleTracker.assertNoErrors('test completion');
 */
export function setupConsoleErrorTracking(page: Page): ConsoleErrorTracker {
  const tracker = createConsoleErrorTracker();
  tracker.startTracking(page);
  return tracker;
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
 * NOTE: mockAuthAPI has been removed.
 * Tests now use real authentication via setupAuthenticatedUser.
 */

/**
 * Setup authenticated user for tests that require login
 * Uses default admin user (username: admin, password: admin)
 * Creates the user if it doesn't exist
 */
export async function setupAuthenticatedUser(page: Page) {
  const username = 'admin';
  const password = 'Admin123!'; // Must meet requirements: min 8 chars, uppercase, lowercase, number, special char
  const email = 'admin@example.com';
  const gatewayUrl = 'http://localhost:5000';
  
  // First, try to login with admin credentials
  try {
    const loginResponse = await page.request.post(`${gatewayUrl}/api/auth/login`, {
      data: {
        username: username,
        password: password,
        rememberMe: false
      }
    });
    
    if (loginResponse.ok()) {
      const loginData = await loginResponse.json();
      
      // Store real auth token
      await page.addInitScript((token, user) => {
        localStorage.setItem('auth_token', token);
        if (user) {
          localStorage.setItem('user', JSON.stringify(user));
        }
      }, loginData.accessToken || loginData.token, loginData.user);
      
      return; // Successfully logged in
    }
  } catch (error) {
    // Login failed, will try to register below
    console.log('Login failed, attempting to register admin user...');
  }
  
  // If login fails, try to register the admin user
  try {
    const registerResponse = await page.request.post(`${gatewayUrl}/api/auth/register`, {
      data: {
        username: username,
        email: email,
        password: password,
        confirmPassword: password
      }
    });
    
    if (registerResponse.ok()) {
      console.log('Admin user registered successfully');
    } else {
      const errorText = await registerResponse.text().catch(() => 'Unknown error');
      // User might already exist, which is okay
      console.log('Registration response:', registerResponse.status(), errorText);
    }
  } catch (error) {
    // Registration might fail if user exists, which is okay
    console.log('Registration attempt completed (might have failed if user exists)');
  }
  
  // Try login again after registration attempt
  try {
    const retryResponse = await page.request.post(`${gatewayUrl}/api/auth/login`, {
      data: {
        username: username,
        password: password,
        rememberMe: false
      }
    });
    
    if (retryResponse.ok()) {
      const loginData = await retryResponse.json();
      
      await page.addInitScript((token, user) => {
        localStorage.setItem('auth_token', token);
        if (user) {
          localStorage.setItem('user', JSON.stringify(user));
        }
      }, loginData.accessToken || loginData.token, loginData.user);
      
      return; // Successfully logged in
    } else {
      const errorText = await retryResponse.text().catch(() => 'Unknown error');
      throw new Error(`Failed to login with admin credentials. Status: ${retryResponse.status()}, Error: ${errorText}`);
    }
  } catch (error: any) {
    throw new Error(`Failed to setup authenticated user: ${error.message || error}`);
  }
}

/**
 * Setup admin user for tests that require admin role
 * Uses default admin user (username: admin, password: admin)
 * Creates the user if it doesn't exist
 */
export async function setupAdminSession(page: Page) {
  // Use the same setup as authenticated user (admin is the default)
  await setupAuthenticatedUser(page);
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
 * NOTE: All SignalR mocking functions have been removed.
 * Tests now use real SignalR connections to the backend.
 * 
 * Real SignalR connections require:
 * - Backend SignalR hub to be running
 * - WebSocket support in the test environment
 * - Network connectivity between test and backend
 */
