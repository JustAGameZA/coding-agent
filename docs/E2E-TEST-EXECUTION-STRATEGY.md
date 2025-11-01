# E2E Test Execution Strategy

## Overview

This document outlines the strategy for executing end-to-end (E2E) tests for the Coding Agent Dashboard frontend application. The test suite uses Playwright for browser automation and follows best practices for reliable, maintainable E2E testing.

## Test Structure

### Test Organization

```
e2e/
├── pages/              # Page Object Models
│   ├── tasks.page.ts
│   ├── task-detail.page.ts
│   ├── task-create.page.ts
│   └── ...
├── factories/          # Mock data factories
│   ├── task.factory.ts
│   └── ...
├── utils/              # Test utilities
│   └── test-helpers.ts
├── fixtures.ts         # Shared test fixtures
├── task-create.spec.ts
├── task-execution.spec.ts
└── ...
```

### Page Object Model (POM)

We use the Page Object Model pattern to encapsulate page-specific logic and selectors:

- **Benefits**: Reusable, maintainable, reduces duplication
- **Structure**: Each page/component has a corresponding page object
- **Usage**: Tests interact with pages through page objects, not direct selectors

Example:
```typescript
const taskPage = new TaskDetailPage(page);
await taskPage.goto(taskId);
await taskPage.executeTask();
```

### Mock Data Factories

Factories generate consistent, reusable test data:

- **TaskFactory**: Creates task DTOs with various statuses
- **Pattern**: Factory methods for common scenarios (pending, running, completed, failed)
- **Usage**: `TaskFactory.createPendingTask()` returns a pending task with all required fields

## Test Execution

### Running Tests

```bash
# Run all E2E tests
npm run e2e

# Run specific test file
npx playwright test task-create.spec.ts

# Run in UI mode (interactive)
npx playwright test --ui

# Run in headed mode (see browser)
npx playwright test --headed

# Run specific browser
npx playwright test --project=chromium
```

### CI/CD Execution

Tests run automatically on:
- Pull requests (before merge)
- Main branch pushes
- Scheduled runs (nightly)

CI Configuration:
- **Parallel execution**: Tests run in parallel across multiple workers
- **Retries**: Failed tests retry up to 2 times before marking as failed
- **Artifacts**: Screenshots and videos saved for failed tests

### Test Categories

1. **Smoke Tests**: Critical path tests (login, dashboard load)
   - Run: Every commit
   - Duration: < 5 minutes
   
2. **Regression Tests**: Full test suite
   - Run: On PR, nightly
   - Duration: ~15-20 minutes
   
3. **Extended Tests**: Full suite + edge cases
   - Run: Weekly, pre-release
   - Duration: ~30-40 minutes

## Test Data Management

### Mock APIs

All tests use mocked API responses for:
- **Isolation**: Tests don't depend on backend state
- **Speed**: No network delays
- **Reliability**: Consistent responses

Mocking strategy:
```typescript
await page.route('**/api/tasks', async route => {
  await route.fulfill({
    status: 200,
    body: JSON.stringify(mockTasks)
  });
});
```

### Authentication

Tests use mock authentication tokens:
```typescript
await setupAuthenticatedUser(page);
// Sets localStorage with mock token
```

### Data Cleanup

- **Before each test**: Clear localStorage, cookies
- **After each test**: No cleanup needed (tests are isolated)
- **Test data**: Each test creates its own data

## Test Reliability

### Flakiness Prevention

1. **Explicit Waits**: Use Playwright's built-in waits
   ```typescript
   await page.waitForSelector('[data-testid="element"]');
   await page.waitForLoadState('networkidle');
   ```

2. **Avoid Fixed Delays**: Use `waitFor` instead of `setTimeout`
   ```typescript
   // ❌ Bad
   await page.waitForTimeout(5000);
   
   // ✅ Good
   await page.waitForSelector('.element', { state: 'visible' });
   ```

3. **Stable Selectors**: Use `data-testid` attributes
   ```typescript
   // ❌ Bad (CSS class might change)
   await page.locator('.task-card').click();
   
   // ✅ Good (stable test ID)
   await page.locator('[data-testid="task-card"]').click();
   ```

### Retry Strategy

- **Automatic retries**: Playwright retries failed assertions
- **Manual retries**: Retry up to 2 times in CI
- **Flaky test detection**: Track tests that fail intermittently

## Test Coverage

### Current Coverage

- ✅ User Authentication (login, register, password change)
- ✅ Task Management (create, execute, cancel, retry)
- ✅ Configuration Management (admin features)
- ✅ Agentic AI Dashboard
- ✅ Conversation Creation
- ✅ Token Refresh
- ⏳ Agentic AI Features (memory, reflection, planning)
- ⏳ GitHub Integration
- ⏳ Browser Automation
- ⏳ CI/CD Monitoring
- ⏳ ML Classification
- ⏳ Accessibility

### Coverage Goals

- **Target**: 80% of critical user paths
- **Priority**: High-traffic flows first
- **Edge Cases**: Cover error scenarios, validation

## Best Practices

### 1. Test Independence

Each test should:
- Set up its own state
- Not depend on other tests
- Be able to run in any order

### 2. Descriptive Test Names

```typescript
// ❌ Bad
test('test task', async () => {});

// ✅ Good
test('should create task with valid data', async () => {});
```

### 3. Arrange-Act-Assert Pattern

```typescript
test('should execute task', async ({ page }) => {
  // Arrange
  await setupAuthenticatedUser(page);
  await page.goto('/tasks');
  
  // Act
  await taskPage.executeTask();
  
  // Assert
  await expect(taskPage.executeButton).not.toBeVisible();
});
```

### 4. Test Isolation

- Clear state before each test
- Don't share state between tests
- Use beforeEach/afterEach hooks

### 5. Meaningful Assertions

```typescript
// ❌ Bad
expect(result).toBeTruthy();

// ✅ Good
expect(result.status).toBe('Completed');
expect(result.taskId).toBeDefined();
```

## Debugging Failed Tests

### Local Debugging

1. **UI Mode**: Run with `--ui` flag
   ```bash
   npx playwright test --ui
   ```

2. **Debug Mode**: Step through test
   ```bash
   npx playwright test --debug
   ```

3. **Headed Mode**: See browser actions
   ```bash
   npx playwright test --headed
   ```

### CI Debugging

1. **Screenshots**: Automatically captured on failure
2. **Videos**: Recorded for each test
3. **Traces**: Playwright trace for detailed debugging
   ```bash
   npx playwright show-trace trace.zip
   ```

### Common Issues

1. **Selector not found**: Element not loaded yet
   - Solution: Add explicit wait
   
2. **Timeout errors**: Slow API responses
   - Solution: Increase timeout or mock API
   
3. **Flaky tests**: Timing issues
   - Solution: Use stable selectors and explicit waits

## Performance

### Test Execution Time

- **Target**: Full suite < 20 minutes
- **Optimization**: Parallel execution, selective test runs
- **Monitoring**: Track execution time trends

### Optimization Strategies

1. **Parallel Execution**: Run tests in parallel across workers
2. **Selective Runs**: Run only changed tests
3. **Test Prioritization**: Run critical tests first

## Maintenance

### Regular Tasks

1. **Update selectors**: When UI changes
2. **Update mocks**: When API contracts change
3. **Review flaky tests**: Identify and fix unreliable tests
4. **Update documentation**: Keep strategy doc current

### Test Maintenance Schedule

- **Weekly**: Review flaky test reports
- **Monthly**: Update test data factories
- **Quarterly**: Review and update test strategy

## Tools and Resources

### Playwright Documentation
- https://playwright.dev/docs/intro

### Test Utilities
- `waitForAngular()`: Wait for Angular to be ready
- `setupAuthenticatedUser()`: Setup mock auth session
- `mockAPIResponse()`: Mock API responses

### Test Factories
- `TaskFactory`: Create mock task data
- `UserFactory`: Create mock user data (if needed)

## Future Improvements

1. **Visual Regression Testing**: Compare screenshots
2. **Performance Testing**: Measure page load times
3. **Cross-Browser Testing**: Test on all supported browsers
4. **Mobile Testing**: Test responsive design
5. **Accessibility Testing**: Automated a11y checks

---

**Last Updated**: January 2025  
**Maintained By**: QA Team

