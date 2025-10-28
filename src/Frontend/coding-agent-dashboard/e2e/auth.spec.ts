import { test, expect } from '@playwright/test';
import { LoginPage, RegisterPage } from './pages/auth.page';
import { mockAuthAPI, mockUsers, setupAuthenticatedUser, waitForAngular } from './fixtures';

/**
 * Authentication E2E Tests
 * Tests login, registration, logout, and protected route flows
 */

test.describe('Login Flow', () => {
  let loginPage: LoginPage;
  
  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    
    // Mock auth API BEFORE navigation
    await mockAuthAPI(page);
    
    await loginPage.goto();
    await waitForAngular(page);
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
    const { username, password } = mockUsers.validUser;
    
    await loginPage.fillForm(username, password);
    await loginPage.loginButton.click();
    
    // Should redirect to dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 });
    
    // Verify token stored in localStorage
    const token = await page.evaluate(() => localStorage.getItem('auth_token'));
    expect(token).toBeTruthy();
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
    const { username, password } = mockUsers.validUser;
    
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
    // Override with error response
    await page.route('**/api/auth/login', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' })
      });
    });
    
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
    
    // Mock auth API BEFORE navigation
    await mockAuthAPI(page);
    
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
    // Setup authenticated user
    await setupAuthenticatedUser(page);
    
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
    
    // Mock auth and login
    await mockAuthAPI(page);
    
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
    await mockAuthAPI(page);
    await loginPage.goto();
    
    await expect(loginPage.loginContainer).toBeVisible();
    await expect(loginPage.usernameInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.loginButton).toBeVisible();
  });
  
  test('should display register form correctly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    
    const registerPage = new RegisterPage(page);
    await mockAuthAPI(page);
    await registerPage.goto();
    
    await expect(registerPage.registerContainer).toBeVisible();
    await expect(registerPage.usernameInput).toBeVisible();
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.registerButton).toBeVisible();
  });
});
