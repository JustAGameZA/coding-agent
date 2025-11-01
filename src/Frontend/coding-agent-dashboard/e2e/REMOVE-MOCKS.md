# Removing Mocks from E2E Tests

## Status: In Progress

All E2E tests are being updated to use real backend services instead of mocks.

## Changes Made

### fixtures.ts
- ✅ Removed `mockDashboardAPI()`
- ✅ Removed `mockTasksAPI()`
- ✅ Removed `mockChatAPI()`
- ✅ Removed `mockAuthAPI()`
- ✅ Removed `mockAPIError()`
- ✅ Removed all SignalR mocking functions
- ✅ Updated `setupAuthenticatedUser()` to use real login API
- ✅ Updated `setupAdminSession()` to use real login API

### Test Files - TODO
The following test files still have inline `page.route()` mocks that need to be removed:

- [ ] `task-create.spec.ts` - Partially updated
- [ ] `task-execution.spec.ts` - Partially updated  
- [ ] `github-integration.spec.ts` - Partially updated
- [ ] `agentic-ai-features.spec.ts`
- [ ] `token-refresh.spec.ts`
- [ ] `conversation-creation.spec.ts`
- [ ] `agentic-dashboard.spec.ts`
- [ ] `config-management.spec.ts`
- [ ] `password-change.spec.ts`
- [ ] `browser-automation.spec.ts`
- [ ] `cicd-monitoring.spec.ts`
- [ ] `ml-classification.spec.ts`
- [ ] `offline-detection.spec.ts`
- [ ] `navigation.spec.ts`
- [ ] `error-handling.spec.ts`
- [ ] `dashboard.spec.ts`
- [ ] `chat.spec.ts`
- [ ] `auth.spec.ts`
- [ ] `chat.integration.spec.ts`

## Pattern to Remove

Remove all instances of:
```typescript
await page.route('**/api/...', async route => {
  await route.fulfill({
    status: ...,
    body: JSON.stringify(...)
  });
});
```

## Requirements for Real Backend

All tests now require:
1. **Backend services running**:
   - Gateway (port 5000)
   - Auth Service
   - Chat Service  
   - Orchestration Service
   - Dashboard BFF
   - All other microservices

2. **Test data setup**:
   - Test user (`testuser` / `TestPassword123!`)
   - Admin user (`admin` / `AdminPassword123!`)
   - Test tasks, conversations, etc. (created during tests)

3. **Network connectivity**:
   - All services accessible from test environment
   - No network isolation/mocking

## Running Tests

```bash
# Ensure all backend services are running first
# Then run E2E tests
npm run test:e2e
```

