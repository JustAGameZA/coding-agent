# End-to-End Tests with Playwright

## Overview

This directory contains comprehensive E2E tests for the Angular Dashboard application using **Playwright Test**. These tests validate the full stack integration: Angular Frontend → Gateway → Microservices.

## Test Structure

```
e2e/
├── fixtures.ts              # Mock data and helper functions
├── dashboard.spec.ts        # Dashboard page tests (8 tests)
├── tasks.spec.ts            # Tasks page tests (10+ tests)
├── chat.spec.ts             # Chat flow tests (10+ tests)
├── navigation.spec.ts       # Navigation and routing tests (10+ tests)
├── error-handling.spec.ts   # Error scenarios (10+ tests)
└── pages/                   # Page Object Models
    ├── dashboard.page.ts
    ├── tasks.page.ts
    └── chat.page.ts
```

## Prerequisites

### Required Software
- Node.js 18+ and npm
- Playwright browsers (installed via npm script)

### Backend Services (Optional)
Tests can run with **mocked APIs** OR real backend services:
- Gateway: `http://localhost:5000`
- Chat Service: `http://localhost:5001`
- Orchestration Service: `http://localhost:5002`
- Dashboard BFF: `http://localhost:5003`

## Installation

### 1. Install Playwright Test (Already Done)
```bash
npm install --save-dev @playwright/test
```

### 2. Install Playwright Browsers
```bash
npx playwright install chromium firefox
```

This downloads Chromium (~450MB) and Firefox (~350MB) browsers.

## Running Tests

### Quick Start
```bash
# Run all E2E tests (headless)
npm run test:e2e

# Run with UI mode (interactive, recommended for development)
npm run test:e2e:ui

# Run in headed mode (see browser)
npm run test:e2e:headed

# Run in debug mode (step through tests)
npm run test:e2e:debug
```

### Browser-Specific Tests
```bash
# Chromium only
npm run test:e2e:chromium

# Firefox only
npm run test:e2e:firefox

# Mobile (iPhone 13 simulation)
npm run test:e2e:mobile
```

### Individual Test Files
```bash
# Dashboard tests only
npx playwright test dashboard.spec.ts

# Tasks tests only
npx playwright test tasks.spec.ts

# Chat tests only
npx playwright test chat.spec.ts

# Specific test by name
npx playwright test -g "should display all 6 stat cards"
```

### View Test Reports
```bash
# Open HTML report (after test run)
npm run test:e2e:report
```

## Test Coverage

### Dashboard Page (8 tests)
- ✅ Display page title
- ✅ Display all 6 stat cards
- ✅ Load stats from API
- ✅ Display correct stat values
- ✅ Display last updated timestamp
- ✅ Handle API errors gracefully
- ✅ Responsive on mobile viewport
- ✅ Responsive on tablet viewport
- ⏭️ Auto-refresh stats after 30 seconds (skipped - slow)

### Tasks Page (10+ tests)
- ✅ Display tasks table
- ✅ Display table headers
- ✅ Load tasks from API
- ✅ Display task data correctly
- ✅ Display status chips with colors
- ✅ Display PR links for completed tasks
- ✅ Handle empty state when no tasks
- ⏭️ Display paginator (skipped - needs more data)
- ⏭️ Navigate to next page (skipped - needs pagination)
- ✅ Responsive on mobile
- ✅ Scrollable on small screens

### Chat Page (10+ tests)
- ✅ Display conversation list
- ✅ Load conversations from API
- ✅ Select a conversation
- ✅ Display messages in selected conversation
- ✅ Display connection status indicator
- ⏭️ Send a message via SignalR (skipped - requires SignalR)
- ⏭️ Display typing indicator (skipped - requires SignalR)
- ⏭️ Upload file attachment (skipped - requires upload implementation)
- ✅ Side-by-side layout on desktop
- ✅ Responsive on mobile
- ✅ Handle conversation load failure
- ⏭️ Handle SignalR connection failure (skipped - requires SignalR)

### Navigation (10+ tests)
- ✅ Navigate to dashboard
- ✅ Navigate to tasks
- ✅ Navigate to chat
- ✅ Redirect root to dashboard
- ✅ Display navigation sidebar
- ✅ Navigate via sidebar links
- ✅ Highlight active route
- ✅ Browser back button
- ✅ Browser forward button
- ✅ Handle 404 for invalid routes
- ✅ Mobile menu display
- ⏭️ Toggle mobile menu (skipped - UI specific)

### Error Handling (10+ tests)
- ✅ Display error notification on API failure
- ✅ Retry failed requests (2 retries)
- ✅ Show fallback UI when API is down
- ✅ Handle task list load failure
- ✅ Handle network timeout
- ✅ Handle 401 Unauthorized
- ✅ Handle 403 Forbidden
- ✅ Handle 404 Not Found
- ✅ Handle malformed JSON response
- ⏭️ Show offline indicator (skipped - requires offline detection)
- ⏭️ Recover when network restored (skipped - requires offline handling)

**Total Tests**: ~50+ (including skipped tests for future features)

## Configuration

### Playwright Config (`playwright.config.ts`)

Key settings:
- **Base URL**: `http://localhost:4200`
- **Timeout**: 30 seconds per test
- **Retries**: 2 in CI, 0 locally
- **Browsers**: Chromium, Firefox, Mobile (iPhone 13)
- **Screenshots**: On failure only
- **Video**: Retained on failure
- **Web Server**: Auto-starts Angular dev server if not running

### Environment Variables

```bash
# Run in CI mode (more retries, 1 worker)
CI=true npm run test:e2e

# Use existing server (don't auto-start)
PLAYWRIGHT_SKIP_WEBSERVER=true npm run test:e2e
```

## Mocking vs Real Services

### Mocked APIs (Default for Fast Tests)
Tests use `mockDashboardAPI()`, `mockTasksAPI()`, `mockChatAPI()` from `fixtures.ts`:
- Instant responses
- No backend dependencies
- Consistent test data
- Ideal for local development

### Real Backend Services
To test against real services:
1. Start all backend services (Gateway, Chat, Orchestration, Dashboard BFF)
2. Comment out `await mockXXXAPI(page)` calls in test files
3. Run tests

## Debugging Tests

### Interactive UI Mode (Recommended)
```bash
npm run test:e2e:ui
```
- Click on tests to run
- See browser UI
- Time travel through test steps
- Inspect DOM at each step

### Debug Mode
```bash
npm run test:e2e:debug
```
- Opens Playwright Inspector
- Step through tests line-by-line
- Inspect locators
- Record new tests

### VS Code Debugging
Install "Playwright Test for VSCode" extension:
- Set breakpoints in test files
- Run tests with debugger attached
- Inspect variables and state

## CI/CD Integration

### GitHub Actions Example
```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 18
      
      - name: Install dependencies
        run: cd src/Frontend/coding-agent-dashboard && npm ci
      
      - name: Install Playwright Browsers
        run: cd src/Frontend/coding-agent-dashboard && npx playwright install --with-deps chromium
      
      - name: Run E2E Tests
        run: cd src/Frontend/coding-agent-dashboard && npm run test:e2e
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: src/Frontend/coding-agent-dashboard/playwright-report/
```

## Best Practices

### Writing Tests
1. **Use Page Objects**: Encapsulate selectors and actions
2. **Stable Selectors**: Prefer `getByRole()`, `getByText()`, `data-testid`
3. **Auto-Waiting**: Playwright waits automatically, avoid `waitForTimeout` when possible
4. **Meaningful Names**: Test names should describe what is tested
5. **AAA Pattern**: Arrange, Act, Assert
6. **Idempotent**: Tests should not depend on each other
7. **Mock External APIs**: Keep tests fast and deterministic

### Avoiding Flaky Tests
- ✅ Use `expect(locator).toBeVisible()` not `expect(await locator.isVisible()).toBe(true)`
- ✅ Use `page.waitForResponse()` for API calls
- ✅ Use `page.waitForLoadState('networkidle')` after navigation
- ❌ Avoid arbitrary `waitForTimeout()` - use specific waits
- ❌ Avoid brittle CSS selectors - use semantic locators

## Troubleshooting

### Tests Timeout
- Increase timeout in `playwright.config.ts`
- Check if backend services are running (or use mocks)
- Use `test.slow()` for slow tests

### Browser Not Found
```bash
npx playwright install chromium
```

### Port Already in Use (4200)
- Stop Angular dev server manually
- Or set `reuseExistingServer: true` in config

### Flaky Tests
- Run multiple times: `npx playwright test --repeat-each=10`
- Check for race conditions
- Add proper waits for dynamic content

### Screenshots/Videos Missing
- Videos only saved on failure
- Enable always: `video: 'on'` in config

## Known Limitations

1. **SignalR Tests Skipped**: Real-time messaging tests require SignalR connection (currently mocked)
2. **File Upload Skipped**: Requires file upload implementation
3. **Auto-Refresh Test Skipped**: Takes 30+ seconds, slow for CI
4. **Pagination Tests Skipped**: Need more mock data to trigger pagination

## Next Steps

### Phase 1: Complete Core Tests (Current)
- ✅ Dashboard, Tasks, Navigation, Error Handling

### Phase 2: Real SignalR Integration
- Implement SignalR connection in tests
- Test real-time message sending
- Test typing indicators
- Test presence updates

### Phase 3: Advanced Scenarios
- Visual regression tests (screenshot comparison)
- Performance tests (Lighthouse CI)
- Accessibility tests (axe-core)
- API contract testing (MSW)

### Phase 4: CI/CD Pipeline
- Add E2E tests to GitHub Actions
- Parallel test execution
- Test sharding (split tests across runners)
- Automatic PR comments with test results

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Playwright Best Practices](https://playwright.dev/docs/best-practices)
- [Page Object Model](https://playwright.dev/docs/pom)
- [VS Code Extension](https://playwright.dev/docs/getting-started-vscode)

## Support

For issues or questions:
1. Check Playwright docs
2. Run tests with `--debug` flag
3. Use UI mode for interactive debugging
4. Check test output and screenshots in `test-results/`
