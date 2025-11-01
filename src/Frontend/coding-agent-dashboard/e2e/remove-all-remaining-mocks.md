# Status: Removing All Mocks

## Progress
- ✅ Removed mocks from 9+ test files
- ⏳ 33 mocks remaining across ~10 files

## Remaining Files with Mocks:
1. token-refresh.spec.ts - 4 mocks
2. conversation-creation.spec.ts - 3 mocks  
3. agentic-dashboard.spec.ts - 1 mock
4. browser-automation.spec.ts - 4 mocks
5. cicd-monitoring.spec.ts - 4 mocks
6. ml-classification.spec.ts - 4 mocks
7. offline-detection.spec.ts - 2 mocks
8. chat.spec.ts - Multiple mocks (SignalR, API routes)
9. error-handling.spec.ts - 6 mocks

## Strategy
- Remove all `page.route()` calls
- Update tests to use real APIs
- For error scenarios, use real backend errors or skip tests that require mocking
- Update authentication to use real login

## Next Steps
1. Remove mocks from remaining files
2. Verify all 48 use cases are covered
3. Run E2E tests
4. Fix any failures

