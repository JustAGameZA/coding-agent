# Integration Tests - Real Backend

This directory contains both **E2E tests with mocked APIs** and **Integration tests with real backend services**.

## Test Types

### E2E Tests (Mocked)
- **Files**: `auth.spec.ts`, `dashboard.spec.ts`, `chat.spec.ts`, etc.
- **Backend**: Mocked API responses via Playwright route interception
- **Purpose**: Test UI behavior, form validation, navigation, error handling
- **Speed**: Very fast (~6 seconds for full auth suite)
- **Dependencies**: Only Angular dev server (port 4200)
- **Run**: `npm run test:e2e`

### Integration Tests (Real Backend)
- **File**: `auth.integration.spec.ts`
- **Backend**: Real Gateway → Auth Service → PostgreSQL
- **Purpose**: Verify full stack integration, API contracts, auth flow
- **Speed**: Slower (~30-60 seconds depending on Docker startup)
- **Dependencies**: All Docker services running
- **Run**: `npm run test:integration`

## Prerequisites for Integration Tests

### 1. Start Docker Services
```powershell
cd c:\sourcecode\coding-agent
docker-compose -f deployment/docker-compose/docker-compose.yml `
               -f deployment/docker-compose/docker-compose.apps.dev.yml up -d
```

### 2. Verify Services Are Healthy
```powershell
# Check all containers are running
docker ps --filter "name=coding-agent"

# Verify Gateway health
curl http://localhost:5000/health

# Verify Auth Service health (via Gateway)
curl http://localhost:5000/api/auth/health
```

### 3. Verify Database Migrations Applied
```powershell
# Check Auth Service logs for migration success
docker logs coding-agent-auth-dev --tail 50 | Select-String "migration"

# Should see: "Successfully applied EF Core migrations for AuthDbContext"
```

## Running Integration Tests

### Run All Integration Tests
```bash
npm run test:integration
```

### Run with Browser Visible (Debug)
```bash
npm run test:integration:headed
```

### Run in Debug Mode (Step Through)
```bash
npm run test:integration:debug
```

### Run Specific Test Suite
```bash
npx playwright test auth.integration.spec.ts --project=chromium --grep "Login Flow"
```

## Test Coverage

The integration tests verify:

### ✅ Registration Flow
- [x] Register new user with real backend
- [x] Reject duplicate username (409 Conflict)
- [x] Reject duplicate email (409 Conflict)
- [x] Validate password requirements
- [x] Store JWT token in localStorage
- [x] Redirect to dashboard on success

### ✅ Login Flow
- [x] Login with valid credentials
- [x] Reject invalid credentials (401 Unauthorized)
- [x] Reject non-existent user
- [x] Persist login with "Remember Me" (refresh token)
- [x] Decode JWT and verify claims (username, email, roles)

### ✅ Token Management
- [x] Refresh access token with valid refresh token
- [x] Reject invalid refresh token (401)
- [x] JWT structure validation (header.payload.signature)
- [x] Verify JWT claims (sub, username, email, roles, exp, iat, iss, aud)

### ✅ Protected Routes
- [x] Access /auth/me with valid token (200)
- [x] Reject /auth/me without token (401)
- [x] Reject /auth/me with invalid token (401)
- [x] Redirect to login when accessing dashboard without token
- [x] Access dashboard with valid token

### ✅ Logout Flow
- [x] Logout clears tokens from localStorage
- [x] Redirect to login after logout

### ✅ Error Handling
- [x] Handle malformed request data (400 Bad Request)
- [x] Handle validation errors from backend (400)
- [x] Display user-friendly error messages

## Troubleshooting

### Tests Fail with "Connection Refused" or 502 Errors

**Cause**: Backend services not running or Auth Service build failed

**Solution**:
```powershell
# Check container status
docker ps -a --filter "name=auth-dev"

# If Exited, check logs
docker logs coding-agent-auth-dev --tail 100

# Rebuild if necessary
cd c:\sourcecode\coding-agent
docker-compose -f deployment/docker-compose/docker-compose.yml `
               -f deployment/docker-compose/docker-compose.apps.dev.yml `
               up -d --build auth-service
```

### Tests Fail with "Database Connection Error"

**Cause**: PostgreSQL not running or migrations not applied

**Solution**:
```powershell
# Check PostgreSQL container
docker ps --filter "name=postgres"

# Check Auth Service applied migrations
docker logs coding-agent-auth-dev | Select-String "migration"

# If migrations not applied, restart Auth Service
docker restart coding-agent-auth-dev
```

### Tests Timeout Waiting for Dashboard Redirect

**Cause**: Angular dev server not running or too slow

**Solution**:
```powershell
# Start Angular dev server in separate terminal
cd c:\sourcecode\coding-agent\src\Frontend\coding-agent-dashboard
npm start

# Or increase timeout in test:
await page.waitForURL('**/dashboard', { timeout: 30000 });
```

### "User already exists" Errors

**Cause**: Test users from previous runs still in database

**Solution**: Tests use unique usernames with timestamps (`test_${Date.now()}`), so this should be rare. If it happens:
```powershell
# Clear auth database (warning: loses all users)
docker exec -it coding-agent-postgres psql -U dev -d coding_agent -c "DELETE FROM auth.users WHERE username LIKE 'test_%';"
```

### Gateway Returns 502 Bad Gateway

**Cause**: Auth Service failed to start due to build errors

**Solution**: See earlier troubleshooting guide in copilot-instructions.md or conversation history for file-scoped namespace fix.

## Continuous Integration

In CI/CD pipelines, run integration tests after E2E tests:

```yaml
# .github/workflows/frontend.yml
- name: Run E2E Tests (Mocked)
  run: npm run test:e2e
  
- name: Start Backend Services
  run: docker-compose up -d
  
- name: Wait for Services
  run: |
    timeout 60 bash -c 'until curl -f http://localhost:5000/health; do sleep 2; done'
  
- name: Run Integration Tests (Real Backend)
  run: npm run test:integration
```

## Best Practices

1. **Use unique test data**: Integration tests use `test_${Date.now()}` for usernames to avoid conflicts
2. **Clean up after tests**: Tests don't explicitly delete users (rely on unique names), but you can add cleanup hooks
3. **Test realistic scenarios**: Integration tests verify the full request/response cycle, including JWT generation, database persistence, and error responses
4. **Keep tests independent**: Each test should work standalone (don't rely on previous test state)
5. **Monitor backend logs**: If tests fail, check Docker logs for Auth Service and Gateway to diagnose issues

## Comparison: E2E vs Integration

| Aspect | E2E (Mocked) | Integration (Real) |
|--------|--------------|-------------------|
| **Speed** | ⚡ Fast (~6s) | 🐢 Slower (~30-60s) |
| **Reliability** | ✅ Very reliable | ⚠️ Depends on backend |
| **Coverage** | UI/UX behavior | Full stack contracts |
| **Dependencies** | Angular only | Docker services |
| **CI Time** | Quick feedback | Thorough validation |
| **Debugging** | Easy (no backend) | Complex (multi-service) |
| **Value** | Catch UI bugs | Catch integration bugs |

**Best Practice**: Run E2E tests frequently during development, run integration tests before commits/PRs.

## Test Results Example

```
Running 24 tests using 1 worker

  ✓ [chromium] › auth.integration.spec.ts:28:3 › Auth Integration - Real Backend › should verify Gateway is running (1.2s)
  ✓ [chromium] › auth.integration.spec.ts:33:3 › Auth Integration - Real Backend › should verify Auth Service is accessible (1.8s)
  ✓ [chromium] › auth.integration.spec.ts:50:5 › Auth Integration - Real Backend › Registration Flow - Real API › should register a new user (3.4s)
  ✓ [chromium] › auth.integration.spec.ts:75:5 › Auth Integration - Real Backend › Registration Flow - Real API › should reject duplicate username (4.2s)
  ✓ [chromium] › auth.integration.spec.ts:102:5 › Auth Integration - Real Backend › Registration Flow - Real API › should reject duplicate email (4.1s)
  ✓ [chromium] › auth.integration.spec.ts:156:5 › Auth Integration - Real Backend › Login Flow - Real API › should login with valid credentials (2.9s)
  ... (18 more tests)

  24 passed (58.3s)
```

## Next Steps

- [ ] Add integration tests for Chat Service (SignalR connections)
- [ ] Add integration tests for Dashboard Service (task creation, status updates)
- [ ] Add integration tests for GitHub Service (PR creation, commits)
- [ ] Add performance tests (measure auth latency under load)
- [ ] Add security tests (SQL injection, XSS, CSRF)
