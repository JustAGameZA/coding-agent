# E2E Test Maintenance Guide

## Overview

This guide provides instructions for maintaining the E2E test suite, including how to update tests when UI changes, fix flaky tests, and add new test cases.

## Quick Reference

### Common Commands

```bash
# Run all tests
npm run e2e

# Run specific test file
npx playwright test task-create.spec.ts

# Run with UI (interactive)
npx playwright test --ui

# Debug a test
npx playwright test --debug task-create.spec.ts

# Update screenshots
npx playwright test --update-snapshots

# Show test report
npx playwright show-report
```

## Updating Tests When UI Changes

### When Selectors Break

If a test fails because a selector changed:

1. **Identify the changed element**
   ```typescript
   // Old selector (broken)
   await page.locator('.old-class').click();
   
   // New selector (working)
   await page.locator('[data-testid="new-element"]').click();
   ```

2. **Update the Page Object**
   ```typescript
   // In pages/tasks.page.ts
   readonly newElement = page.locator('[data-testid="new-element"]');
   ```

3. **Update all references**
   - Search codebase for old selector
   - Replace with new selector
   - Test locally before committing

### When Component Structure Changes

If a component's HTML structure changes:

1. **Update Page Object selectors**
   ```typescript
   // Before
   readonly title = page.locator('.task-title');
   
   // After
   readonly title = page.locator('[data-testid="task-title"]');
   ```

2. **Update test expectations**
   ```typescript
   // Before
   await expect(page.locator('.task-title')).toHaveText('Task');
   
   // After
   await expect(taskPage.title).toHaveText('Task');
   ```

### When API Contracts Change

If backend API responses change:

1. **Update mock data in fixtures.ts**
   ```typescript
   // Before
   export const mockTask = {
     id: '123',
     name: 'Task'
   };
   
   // After
   export const mockTask = {
     id: '123',
     title: 'Task',  // Changed from 'name'
     description: '...',
     status: 'Pending'
   };
   ```

2. **Update factories**
   ```typescript
   // In factories/task.factory.ts
   static createTask(overrides = {}) {
     return {
       title: overrides.title || 'Task',  // Updated field
       // ...
     };
   }
   ```

3. **Update test expectations**
   ```typescript
   // Before
   expect(task.name).toBe('Task');
   
   // After
   expect(task.title).toBe('Task');
   ```

## Fixing Flaky Tests

### Identifying Flaky Tests

A flaky test is one that:
- Passes sometimes, fails other times
- Fails in CI but passes locally
- Fails on retry

### Common Causes and Fixes

#### 1. Timing Issues

**Symptom**: Element not found errors
```typescript
// ❌ Flaky - might not be loaded yet
await page.locator('[data-testid="element"]').click();
```

**Fix**: Add explicit wait
```typescript
// ✅ Stable - waits for element
await page.waitForSelector('[data-testid="element"]');
await page.locator('[data-testid="element"]').click();
```

#### 2. Race Conditions

**Symptom**: Test fails intermittently

**Fix**: Wait for network idle
```typescript
await page.goto('/tasks');
await page.waitForLoadState('networkidle');
await taskPage.executeTask();
```

#### 3. Unstable Selectors

**Symptom**: Selector works sometimes

**Fix**: Use stable `data-testid` attributes
```typescript
// ❌ Flaky - CSS class might change
await page.locator('.task-card').click();

// ✅ Stable - test ID won't change
await page.locator('[data-testid="task-card"]').click();
```

#### 4. Async Operations

**Symptom**: Actions happen before page is ready

**Fix**: Wait for Angular
```typescript
await page.goto('/tasks');
await waitForAngular(page);  // Wait for Angular
await taskPage.executeTask();
```

### Debugging Flaky Tests

1. **Run test multiple times locally**
   ```bash
   npx playwright test task-create.spec.ts --repeat-each=10
   ```

2. **Check test execution video**
   - Videos saved in `test-results/`
   - Review to see what happened

3. **Add debugging logs**
   ```typescript
   test('should create task', async ({ page }) => {
     console.log('Step 1: Navigating');
     await page.goto('/tasks');
     
     console.log('Step 2: Waiting for Angular');
     await waitForAngular(page);
     
     console.log('Step 3: Clicking create');
     await taskPage.createButton.click();
   });
   ```

4. **Use Playwright Inspector**
   ```bash
   npx playwright test --debug
   ```

## Adding New Tests

### Step-by-Step Process

1. **Create test file**
   ```typescript
   // e2e/new-feature.spec.ts
   import { test, expect } from '@playwright/test';
   import { setupAuthenticatedUser } from './fixtures';
   
   test.describe('New Feature', () => {
     test.beforeEach(async ({ page }) => {
       await setupAuthenticatedUser(page);
     });
     
     test('should do something', async ({ page }) => {
       // Test implementation
     });
   });
   ```

2. **Create Page Object (if needed)**
   ```typescript
   // e2e/pages/new-feature.page.ts
   export class NewFeaturePage {
     readonly page: Page;
     readonly button = page.locator('[data-testid="new-button"]');
     
     constructor(page: Page) {
       this.page = page;
     }
     
     async doAction() {
       await this.button.click();
     }
   }
   ```

3. **Add test data factories (if needed)**
   ```typescript
   // e2e/factories/new-feature.factory.ts
   export class NewFeatureFactory {
     static createItem(overrides = {}) {
       return {
         id: '123',
         name: 'Item',
         ...overrides
       };
     }
   }
   ```

4. **Mock APIs**
   ```typescript
   await page.route('**/api/new-feature', async route => {
     await route.fulfill({
       status: 200,
       body: JSON.stringify(mockData)
     });
   });
   ```

5. **Run and verify**
   ```bash
   npx playwright test new-feature.spec.ts
   ```

### Test Template

```typescript
import { test, expect } from '@playwright/test';
import { setupAuthenticatedUser, waitForAngular } from './fixtures';
import { NewFeaturePage } from './pages/new-feature.page';

test.describe('New Feature', () => {
  let featurePage: NewFeaturePage;
  
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedUser(page);
    featurePage = new NewFeaturePage(page);
    
    // Mock APIs
    await page.route('**/api/new-feature/**', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ /* mock data */ })
      });
    });
  });
  
  test('should do something', async ({ page }) => {
    // Arrange
    await page.goto('/new-feature');
    await waitForAngular(page);
    
    // Act
    await featurePage.doAction();
    
    // Assert
    await expect(featurePage.result).toBeVisible();
  });
});
```

## Best Practices

### 1. Use Page Objects

Always use Page Objects for page interactions:
```typescript
// ✅ Good
const taskPage = new TaskDetailPage(page);
await taskPage.executeTask();

// ❌ Bad
await page.locator('[data-testid="execute-button"]').click();
```

### 2. Use Test IDs

Prefer `data-testid` over CSS classes:
```typescript
// ✅ Good
page.locator('[data-testid="task-card"]');

// ❌ Bad
page.locator('.task-card');  // CSS class might change
```

### 3. Explicit Waits

Always wait for elements/network:
```typescript
// ✅ Good
await page.waitForSelector('[data-testid="element"]');
await page.waitForLoadState('networkidle');

// ❌ Bad
await page.waitForTimeout(5000);  // Fixed delay
```

### 4. Isolated Tests

Each test should be independent:
```typescript
// ✅ Good - Each test sets up its own state
test('test 1', async ({ page }) => {
  await setupAuthenticatedUser(page);
  // Test logic
});

test('test 2', async ({ page }) => {
  await setupAuthenticatedUser(page);
  // Different test logic
});

// ❌ Bad - Tests depend on each other
let sharedState;
test('test 1', () => {
  sharedState = 'something';
});

test('test 2', () => {
  expect(sharedState).toBe('something');  // Depends on test 1
});
```

### 5. Descriptive Test Names

Use clear, descriptive test names:
```typescript
// ✅ Good
test('should create task with valid title and description', async () => {});

// ❌ Bad
test('test task', async () => {});
```

## Troubleshooting

### Test Fails Locally But Passes in CI

Possible causes:
1. **Environment differences**: Different Node version, browser version
2. **Network issues**: Local network might be slow
3. **Race conditions**: Timing differences

**Solution**: Ensure local environment matches CI

### Test Passes Locally But Fails in CI

Possible causes:
1. **Timeout issues**: CI is slower
2. **Resource limits**: CI has less memory/CPU
3. **Network delays**: CI network might be slower

**Solution**: Increase timeouts or optimize test

### Element Not Found Errors

Possible causes:
1. **Element not loaded**: Page still loading
2. **Selector changed**: UI changed
3. **Element hidden**: Element exists but not visible

**Solution**:
```typescript
// Wait for element
await page.waitForSelector('[data-testid="element"]', { state: 'visible' });

// Or wait for network
await page.waitForLoadState('networkidle');
```

### API Mock Not Working

Possible causes:
1. **Route pattern mismatch**: URL doesn't match pattern
2. **Route registered too late**: Mock after request
3. **Multiple routes**: Earlier route matched

**Solution**:
```typescript
// Register route before navigation
await page.route('**/api/tasks', async route => {
  await route.fulfill({ /* ... */ });
});

await page.goto('/tasks');  // Then navigate
```

## Maintenance Checklist

When making changes:

- [ ] Run tests locally before committing
- [ ] Update Page Objects if selectors changed
- [ ] Update mocks if API contracts changed
- [ ] Update factories if data structures changed
- [ ] Check for flaky tests and fix timing issues
- [ ] Update documentation if test strategy changed
- [ ] Verify tests pass in CI

## Getting Help

- **Playwright Docs**: https://playwright.dev/docs/intro
- **Test Strategy Doc**: `docs/E2E-TEST-EXECUTION-STRATEGY.md`
- **Team Channel**: Ask in #qa or #frontend channels

---

**Last Updated**: January 2025  
**Maintained By**: QA Team

