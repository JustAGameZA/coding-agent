import { test, expect, Page } from '@playwright/test';
import { InfrastructurePage, UserManagementPage, UserEditDialogPage } from './pages/admin.page';
import { LoginPage } from './pages/auth.page';
import { waitForAngular } from './fixtures';

/**
 * Admin E2E Tests
 * Tests admin-only features: infrastructure monitoring links and user management
 * NOTE: Tests run against REAL backend services (no mocks)
 */

// Helper to create admin test user
async function createAdminUser(page: Page): Promise<{ username: string; email: string; password: string }> {
  const timestamp = Date.now();
  const adminUser = {
    username: `e2eadmin_${timestamp}`,
    email: `e2eadmin_${timestamp}@example.com`,
    password: 'AdminE2E@1234!'
  };

  // Register admin user via API (backend will assign User role by default)
  const response = await page.request.post('/api/auth/register', {
    data: adminUser
  });

  if (!response.ok()) {
    throw new Error(`Failed to register admin user: ${response.status()}`);
  }

  // Note: In production, admin role would need to be assigned manually via database
  // For E2E tests, we assume there's a seeded admin user or role elevation API
  
  return adminUser;
}

// Helper to create regular test user
async function createRegularUser(page: Page): Promise<{ username: string; email: string; password: string }> {
  const timestamp = Date.now();
  const regularUser = {
    username: `e2euser_${timestamp}`,
    email: `e2euser_${timestamp}@example.com`,
    password: 'UserE2E@1234!'
  };

  const response = await page.request.post('/api/auth/register', {
    data: regularUser
  });

  if (!response.ok()) {
    throw new Error(`Failed to register regular user: ${response.status()}`);
  }

  return regularUser;
}

// Helper to login
async function login(page: Page, username: string, password: string) {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await waitForAngular(page);
  await loginPage.login(username, password);
  await page.waitForLoadState('networkidle');
}

test.describe('Admin Route Guards', () => {
  test('non-admin user should be redirected when accessing admin pages', async ({ page }) => {
    // Create and login as regular user
    const regularUser = await createRegularUser(page);
    await login(page, regularUser.username, regularUser.password);

    // Try to access admin infrastructure page
    await page.goto('/admin/infrastructure');
    await waitForAngular(page);

    // Should be redirected (either to dashboard or show access denied)
    // Check that we're NOT on the admin page
    const url = page.url();
    const isOnAdminPage = url.includes('/admin/infrastructure');
    
    if (isOnAdminPage) {
      // If still on admin page, check for access denied message
      const accessDenied = page.locator('text=/access denied|unauthorized|forbidden/i');
      await expect(accessDenied).toBeVisible({ timeout: 5000 });
    } else {
      // Redirected away from admin page
      expect(url).not.toContain('/admin/infrastructure');
    }

    // Try to access admin users page
    await page.goto('/admin/users');
    await waitForAngular(page);

    const usersUrl = page.url();
    const isOnUsersPage = usersUrl.includes('/admin/users');
    
    if (isOnUsersPage) {
      const accessDenied = page.locator('text=/access denied|unauthorized|forbidden/i');
      await expect(accessDenied).toBeVisible({ timeout: 5000 });
    } else {
      expect(usersUrl).not.toContain('/admin/users');
    }
  });

  test('unauthenticated user should be redirected to login', async ({ page }) => {
    // Clear any existing auth
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());

    // Try to access admin page
    await page.goto('/admin/infrastructure');
    await waitForAngular(page);

    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
    expect(page.url()).toContain('login');
  });
});

test.describe('Admin Infrastructure Page', () => {
  test.beforeEach(async ({ page }) => {
    // For these tests, use the seeded admin user (username: 'admin', password: 'Admin@1234!')
    // This user should be created by the database seed script
    await login(page, 'admin', 'Admin@1234!');
  });

  test('should display infrastructure page with all monitoring tools', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Verify page elements
    await expect(infrastructurePage.infrastructureContainer).toBeVisible();
    await expect(infrastructurePage.pageTitle).toBeVisible();
    await expect(infrastructurePage.subtitle).toBeVisible();

    // Verify all 5 cards are present
    const cardCount = await infrastructurePage.getCardCount();
    expect(cardCount).toBe(5);

    // Verify specific cards
    await expect(infrastructurePage.grafanaCard).toBeVisible();
    await expect(infrastructurePage.seqCard).toBeVisible();
    await expect(infrastructurePage.jaegerCard).toBeVisible();
    await expect(infrastructurePage.prometheusCard).toBeVisible();
    await expect(infrastructurePage.rabbitmqCard).toBeVisible();
  });

  test('should have correct URLs for all infrastructure tools', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Verify URLs
    const grafanaLink = await infrastructurePage.getCardLink('Grafana');
    expect(grafanaLink).toBe('http://localhost:3000');

    const seqLink = await infrastructurePage.getCardLink('Seq');
    expect(seqLink).toBe('http://localhost:5341');

    const jaegerLink = await infrastructurePage.getCardLink('Jaeger');
    expect(jaegerLink).toBe('http://localhost:16686');

    const prometheusLink = await infrastructurePage.getCardLink('Prometheus');
    expect(prometheusLink).toBe('http://localhost:9090');

    const rabbitmqLink = await infrastructurePage.getCardLink('RabbitMQ');
    expect(rabbitmqLink).toBe('http://localhost:15672');
  });

  test('infrastructure card links should open in new tab', async ({ page }) => {
    const infrastructurePage = new InfrastructurePage(page);
    await infrastructurePage.goto();
    await waitForAngular(page);

    // Check that links have target="_blank" and rel="noopener noreferrer"
    const grafanaLink = infrastructurePage.grafanaCard.locator('a');
    
    const target = await grafanaLink.getAttribute('target');
    expect(target).toBe('_blank');

    const rel = await grafanaLink.getAttribute('rel');
    expect(rel).toBe('noopener noreferrer');
  });
});

test.describe('Admin User Management Page', () => {
  test.beforeEach(async ({ page }) => {
    // Use seeded admin user
    await login(page, 'admin', 'Admin@1234!');
  });

  test('should display user management page with user list table', async ({ page }) => {
    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Verify page elements
    await expect(userManagementPage.userListContainer).toBeVisible();
    await expect(userManagementPage.pageTitle).toBeVisible();
    await expect(userManagementPage.subtitle).toBeVisible();

    // Verify search toolbar
    await expect(userManagementPage.searchField).toBeVisible();
    await expect(userManagementPage.searchButton).toBeVisible();

    // Verify table is present
    await expect(userManagementPage.userTable).toBeVisible();

    // Verify paginator
    await expect(userManagementPage.paginator).toBeVisible();
  });

  test('should display user rows with username, email, roles, and status', async ({ page }) => {
    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Should have at least the admin user
    const userCount = await userManagementPage.getUserCount();
    expect(userCount).toBeGreaterThan(0);

    // Find admin user row
    const adminRow = await userManagementPage.getUserByUsername('admin');
    expect(adminRow).not.toBeNull();

    if (adminRow) {
      // Verify admin has email
      await expect(adminRow).toContainText('admin@codingagent.local');

      // Verify admin has Admin role
      const roles = await userManagementPage.getUserRoles('admin');
      expect(roles).toContain('Admin');

      // Verify status is Active
      const status = await userManagementPage.getUserStatus('admin');
      expect(status).toBe('Active');
    }
  });

  test('should search users by username', async ({ page }) => {
    // Create a test user first
    const testUser = await createRegularUser(page);
    
    // Login as admin
    await login(page, 'admin', 'Admin@1234!');

    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Search for the test user
    await userManagementPage.search(testUser.username);

    // Verify test user appears in results
    const foundUser = await userManagementPage.getUserByUsername(testUser.username);
    expect(foundUser).not.toBeNull();
  });

  test('should search users by email', async ({ page }) => {
    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Search for admin email
    await userManagementPage.search('admin@codingagent.local');

    // Verify admin user appears
    const foundUser = await userManagementPage.getUserByUsername('admin');
    expect(foundUser).not.toBeNull();
  });

  test('should clear search and reload all users', async ({ page }) => {
    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Get initial count
    const initialCount = await userManagementPage.getUserCount();

    // Search for something specific
    await userManagementPage.search('admin');
    const searchCount = await userManagementPage.getUserCount();

    // Clear search
    await userManagementPage.clearSearch();
    const afterClearCount = await userManagementPage.getUserCount();

    // Should be back to showing all users
    expect(afterClearCount).toBeGreaterThanOrEqual(searchCount);
  });

  test('should open edit roles dialog', async ({ page }) => {
    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Click edit on admin user
    await userManagementPage.clickEditRoles('admin');

    // Verify dialog opens
    const editDialog = new UserEditDialogPage(page);
    await editDialog.waitForDialog();
    await expect(editDialog.dialog).toBeVisible();
    await expect(editDialog.dialogTitle).toBeVisible();

    // Close dialog
    await editDialog.cancel();
  });

  test('should edit user roles', async ({ page }) => {
    // Create a test user
    const testUser = await createRegularUser(page);

    // Login as admin
    await login(page, 'admin', 'Admin@1234!');

    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Search for test user
    await userManagementPage.search(testUser.username);

    // Get initial roles
    const initialRoles = await userManagementPage.getUserRoles(testUser.username);
    const hadAdminRole = initialRoles.includes('Admin');

    // Click edit roles
    await userManagementPage.clickEditRoles(testUser.username);

    const editDialog = new UserEditDialogPage(page);
    await editDialog.waitForDialog();

    // Toggle Admin role
    await editDialog.toggleAdmin();

    // Save changes
    await editDialog.save();

    // Wait for snackbar notification
    await page.waitForTimeout(1000);

    // Reload and verify role changed
    await userManagementPage.search(testUser.username);
    const updatedRoles = await userManagementPage.getUserRoles(testUser.username);
    const hasAdminRole = updatedRoles.includes('Admin');

    // Role should have toggled
    expect(hasAdminRole).toBe(!hadAdminRole);
  });

  test('should deactivate user', async ({ page }) => {
    // Create a test user
    const testUser = await createRegularUser(page);

    // Login as admin
    await login(page, 'admin', 'Admin@1234!');

    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Search for test user
    await userManagementPage.search(testUser.username);

    // Verify user is active
    let status = await userManagementPage.getUserStatus(testUser.username);
    expect(status).toBe('Active');

    // Deactivate user
    await userManagementPage.clickDeactivate(testUser.username);

    // Wait for snackbar
    await page.waitForTimeout(1000);

    // Reload and verify status changed
    await userManagementPage.search(testUser.username);
    status = await userManagementPage.getUserStatus(testUser.username);
    expect(status).toBe('Inactive');
  });

  test('should activate user', async ({ page }) => {
    // Create a test user
    const testUser = await createRegularUser(page);

    // Login as admin
    await login(page, 'admin', 'Admin@1234!');

    const userManagementPage = new UserManagementPage(page);
    await userManagementPage.goto();
    await waitForAngular(page);
    await userManagementPage.waitForTableLoad();

    // Search and deactivate user first
    await userManagementPage.search(testUser.username);
    await userManagementPage.clickDeactivate(testUser.username);
    await page.waitForTimeout(1000);

    // Now reactivate
    await userManagementPage.search(testUser.username);
    await userManagementPage.clickActivate(testUser.username);
    await page.waitForTimeout(1000);

    // Verify status is Active
    await userManagementPage.search(testUser.username);
    const status = await userManagementPage.getUserStatus(testUser.username);
    expect(status).toBe('Active');
  });
});

test.describe('Admin Navigation', () => {
  test('should navigate between admin pages', async ({ page }) => {
    // Login as admin
    await login(page, 'admin', 'Admin@1234!');

    // Navigate to infrastructure page
    await page.goto('/admin/infrastructure');
    await waitForAngular(page);
    expect(page.url()).toContain('/admin/infrastructure');

    // Navigate to user management
    await page.goto('/admin/users');
    await waitForAngular(page);
    expect(page.url()).toContain('/admin/users');

    // Navigate back to infrastructure
    await page.goto('/admin/infrastructure');
    await waitForAngular(page);
    expect(page.url()).toContain('/admin/infrastructure');
  });
});
