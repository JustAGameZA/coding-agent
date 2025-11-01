# E2E Test Checklist

## Pre-Run Checks

- [ ] All test files are present in `e2e/` directory
- [ ] All imports resolve correctly
- [ ] Page objects exist and are properly structured
- [ ] Mock factories are in place
- [ ] Test fixtures are correctly exported

## Test Categories

### Authentication Tests
- [x] `auth.spec.ts` - Login, register, logout
- [x] `auth.integration.spec.ts` - Full auth flow
- [x] `password-change.spec.ts` - Password change UI and API

### Task Management Tests
- [x] `tasks.spec.ts` - Task list view
- [x] `task-detail.spec.ts` - Task detail view
- [x] `task-create.spec.ts` - Create task dialog
- [x] `task-execution.spec.ts` - Execute, cancel, retry tasks

### Agentic AI Tests
- [x] `agentic-dashboard.spec.ts` - Agentic AI dashboard
- [x] `agentic-ai-features.spec.ts` - Memory, reflection, planning

### Configuration Tests
- [x] `config-management.spec.ts` - Admin configuration page

### GitHub Integration Tests
- [x] `github-integration.spec.ts` - Repository connection and sync

### Other Service Tests
- [x] `browser-automation.spec.ts` - Browser automation APIs
- [x] `cicd-monitoring.spec.ts` - CI/CD monitoring
- [x] `ml-classification.spec.ts` - ML classification
- [x] `conversation-creation.spec.ts` - Chat conversation creation
- [x] `token-refresh.spec.ts` - Token refresh flow
- [x] `offline-detection.spec.ts` - Offline detection
- [x] `accessibility.spec.ts` - Accessibility features

## Common Issues to Check

### Import Issues
- [x] All tests import from `./fixtures` correctly
- [x] All page objects are imported correctly
- [x] All factories are imported correctly

### API Mocking
- [x] Task APIs use `/api/orchestration/tasks` for create/execute/cancel/retry
- [x] Task list uses `/api/dashboard/tasks` (Dashboard BFF)
- [x] Auth APIs use `/api/auth/*`
- [x] GitHub APIs use `/api/github/*`
- [x] Config APIs use `/api/admin/config`

### Test Data
- [x] Mock tasks match `TaskDto` interface
- [x] Mock conversations match expected format
- [x] Mock users have correct roles

### Selectors
- [x] All selectors use `data-testid` attributes
- [x] Page objects encapsulate selectors
- [x] Selectors match actual component attributes

## Running Tests

```bash
# Run all tests
npm run test:e2e

# Run in UI mode (recommended for debugging)
npm run test:e2e:ui

# Run specific test file
npx playwright test task-create.spec.ts

# Run with headed browser
npm run test:e2e:headed

# Run only Chromium
npm run test:e2e:chromium
```

## Fixes Applied

1. ✅ Fixed duplicate `waitForAngular` export
2. ✅ Added `setupAdminSession` to fixtures.ts
3. ✅ Fixed type inference issues in token-refresh and conversation-creation tests
4. ✅ Ensured all imports are correct
5. ✅ Verified API routes match actual service endpoints
6. ✅ Verified test data structures match models

## Notes

- Tests use mocked APIs by default for fast execution
- To test against real backend, comment out mock API calls
- Some tests may be skipped if features aren't implemented yet
- All tests should pass with mocked APIs

