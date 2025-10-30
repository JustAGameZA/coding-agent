# Quick Start: Admin E2E Tests

## Prerequisites Check

### ✅ Services Running
```powershell
# Check all containers
docker ps

# Expected containers:
# - coding-agent-postgres (port 5432)
# - coding-agent-gateway (port 5000)
# - coding-agent-auth (port 5001)
# - coding-agent-dashboard-bff (port 5007)
```

### ✅ Start Services (if not running)
```powershell
cd c:\sourcecode\coding-agent
docker compose up -d
```

### ✅ Seed Admin User
```powershell
# This will:
# 1. Register 'admin' user via API
# 2. Elevate to Admin role in database
# 3. Verify creation

powershell -ExecutionPolicy Bypass -File seed-admin-user.ps1
```

**Admin Credentials**:
- Username: `admin`
- Password: `Admin@1234!`
- URL: `http://localhost:4200/login`

---

## Running the Tests

### Option 1: Run All Admin Tests
```powershell
cd src\Frontend\coding-agent-dashboard
npx playwright test e2e/admin.spec.ts e2e/grafana.spec.ts --headed
```

### Option 2: Run Specific Test Suite
```powershell
# Admin functionality only
npx playwright test e2e/admin.spec.ts --headed

# Grafana integration only
npx playwright test e2e/grafana.spec.ts --headed
```

### Option 3: Run with Playwright UI (Interactive)
```powershell
npx playwright test e2e/admin.spec.ts --ui
```

### Option 4: Run Specific Test
```powershell
# Run single test by name
npx playwright test e2e/admin.spec.ts --headed -g "should display infrastructure page"
```

---

## Test Coverage

### Admin Tests (admin.spec.ts)
- ✅ Route guards (unauthorized access)
- ✅ Infrastructure page with 5 monitoring tools
- ✅ User management page with search
- ✅ Edit user roles
- ✅ Activate/deactivate users

### Grafana Tests (grafana.spec.ts)
- ✅ Grafana card display
- ✅ Correct URL configuration
- ✅ Link attributes (security)
- ✅ All infrastructure tools validation
- ✅ Mobile responsiveness

---

## Troubleshooting

### Issue: "Admin user not found"
**Solution**: Run seed script
```powershell
powershell -ExecutionPolicy Bypass -File seed-admin-user.ps1
```

### Issue: "502 Bad Gateway"
**Solution**: Start Auth service
```powershell
docker compose up -d auth-service gateway
docker ps | findstr auth
```

### Issue: "Tests timing out"
**Solution**: Increase timeout in playwright.config.ts or check if services are responding
```powershell
# Test Gateway
curl http://localhost:5000/health

# Test Dashboard BFF
curl http://localhost:5007/health

# Test Auth
curl http://localhost:5001/health
```

### Issue: "Cannot find page objects"
**Solution**: Verify files exist
```powershell
Get-ChildItem -Path "src\Frontend\coding-agent-dashboard\e2e\pages" -Recurse
```

---

## Database Cleanup

### Clean Up Test Users (After Test Runs)
```powershell
# This will delete all test users created during E2E tests
powershell -ExecutionPolicy Bypass -File cleanup-test-users.ps1

# Type 'yes' when prompted
```

### Verify Database State
```powershell
# Count all users
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "SELECT COUNT(*) FROM auth.users;"

# Show all users
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "SELECT username, email, roles, is_active FROM auth.users ORDER BY created_at DESC;"
```

---

## Expected Results

### Successful Test Run Output
```
Running 23 tests using 1 worker

✓ Admin Route Guards › non-admin user should be redirected (1.5s)
✓ Admin Route Guards › unauthenticated user should be redirected (0.8s)
✓ Admin Infrastructure Page › should display infrastructure page (1.2s)
✓ Admin Infrastructure Page › should have correct URLs (0.9s)
✓ Admin Infrastructure Page › links should open in new tab (0.7s)
✓ Admin User Management › should display user list table (1.8s)
✓ Admin User Management › should search users by username (1.4s)
✓ Admin User Management › should edit user roles (2.3s)
✓ Admin User Management › should deactivate user (2.1s)
✓ Admin User Management › should activate user (2.5s)
...

23 passed (35s)
```

---

## Quick Commands Reference

| Command | Purpose |
|---------|---------|
| `docker ps` | Check running services |
| `docker compose up -d` | Start all services |
| `.\seed-admin-user.ps1` | Create admin user |
| `npx playwright test e2e/admin.spec.ts --headed` | Run admin tests |
| `npx playwright test e2e/admin.spec.ts --ui` | Interactive test runner |
| `.\cleanup-test-users.ps1` | Delete test users |
| `npx playwright show-report` | View test results |

---

## Files Created

✅ **Test Files**:
- `src/Frontend/coding-agent-dashboard/e2e/admin.spec.ts`
- `src/Frontend/coding-agent-dashboard/e2e/grafana.spec.ts`

✅ **Page Objects**:
- `src/Frontend/coding-agent-dashboard/e2e/pages/admin.page.ts`

✅ **Scripts**:
- `cleanup-test-users.ps1`
- `seed-admin-user.ps1`

✅ **Documentation**:
- `E2E-ADMIN-TESTS-SUMMARY.md`
- `E2E-ADMIN-TESTS-QUICK-START.md` (this file)

---

## Next Steps

1. ✅ Start Docker services
2. ✅ Seed admin user
3. ✅ Run tests
4. ✅ Review results
5. ✅ Clean up test data

**Happy Testing! 🧪**
