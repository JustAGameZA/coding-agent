import { Page, Locator } from '@playwright/test';

/**
 * Login Page Object
 * Encapsulates selectors and actions for the Login page
 */
export class LoginPage {
  readonly page: Page;
  
  // Form elements
  readonly loginContainer: Locator;
  readonly loginCard: Locator;
  readonly loginTitle: Locator;
  readonly loginForm: Locator;
  readonly usernameInput: Locator;
  readonly passwordInput: Locator;
  readonly rememberMeCheckbox: Locator;
  readonly loginButton: Locator;
  readonly errorMessage: Locator;
  readonly togglePasswordVisibility: Locator;
  
  // Links
  readonly forgotPasswordLink: Locator;
  readonly registerLink: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    // Main elements
    this.loginContainer = page.locator('[data-testid="login-container"]');
    this.loginCard = page.locator('[data-testid="login-card"]');
    this.loginTitle = page.locator('[data-testid="login-title"]');
    this.loginForm = page.locator('[data-testid="login-form"]');
    
    // Form inputs
    this.usernameInput = page.locator('[data-testid="username-input"]');
    this.passwordInput = page.locator('[data-testid="password-input"]');
    this.rememberMeCheckbox = page.locator('[data-testid="remember-me-checkbox"]');
    this.loginButton = page.locator('[data-testid="login-button"]');
    this.togglePasswordVisibility = page.locator('[data-testid="toggle-password-visibility"]');
    
    // Error and links
    this.errorMessage = page.locator('[data-testid="error-message"]');
    this.forgotPasswordLink = page.locator('[data-testid="forgot-password-link"]');
    this.registerLink = page.locator('[data-testid="register-link"]');
  }
  
  async goto() {
    await this.page.goto('/login');
    await this.page.waitForLoadState('networkidle');
  }
  
  async login(username: string, password: string, rememberMe: boolean = false) {
    await this.fillForm(username, password, rememberMe);
    await this.loginButton.click();
    
    // Wait for navigation or error
    await this.page.waitForLoadState('networkidle');
  }
  
  async fillForm(username: string, password: string, rememberMe: boolean = false) {
    await this.usernameInput.fill(username);
    await this.passwordInput.fill(password);
    
    if (rememberMe) {
      // Material checkbox requires clicking the label or input inside
      await this.rememberMeCheckbox.locator('input').check();
    }
  }
  
  async getErrorMessage(): Promise<string> {
    await this.errorMessage.waitFor({ state: 'visible', timeout: 5000 });
    return await this.errorMessage.textContent() || '';
  }
  
  async waitForError() {
    await this.errorMessage.waitFor({ state: 'visible', timeout: 5000 });
  }
  
  async isLoginButtonDisabled(): Promise<boolean> {
    return await this.loginButton.isDisabled();
  }
  
  async togglePassword() {
    await this.togglePasswordVisibility.click();
  }
  
  async isPasswordVisible(): Promise<boolean> {
    const type = await this.passwordInput.getAttribute('type');
    return type === 'text';
  }
  
  async clickRegisterLink() {
    await this.registerLink.click();
  }
  
  async getCurrentUrl(): Promise<string> {
    return this.page.url();
  }
}

/**
 * Register Page Object
 * Encapsulates selectors and actions for the Register page
 */
export class RegisterPage {
  readonly page: Page;
  
  // Form elements
  readonly registerContainer: Locator;
  readonly registerCard: Locator;
  readonly registerTitle: Locator;
  readonly registerForm: Locator;
  readonly usernameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly registerButton: Locator;
  readonly errorMessage: Locator;
  readonly togglePasswordVisibility: Locator;
  readonly toggleConfirmPasswordVisibility: Locator;
  
  // Password strength
  readonly passwordStrengthBar: Locator;
  readonly passwordStrengthLabel: Locator;
  
  // Links
  readonly loginLink: Locator;
  
  constructor(page: Page) {
    this.page = page;
    
    // Main elements
    this.registerContainer = page.locator('[data-testid="register-container"]');
    this.registerCard = page.locator('[data-testid="register-card"]');
    this.registerTitle = page.locator('[data-testid="register-title"]');
    this.registerForm = page.locator('[data-testid="register-form"]');
    
    // Form inputs
    this.usernameInput = page.locator('[data-testid="username-input"]');
    this.emailInput = page.locator('[data-testid="email-input"]');
    this.passwordInput = page.locator('[data-testid="password-input"]');
    this.confirmPasswordInput = page.locator('[data-testid="confirm-password-input"]');
    this.registerButton = page.locator('[data-testid="register-button"]');
    this.togglePasswordVisibility = page.locator('[data-testid="toggle-password-visibility"]');
    this.toggleConfirmPasswordVisibility = page.locator('[data-testid="toggle-confirm-password-visibility"]');
    
    // Password strength
    this.passwordStrengthBar = page.locator('.strength-fill');
    this.passwordStrengthLabel = page.locator('.strength-label');
    
    // Error and links
    this.errorMessage = page.locator('[data-testid="error-message"]');
    this.loginLink = page.locator('[data-testid="login-link"]');
  }
  
  async goto() {
    await this.page.goto('/register');
    await this.page.waitForLoadState('networkidle');
  }
  
  async register(username: string, email: string, password: string, confirmPassword?: string) {
    await this.fillForm(username, email, password, confirmPassword || password);
    await this.registerButton.click();
    
    // Wait for navigation or error
    await this.page.waitForLoadState('networkidle');
  }
  
  async fillForm(username: string, email: string, password: string, confirmPassword: string) {
    await this.usernameInput.fill(username);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(confirmPassword);
  }
  
  async getErrorMessage(): Promise<string> {
    await this.errorMessage.waitFor({ state: 'visible', timeout: 5000 });
    return await this.errorMessage.textContent() || '';
  }
  
  async waitForError() {
    await this.errorMessage.waitFor({ state: 'visible', timeout: 5000 });
  }
  
  async isRegisterButtonDisabled(): Promise<boolean> {
    return await this.registerButton.isDisabled();
  }
  
  async getPasswordStrength(): Promise<string> {
    const strengthClass = await this.passwordStrengthBar.getAttribute('class');
    
    if (strengthClass?.includes('strong')) return 'strong';
    if (strengthClass?.includes('good')) return 'good';
    if (strengthClass?.includes('fair')) return 'fair';
    return 'weak';
  }
  
  async getPasswordStrengthLabel(): Promise<string> {
    return await this.passwordStrengthLabel.textContent() || '';
  }
  
  async validatePasswordStrength(password: string): Promise<string> {
    await this.passwordInput.fill(password);
    await this.page.waitForTimeout(300); // Wait for strength calculation
    return await this.getPasswordStrength();
  }
  
  async clickLoginLink() {
    await this.loginLink.click();
  }
  
  async hasUsernameError(): Promise<boolean> {
    const field = this.usernameInput.locator('xpath=ancestor::mat-form-field');
    const error = field.locator('mat-error');
    return await error.isVisible();
  }
  
  async hasEmailError(): Promise<boolean> {
    const field = this.emailInput.locator('xpath=ancestor::mat-form-field');
    const error = field.locator('mat-error');
    return await error.isVisible();
  }
  
  async hasPasswordError(): Promise<boolean> {
    const field = this.passwordInput.locator('xpath=ancestor::mat-form-field');
    const error = field.locator('mat-error');
    return await error.isVisible();
  }
  
  async hasConfirmPasswordError(): Promise<boolean> {
    const field = this.confirmPasswordInput.locator('xpath=ancestor::mat-form-field');
    const error = field.locator('mat-error');
    return await error.isVisible();
  }
}
