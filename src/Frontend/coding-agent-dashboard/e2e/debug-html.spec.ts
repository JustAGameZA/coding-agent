import { test } from '@playwright/test';
import { mockTasksAPI, waitForAngular } from './fixtures';

test('debug: check if data-testid attributes are present', async ({ page }) => {
  // Listen to all requests
  page.on('request', request => {
    if (request.url().includes('tasks') || request.url().includes('dashboard')) {
      console.log('REQUEST:', request.method(), request.url());
    }
  });
  
  page.on('response', response => {
    if (response.url().includes('tasks') || response.url().includes('dashboard')) {
      console.log('RESPONSE:', response.status(), response.url());
    }
  });
  
  await mockTasksAPI(page);
  await page.goto('/tasks');
  await waitForAngular(page);
  await page.waitForTimeout(3000); // Extra wait for Angular
  
  // Get page HTML
  const html = await page.content();
  
  // Log relevant parts
  console.log('=== Checking for data-testid attributes ===');
  console.log('tasks-table count:', (html.match(/tasks-table/g) || []).length);
  console.log('task-row count:', (html.match(/task-row/g) || []).length);
  console.log('Table element exists:', html.includes('<table'));
  console.log('Loading overlay visible:', html.includes('loading-overlay'));
  console.log('Empty state visible:', html.includes('No tasks found'));
  console.log('Error message visible:', html.includes('error-message'));
  
  // Extract error message if present
  const errorMessage = await page.locator('.error-message span').textContent().catch(() => null);
  console.log('Error message text:', errorMessage);
  
  // Log visible text
  const body = await page.locator('body').textContent();
  console.log('Page text (first 300 chars):', body?.substring(0, 300));
});
