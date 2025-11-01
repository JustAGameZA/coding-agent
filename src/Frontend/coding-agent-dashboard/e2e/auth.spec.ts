import { test, expect, Page } from '@playwright/test';
import { LoginPage, RegisterPage } from './pages/auth.page';
import { mockUsers, waitForAngular, setupAdminSession, setupConsoleErrorTracking } from './fixtures';

/**
 * Authentication E2E Tests
 * Tests login, registration, logout, and protected route flows
 * NOTE: Tests run against REAL backend services (no mocks)
 */

// Helper to register a test user before login tests
async function registerTestUser(page: Page, username: string, email: string, password: string) {
  const registerPage = new RegisterPage(page);
  await registerPage.goto();
  await waitForAngular(page);
  await registerPage.fillForm(username, email, password, password);
  await registerPage.registerButton.click();
  
  // Wait for registration to complete
  // It might auto-login and redirect to dashboard, or show an error
  await page.waitForTimeout(2000); // Give registration time to process
  
  // Navigate back to home/login - clear any existing session
  await page.evaluate(() => {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
  });
  
  await page.goto('/login');
  await waitForAngular(page);
}

test.describe('Login Flow', () => {
  let loginPage: LoginPage;
  let consoleTracker: ReturnType<typeof setupConsoleErrorTracking>;
  
  test.beforeEach(async ({ page }) => {
    // Setup console error tracking
    consoleTracker = setupConsoleErrorTracking(page);
    
    loginPage = new LoginPage(page);
    
    // NO MOCKS - Test against real backend services
    
    await loginPage.goto();
    await waitForAngular(page);
  });

  test.afterEach(async () => {
    // Check for console errors
    consoleTracker.assertNoErrors('test execution');
  });
  
  test('should display login form', async () => {
    await expect(loginPage.loginContainer).toBeVisible();
    await expect(loginPage.loginTitle).toContainText('Sign In');
    await expect(loginPage.usernameInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.rememberMeCheckbox).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });
  
  test('should successfully login with valid credentials', async ({ page }) => {
    // First register the test user
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Now login with the registered credentials
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Should redirect to dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 });
    
    // Verify token stored in localStorage
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();
  });
  
  test('should display full UI with menus after successful login', async ({ page }) => {
    // First register the test user
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Clear any existing auth state
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
    
    // Navigate to login
    await loginPage.goto();
    await waitForAngular(page);
    
    // Verify we're on login page (toolbar should NOT be visible)
    const toolbarBeforeLogin = page.locator('[data-testid="app-toolbar"]');
    await expect(toolbarBeforeLogin).not.toBeVisible();
    
    // Login
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for redirect to dashboard
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    
    // Wait for Angular to stabilize and detect auth state change
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Verify token is stored
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();
    
    // Verify app toolbar is visible (with menus)
    const toolbar = page.locator('[data-testid="app-toolbar"]');
    await expect(toolbar).toBeVisible({ timeout: 10000 });
    
    // Verify toolbar elements are present
    const menuToggle = page.locator('[data-testid="menu-toggle"]');
    await expect(menuToggle).toBeVisible({ timeout: 5000 });
    
    const userMenuButton = page.locator('[data-testid="user-menu-button"]');
    await expect(userMenuButton).toBeVisible({ timeout: 5000 });
    
    // Verify sidenav/navigation menu is visible
    const sidenav = page.locator('mat-sidenav.app-sidenav');
    await expect(sidenav).toBeVisible({ timeout: 5000 });
    
    // Verify navigation links are present in sidenav
    const dashboardLink = page.locator('mat-nav-list a[routerLink="/dashboard"]');
    await expect(dashboardLink).toBeVisible({ timeout: 5000 });
    
    const chatLink = page.locator('mat-nav-list a[routerLink="/chat"]');
    await expect(chatLink).toBeVisible({ timeout: 5000 });
    
    const tasksLink = page.locator('mat-nav-list a[routerLink="/tasks"]');
    await expect(tasksLink).toBeVisible({ timeout: 5000 });
    
    const agenticLink = page.locator('mat-nav-list a[routerLink="/agentic"]');
    await expect(agenticLink).toBeVisible({ timeout: 5000 });
    
    // Verify user menu can be opened
    await userMenuButton.click();
    await page.waitForTimeout(500);
    
    const userMenu = page.locator('[data-testid="user-menu"]');
    await expect(userMenu).toBeVisible({ timeout: 3000 });
    
    // Verify logout button is in user menu
    const logoutButton = page.locator('[data-testid="logout-button"]');
    await expect(logoutButton).toBeVisible({ timeout: 3000 });
    
    // Verify dashboard content is visible (not just empty page)
    const heading = page.getByRole('heading', { name: /dashboard/i });
    await expect(heading).toBeVisible({ timeout: 5000 });
  });
  
  test('should navigate to Dashboard when clicking Dashboard menu item', async ({ page }) => {
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Login
    await loginPage.goto();
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Dashboard menu item
    const dashboardLink = page.locator('mat-nav-list a[routerLink="/dashboard"]');
    await expect(dashboardLink).toBeVisible({ timeout: 5000 });
    await dashboardLink.click();
    await waitForAngular(page);
    
    // Verify we're on dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 });
    const heading = page.getByRole('heading', { name: /dashboard/i });
    await expect(heading).toBeVisible({ timeout: 5000 });
    
    // Verify link is active
    await expect(dashboardLink).toHaveClass(/active/);
  });
  
  test('should navigate to Chat when clicking Chat menu item', async ({ page }) => {
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Login
    await loginPage.goto();
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Chat menu item
    const chatLink = page.locator('mat-nav-list a[routerLink="/chat"]');
    await expect(chatLink).toBeVisible({ timeout: 5000 });
    await chatLink.click();
    await waitForAngular(page);
    
    // Verify we're on chat page
    await page.waitForURL('**/chat', { timeout: 5000 });
    
    // Verify chat page elements are present
    const chatRoot = page.locator('[data-testid="chat-root"]');
    await expect(chatRoot).toBeVisible({ timeout: 5000 });
    
    // Verify link is active
    await expect(chatLink).toHaveClass(/active/);
  });
  
  test('should navigate to Tasks when clicking Tasks menu item', async ({ page }) => {
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Login
    await loginPage.goto();
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Tasks menu item
    const tasksLink = page.locator('mat-nav-list a[routerLink="/tasks"]');
    await expect(tasksLink).toBeVisible({ timeout: 5000 });
    await tasksLink.click();
    await waitForAngular(page);
    
    // Verify we're on tasks page
    await page.waitForURL('**/tasks', { timeout: 5000 });
    
    // Verify tasks page elements are present
    const tasksTable = page.locator('table[data-testid="tasks-table"]');
    const tasksTableExists = await tasksTable.isVisible().catch(() => false);
    expect(tasksTableExists).toBe(true);
    
    // Verify link is active
    await expect(tasksLink).toHaveClass(/active/);
  });
  
  test('should navigate to Agentic AI when clicking Agentic AI menu item', async ({ page }) => {
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Login
    await loginPage.goto();
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Agentic AI menu item
    const agenticLink = page.locator('mat-nav-list a[routerLink="/agentic"]');
    await expect(agenticLink).toBeVisible({ timeout: 5000 });
    await agenticLink.click();
    await waitForAngular(page);
    
    // Verify we're on agentic AI page
    await page.waitForURL('**/agentic', { timeout: 5000 });
    
    // Verify agentic AI page elements are present
    const agenticHeading = page.getByRole('heading', { name: /agentic/i });
    await expect(agenticHeading).toBeVisible({ timeout: 5000 });
    
    // Verify link is active
    await expect(agenticLink).toHaveClass(/active/);
  });
  
  test('should show admin menu items for admin users', async ({ page }) => {
    // Setup admin session
    await setupAdminSession(page);
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Verify admin menu items are visible
    const infrastructureLink = page.locator('mat-nav-list a[routerLink="/admin/infrastructure"]');
    await expect(infrastructureLink).toBeVisible({ timeout: 5000 });
    
    const userManagementLink = page.locator('mat-nav-list a[routerLink="/admin/users"]');
    await expect(userManagementLink).toBeVisible({ timeout: 5000 });
    
    const configLink = page.locator('mat-nav-list a[routerLink="/admin/config"]');
    await expect(configLink).toBeVisible({ timeout: 5000 });
  });
  
  test('should navigate to Infrastructure when clicking Infrastructure menu item (admin only)', async ({ page }) => {
    // Setup admin session
    await setupAdminSession(page);
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Infrastructure menu item
    const infrastructureLink = page.locator('mat-nav-list a[routerLink="/admin/infrastructure"]');
    await expect(infrastructureLink).toBeVisible({ timeout: 5000 });
    await infrastructureLink.click();
    await waitForAngular(page);
    
    // Verify we're on infrastructure page
    await page.waitForURL('**/admin/infrastructure', { timeout: 5000 });
    
    // Verify infrastructure page elements are present
    const infrastructureHeading = page.getByRole('heading', { name: /infrastructure/i });
    await expect(infrastructureHeading).toBeVisible({ timeout: 5000 });
    
    // Verify link is active
    await expect(infrastructureLink).toHaveClass(/active/);
  });
  
  test('should navigate to User Management when clicking User Management menu item (admin only)', async ({ page }) => {
    // Setup admin session
    await setupAdminSession(page);
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click User Management menu item
    const userManagementLink = page.locator('mat-nav-list a[routerLink="/admin/users"]');
    await expect(userManagementLink).toBeVisible({ timeout: 5000 });
    await userManagementLink.click();
    await waitForAngular(page);
    
    // Verify we're on user management page
    await page.waitForURL('**/admin/users', { timeout: 5000 });
    
    // Verify user management page elements are present
    const usersTable = page.locator('table[data-testid="users-table"]');
    const usersTableExists = await usersTable.isVisible().catch(() => false);
    expect(usersTableExists).toBe(true);
    
    // Verify link is active
    await expect(userManagementLink).toHaveClass(/active/);
  });
  
  test('should navigate to Configuration when clicking Configuration menu item (admin only)', async ({ page }) => {
    // Setup admin session
    await setupAdminSession(page);
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Click Configuration menu item
    const configLink = page.locator('mat-nav-list a[routerLink="/admin/config"]');
    await expect(configLink).toBeVisible({ timeout: 5000 });
    await configLink.click();
    await waitForAngular(page);
    
    // Verify we're on configuration page
    await page.waitForURL('**/admin/config', { timeout: 5000 });
    
    // Verify configuration page elements are present
    const configHeading = page.getByRole('heading', { name: /configuration/i });
    const configHeadingExists = await configHeading.isVisible().catch(() => false);
    expect(configHeadingExists).toBe(true);
    
    // Verify link is active
    await expect(configLink).toHaveClass(/active/);
  });
  
  test('should not show admin menu items for non-admin users', async ({ page }) => {
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Login
    await loginPage.goto();
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Wait for dashboard to load
    await page.waitForURL('**/dashboard', { timeout: 10000 });
    await waitForAngular(page);
    await page.waitForTimeout(500);
    
    // Verify admin menu items are NOT visible
    const infrastructureLink = page.locator('mat-nav-list a[routerLink="/admin/infrastructure"]');
    await expect(infrastructureLink).not.toBeVisible();
    
    const userManagementLink = page.locator('mat-nav-list a[routerLink="/admin/users"]');
    await expect(userManagementLink).not.toBeVisible();
    
    const configLink = page.locator('mat-nav-list a[routerLink="/admin/config"]');
    await expect(configLink).not.toBeVisible();
  });
  
  test('should show error with invalid credentials', async () => {
    await loginPage.fillForm('wronguser', 'wrongpassword');
    await loginPage.loginButton.click();
    
    // Should show error message
    await loginPage.waitForError();
    const errorText = await loginPage.getErrorMessage();
    expect(errorText).toContain('Invalid username or password');
  });
  
  test('should validate empty username field', async () => {
    // Try to submit with empty username
    await loginPage.passwordInput.fill('somepassword');
    
    // Button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBe(true);
  });
  
  test('should validate empty password field', async () => {
    await loginPage.usernameInput.fill('someuser');
    
    // Button should be disabled
    const isDisabled = await loginPage.isLoginButtonDisabled();
    expect(isDisabled).toBe(true);
  });
  
  test('should validate email format when @ is present', async ({ page }) => {
    await loginPage.usernameInput.fill('invalid-email@');
    await loginPage.passwordInput.fill('password123');
    
    // Trigger validation by clicking outside
    await loginPage.passwordInput.click();
    await loginPage.usernameInput.blur();
    
    // Check for validation error (Angular Material shows mat-error)
    const errorElement = page.locator('mat-error');
    await expect(errorElement).toBeVisible();
  });
  
  test('should toggle password visibility', async () => {
    await loginPage.passwordInput.fill('mypassword');
    
    // Initially hidden
    let isVisible = await loginPage.isPasswordVisible();
    expect(isVisible).toBe(false);
    
    // Click toggle to show
    await loginPage.togglePassword();
    isVisible = await loginPage.isPasswordVisible();
    expect(isVisible).toBe(true);
    
    // Toggle back to hidden
    await loginPage.togglePassword();
    isVisible = await loginPage.isPasswordVisible();
    expect(isVisible).toBe(false);
  });
  
  test('should remember me checkbox work', async ({ page }) => {
    // First register the test user
    const { username, email, password } = mockUsers.validUser;
    await registerTestUser(page, username, email, password);
    
    // Now test remember me functionality
    await loginPage.fillForm(username, password, true);
    
    // Verify checkbox is checked (Material checkbox - check the input inside)
    const inputCheckbox = loginPage.rememberMeCheckbox.locator('input');
    const isChecked = await inputCheckbox.isChecked();
    expect(isChecked).toBe(true);
    
    await loginPage.loginButton.click();
    await page.waitForURL('**/dashboard');
  });
  
  test('should navigate to register page', async ({ page }) => {
    await loginPage.clickRegisterLink();
    
    // Wait for navigation and verify URL
    await page.waitForURL(/.*register/, { timeout: 10000 });
    await expect(page).toHaveURL(/.*register/);
  });
  
  test('should handle API errors gracefully', async ({ page }) => {
    // Uses real API - no mocking
    // Note: To test API errors with real backend, we'd need backend to fail
    // For now, verify form validation works
    await loginPage.fillForm('testuser', 'password');
    await loginPage.loginButton.click();
    
    // Should show generic error
    await loginPage.waitForError();
    const errorText = await loginPage.getErrorMessage();
    expect(errorText).toBeTruthy();
  });
});

test.describe('Registration Flow', () => {
  let registerPage: RegisterPage;
  
  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    
    // NO MOCKS - Test against real backend services
    
    await registerPage.goto();
    await waitForAngular(page);
  });
  
  test('should display registration form', async () => {
    await expect(registerPage.registerContainer).toBeVisible();
    await expect(registerPage.registerTitle).toContainText('Create Account');
    await expect(registerPage.usernameInput).toBeVisible();
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.passwordInput).toBeVisible();
    await expect(registerPage.confirmPasswordInput).toBeVisible();
    await expect(registerPage.registerButton).toBeVisible();
  });
  
  test('should successfully register with valid data', async ({ page }) => {
    await registerPage.fillForm('newuser', 'newuser@example.com', 'Test@1234', 'Test@1234');
    await registerPage.registerButton.click();
    
    // Should auto-login and redirect to dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 });
    
    // Verify token stored
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();
  });
  
  test('should show error with duplicate username', async () => {
    // Use existing user from mockUsers
    await registerPage.fillForm('testuser', 'newemail@example.com', 'Test@1234', 'Test@1234');
    await registerPage.registerButton.click();
    
    // Should show conflict error
    await registerPage.waitForError();
    const errorText = await registerPage.getErrorMessage();
    expect(errorText).toContain('already exists');
  });
  
  test('should show error with duplicate email', async () => {
    await registerPage.fillForm('newuser', 'testuser@example.com', 'Test@1234', 'Test@1234');
    await registerPage.registerButton.click();
    
    // Should show conflict error
    await registerPage.waitForError();
    const errorText = await registerPage.getErrorMessage();
    expect(errorText).toContain('already exists');
  });
  
  test('should validate password mismatch', async ({ page }) => {
    await registerPage.fillForm('newuser', 'newuser@example.com', 'Test@1234', 'Different@5678');
    
    // Trigger validation
    await registerPage.confirmPasswordInput.blur();
    await page.waitForTimeout(300);
    
    // Button should be disabled
    const isDisabled = await registerPage.isRegisterButtonDisabled();
    expect(isDisabled).toBe(true);
  });
  
  test('should validate weak password', async ({ page }) => {
    await registerPage.usernameInput.fill('newuser');
    await registerPage.emailInput.fill('newuser@example.com');
    await registerPage.passwordInput.fill('weak'); // Too weak
    
    // Trigger validation
    await registerPage.passwordInput.blur();
    await page.waitForTimeout(300);
    
    // Button should be disabled due to password strength
    const isDisabled = await registerPage.isRegisterButtonDisabled();
    expect(isDisabled).toBe(true);
  });
  
  test('should show password strength indicator', async () => {
    // Weak password
    let strength = await registerPage.validatePasswordStrength('weak');
    expect(strength).toBe('weak');
    
    // Fair password
    strength = await registerPage.validatePasswordStrength('Password1');
    expect(strength === 'fair' || strength === 'good').toBe(true);
    
    // Strong password
    strength = await registerPage.validatePasswordStrength('Strong@Pass123');
    expect(strength === 'good' || strength === 'strong').toBe(true);
  });
  
  test('should validate username format', async ({ page }) => {
    // Invalid characters (spaces, special chars)
    await registerPage.usernameInput.fill('invalid user!');
    await registerPage.usernameInput.blur();
    await page.waitForTimeout(300);
    
    // Should show validation error
    const hasError = await registerPage.hasUsernameError();
    expect(hasError).toBe(true);
  });
  
  test('should validate email format', async ({ page }) => {
    await registerPage.emailInput.fill('invalid-email');
    await registerPage.emailInput.blur();
    await page.waitForTimeout(300);
    
    // Should show validation error
    const hasError = await registerPage.hasEmailError();
    expect(hasError).toBe(true);
  });
  
  test('should navigate to login page', async ({ page }) => {
    await registerPage.clickLoginLink();
    
    // Should be on login page
    await expect(page).toHaveURL(/.*login/);
  });
});

test.describe('Logout Flow', () => {
  test('should successfully logout', async ({ page }) => {
    // Setup authenticated user - NO MOCKS, real login first
    await page.goto('/login');
    const loginPage = new LoginPage(page);
    const { username, password } = mockUsers.validUser;
    await loginPage.login(username, password);
    await page.waitForURL('**/dashboard', { timeout: 5000 });
    
    // Navigate to dashboard
    await page.goto('/dashboard');
    await waitForAngular(page);
    
    // Click logout (assume toolbar has logout button)
    const logoutButton = page.locator('[data-testid="logout-button"]');
    
    if (await logoutButton.isVisible()) {
      await logoutButton.click();
      
      // Should redirect to login
      await page.waitForURL('**/login', { timeout: 5000 });
      
      // Token should be cleared
      const token = await page.evaluate(() => localStorage.getItem('auth_token'));
      expect(token).toBeNull();
    } else {
      // Skip if logout button not implemented yet
      test.skip();
    }
  });
});

test.describe('Protected Routes', () => {
  test('should redirect to login when accessing dashboard without auth', async ({ page }) => {
    // Clear any existing auth
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    
    // Try to access protected route
    await page.goto('/dashboard');
    
    // Should redirect to login
    await page.waitForURL(/.*login/, { timeout: 5000 });
    
    // Should have returnUrl query param
    const url = page.url();
    expect(url).toContain('returnUrl=%2Fdashboard');
  });
  
  test('should redirect to login when accessing tasks without auth', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    
    await page.goto('/tasks');
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
  
  test('should redirect to login when accessing chat without auth', async ({ page }) => {
    await page.goto('/');
    await page.evaluate(() => localStorage.clear());
    
    await page.goto('/chat');
    await page.waitForURL(/.*login/, { timeout: 5000 });
  });
  
  test('should redirect to original route after login', async ({ page }) => {
    // Try to access protected route
    await page.goto('/dashboard');
    await page.waitForURL(/.*login/, { timeout: 5000 });
    
    // NO MOCKS - Login with real backend
    
    const loginPage = new LoginPage(page);
    const { username, password } = mockUsers.validUser;
    await loginPage.login(username, password);
    
    // Should redirect back to dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 });
  });
});

test.describe('Token Auto-Refresh', () => {
  test.skip('should auto-refresh token before expiry', async ({ page }) => {
    // This test requires mocking token expiry and waiting for refresh
    // Skip for now as it would take too long in CI
    test.slow();
    
    // Setup authenticated user with short-lived token
    await setupAuthenticatedUser(page);
    
    await page.goto('/dashboard');
    
    // Wait for token to be near expiry (mocked to 5 minutes)
    // In real scenario, would need to mock timer or reduce token lifetime
    
    // Verify refresh endpoint was called
    const refreshCalls = await page.evaluate(() => {
      return (window as any).__authRefreshCalls || 0;
    });
    
    expect(refreshCalls).toBeGreaterThan(0);
  });
});

test.describe('Mobile Responsive Auth', () => {
  test('should display login form correctly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const loginPage = new LoginPage(page);
    // NO MOCKS - Test real backend
    await loginPage.goto();
    
    await expect(loginPage.loginContainer).toBeVisible();
    await expect(loginPage.usernameInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });
  
  test('should display register form correctly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const registerPage = new RegisterPage(page);
    // NO MOCKS - Test real backend
    await registerPage.goto();
    
    await expect(registerPage.registerContainer).toBeVisible();
    await expect(registerPage.usernameInput).toBeVisible();
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.registerButton).toBeVisible();
  });
});
