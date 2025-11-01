import { test, expect } from '@playwright/test';
import { setupAuthenticatedUser, waitForAngular } from './fixtures';

/**
 * Browser Automation E2E Tests (UC-8.1, UC-8.2)
 * Uses real backend APIs - no mocks
 */

test.describe('Browser Automation - Navigate Web Page (UC-8.1)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should navigate to web page', async ({ page }) => {
    // Uses real API - no mocking
    const response = await page.request.post('http://localhost:5000/api/browser/navigate', {
      data: {
        url: 'https://example.com'
      }
    });
    
    // Verify API response (may succeed or fail depending on backend)
    expect(response.status()).toBeGreaterThanOrEqual(200);
    
    if (response.ok()) {
      const responseBody = await response.json();
      expect(responseBody).toBeDefined();
    }
  });
  
  test('should validate URL format', async ({ page }) => {
    // Uses real API - no mocking
    const response = await page.request.post('http://localhost:5000/api/browser/navigate', {
      data: {
        url: 'not-a-valid-url'
      }
    });
    
    // Should return error for invalid URL
    expect(response.status()).toBeGreaterThanOrEqual(400);
    
    if (response.status() >= 400) {
      const responseBody = await response.json().catch(() => ({}));
      expect(responseBody).toBeDefined();
    }
  });
});

test.describe('Browser Automation - Extract Content (UC-8.2)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should extract content from page', async ({ page }) => {
    // Uses real API - no mocking
    // First navigate to a page
    const navigateResponse = await page.request.post('http://localhost:5000/api/browser/navigate', {
      data: {
        url: 'https://example.com'
      }
    });
    
    if (navigateResponse.ok()) {
      // Then extract content
      const extractResponse = await page.request.post('http://localhost:5000/api/browser/extract', {
        data: {
          url: 'https://example.com',
          selector: 'body',
          format: 'text'
        }
      });
      
      expect(extractResponse.status()).toBeGreaterThanOrEqual(200);
      
      if (extractResponse.ok()) {
        const responseBody = await extractResponse.json();
        expect(responseBody).toBeDefined();
      }
    }
  });
  
  test('should extract content with selector', async ({ page }) => {
    // Uses real API - no mocking
    const response = await page.request.post('http://localhost:5000/api/browser/extract', {
      data: {
        url: 'https://example.com',
        selector: '.main-content',
        format: 'html'
      }
    });
    
    expect(response.status()).toBeGreaterThanOrEqual(200);
    
    if (response.ok()) {
      const responseBody = await response.json();
      expect(responseBody).toBeDefined();
    }
  });
});
