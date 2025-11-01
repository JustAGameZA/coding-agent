# Adding Console Error Checking to E2E Tests

## Overview
All E2E tests should check for console errors to ensure no JavaScript errors occur during test execution.

## Pattern to Add

For each `test.describe` block, add:

1. **Import the helper**:
```typescript
import { setupConsoleErrorTracking } from './fixtures';
```

2. **Declare tracker variable** (inside test.describe):
```typescript
let consoleTracker: ReturnType<typeof setupConsoleErrorTracking>;
```

3. **Setup in beforeEach**:
```typescript
test.beforeEach(async ({ page }) => {
  // Setup console error tracking
  consoleTracker = setupConsoleErrorTracking(page);
  
  // ... rest of beforeEach code ...
});
```

4. **Check in afterEach**:
```typescript
test.afterEach(async () => {
  // Check for console errors
  consoleTracker.assertNoErrors('test execution');
});
```

## Example

```typescript
import { test, expect } from '@playwright/test';
import { setupAuthenticatedUser, waitForAngular, setupConsoleErrorTracking } from './fixtures';

test.describe('My Feature', () => {
  let consoleTracker: ReturnType<typeof setupConsoleErrorTracking>;
  
  test.beforeEach(async ({ page }) => {
    // Setup console error tracking
    consoleTracker = setupConsoleErrorTracking(page);
    
    // ... rest of setup ...
  });

  test.afterEach(async () => {
    // Check for console errors
    consoleTracker.assertNoErrors('test execution');
  });
  
  // ... tests ...
});
```

## Alternative: Using BaseTestHelper

If you're using `BaseTestHelper`, console error tracking is already included:

```typescript
import { BaseTestHelper } from './utils/base-test';

test('my test', async ({ page }) => {
  const helper = new BaseTestHelper(page);
  
  // ... test code ...
  
  // This already checks console errors
  await helper.assertNoErrors('test completion');
});
```

## Files to Update

All `.spec.ts` files in the `e2e` directory should have console error checking added.

