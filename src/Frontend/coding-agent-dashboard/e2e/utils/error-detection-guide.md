# Error Detection in E2E Tests

## Overview
All E2E tests should detect database and processing errors that are sent to the client. This ensures tests fail appropriately when backend errors occur.

## Usage

### 1. Using `assertNoErrors` helper function

```typescript
import { assertNoErrors } from './utils/error-detection';

test('should create task', async ({ page }) => {
  // Perform operation that might fail
  await dialogPage.submit();
  
  // Check for errors after operation
  await assertNoErrors(
    () => page.locator('body').textContent().then(t => [t || '']),
    page,
    'task creation'
  );
});
```

### 2. Using `BaseTestHelper` class

```typescript
import { BaseTestHelper } from './utils/base-test';

test('should create task', async ({ page }) => {
  const helper = new BaseTestHelper(page);
  
  // Perform operation
  await dialogPage.submit();
  
  // Check for errors
  await helper.assertNoErrors('task creation');
});
```

### 3. For Page Objects with custom content

```typescript
// In your Page Object (e.g., ChatPage)
async getAllPageContent(): Promise<string[]> {
  // Get messages
  const messages = await this.getAllMessages();
  
  // Get error notifications
  const notifications = await this.page.locator('mat-snack-bar-container').allTextContents();
  
  return [...messages, ...notifications];
}

// In your test
test('should send message', async ({ page }) => {
  await chatPage.sendMessage('Hello');
  
  await assertNoErrors(
    () => chatPage.getAllPageContent(),
    page,
    'message sending'
  );
});
```

## When to Check for Errors

Check for errors after:
- API calls (POST, PUT, DELETE)
- Form submissions
- SignalR message sending
- File uploads
- Any operation that could trigger database operations

## Error Patterns Detected

The error detection looks for:
- `❌` (error emoji)
- `Error`, `error`
- `Exception`, `exception`
- `Database`, `database`
- `Failed`, `failed`

These patterns match error messages sent from the backend via SignalR or API responses.


