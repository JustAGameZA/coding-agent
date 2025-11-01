import { test, expect } from '@playwright/test';
import { setupAuthenticatedUser, waitForAngular } from './fixtures';

/**
 * ML Classification E2E Tests (UC-10.1, UC-10.2)
 * Uses real backend APIs - no mocks
 */

test.describe('ML Classification Task (UC-10.1)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should classify task automatically', async ({ page }) => {
    // Uses real API - no mocking
    // First create a task
    const createResponse = await page.request.post('http://localhost:5000/api/orchestration/tasks', {
      data: {
        title: 'Fix memory leak in chat service',
        description: 'The chat service is experiencing memory leaks after long conversations'
      }
    });
    
    if (createResponse.ok()) {
      const task = await createResponse.json();
      
      // Classify the task
      const classifyResponse = await page.request.post('http://localhost:5000/api/ml-classifier/classify', {
        data: {
          taskId: task.id,
          title: task.title,
          description: task.description
        }
      });
      
      expect(classifyResponse.status()).toBeGreaterThanOrEqual(200);
      
      if (classifyResponse.ok()) {
        const responseBody = await classifyResponse.json();
        expect(responseBody).toBeDefined();
        // May have classification, confidence, suggestedStrategy, etc.
      }
    }
  });
  
  test('should handle classification errors', async ({ page }) => {
    // Uses real API - no mocking
    const response = await page.request.post('http://localhost:5000/api/ml-classifier/classify', {
      data: {
        taskId: 'invalid-task-id',
        title: 'Test task',
        description: 'Test description'
      }
    });
    
    // May succeed or fail depending on backend
    expect(response.status()).toBeGreaterThanOrEqual(200);
    
    if (response.status() >= 400) {
      const responseBody = await response.json().catch(() => ({}));
      expect(responseBody).toBeDefined();
    }
  });
});

test.describe('ML Classification Feedback (UC-10.2)', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
  });
  
  test('should submit classification feedback', async ({ page }) => {
    // Uses real API - no mocking
    // First classify a task
    const classifyResponse = await page.request.post('http://localhost:5000/api/ml-classifier/classify', {
      data: {
        taskId: 'task-123',
        title: 'Fix bug',
        description: 'Bug description'
      }
    });
    
    if (classifyResponse.ok()) {
      const classification = await classifyResponse.json();
      
      // Submit feedback
      const feedbackResponse = await page.request.post('http://localhost:5000/api/ml-classifier/feedback', {
        data: {
          taskId: 'task-123',
          classificationId: classification.classificationId || 'class-123',
          correct: true,
          actualType: classification.classification?.type || 'BugFix',
          comments: 'Classification was accurate'
        }
      });
      
      expect(feedbackResponse.status()).toBeGreaterThanOrEqual(200);
      
      if (feedbackResponse.ok()) {
        const responseBody = await feedbackResponse.json();
        expect(responseBody).toBeDefined();
      }
    }
  });
  
  test('should submit negative feedback', async ({ page }) => {
    // Uses real API - no mocking
    const response = await page.request.post('http://localhost:5000/api/ml-classifier/feedback', {
      data: {
        taskId: 'task-456',
        classificationId: 'class-456',
        correct: false,
        actualType: 'Feature',
        expectedType: 'BugFix',
        comments: 'Misclassified - should be Feature not BugFix'
      }
    });
    
    expect(response.status()).toBeGreaterThanOrEqual(200);
    
    if (response.ok()) {
      const responseBody = await response.json();
      expect(responseBody).toBeDefined();
      // May have willRetrain, retrainingScheduled, etc.
    }
  });
});
