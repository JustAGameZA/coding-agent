import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError, timer } from 'rxjs';
import { tap, catchError, map, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { 
  LoginRequest, 
  LoginResponse, 
  RegisterRequest, 
  User,
  RefreshTokenRequest,
  RefreshTokenResponse 
} from '../models/auth.models';

/**
 * Enhanced AuthService with login, logout, register, and auto-refresh capabilities.
 * Integrates with Auth Service backend via Gateway at /auth/** routes.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly authApiUrl = `${environment.apiUrl}/auth`;
  
  private tokenSubject = new BehaviorSubject<string | null>(null);
  private userSignal = signal<User | null>(null);
  private refreshTimerSubscription: any = null;

  /**
   * Emit token changes for re-auth flows
   */
  public readonly tokenChanged$: Observable<string | null> = this.tokenSubject.asObservable();

  constructor() {
    // Initialize token and user from storage on startup
    this.initializeAuth();
  }

  /**
   * Initialize authentication state from localStorage
   */
  private initializeAuth(): void {
    const token = this.getStoredToken();
    if (token && !this.isTokenExpired(token)) {
      this.tokenSubject.next(token);
      this.authenticatedSignal.set(true);
      
      // Decode token to extract user info
      const decoded = this.decodeToken(token);
      if (decoded) {
        // Construct User object from JWT claims
        const user: User = {
          id: decoded.sub || decoded.uid,
          username: decoded.unique_name || decoded.username,
          email: decoded.email,
          roles: Array.isArray(decoded.role) ? decoded.role : [decoded.role || 'User']
        };
        this.userSignal.set(user);
        this.setupAutoRefresh(token);
      }
    } else {
      this.authenticatedSignal.set(false);
    }
  }

  /**
   * Login with username and password
   */
  login(username: string, password: string, rememberMe: boolean = false): Observable<LoginResponse> {
    const request: LoginRequest = { username, password, rememberMe };
    
    return this.http.post<LoginResponse>(`${this.authApiUrl}/login`, request).pipe(
      tap(response => {
        this.setToken(response.accessToken);
        this.userSignal.set(response.user);
        
        // Store refresh token if provided
        if (response.refreshToken) {
          this.setRefreshToken(response.refreshToken);
        }
        
        // Setup auto-refresh
        this.setupAutoRefresh(response.accessToken, response.expiresIn);
      }),
      catchError(this.handleAuthError.bind(this))
    );
  }

  /**
   * Register new user account
   */
  register(registerRequest: RegisterRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.authApiUrl}/register`, registerRequest).pipe(
      tap(response => {
        // Auto-login after successful registration
        this.setToken(response.accessToken);
        this.userSignal.set(response.user);
        
        if (response.refreshToken) {
          this.setRefreshToken(response.refreshToken);
        }
        
        this.setupAutoRefresh(response.accessToken, response.expiresIn);
      }),
      catchError(this.handleAuthError.bind(this))
    );
  }

  /**
   * Logout user and clear all stored data
   */
  logout(): void {
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  /**
   * Authentication state as a signal for reactive updates
   */
  private readonly authenticatedSignal = signal<boolean>(false);
  
  /**
   * Get authentication state as signal (for reactive templates)
   */
  get isAuthenticatedSignal() {
    return this.authenticatedSignal.asReadonly();
  }
  
  /**
   * Check if user is authenticated (has valid token)
   */
  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      this.authenticatedSignal.set(false);
      return false;
    }
    
    // Check if token is expired
    const authenticated = !this.isTokenExpired(token);
    this.authenticatedSignal.set(authenticated);
    return authenticated;
  }

  /**
   * Get current authenticated user
   */
  getCurrentUser(): User | null {
    return this.userSignal();
  }

  /**
   * Get current user as signal (for reactive templates)
   */
  get currentUser() {
    return this.userSignal.asReadonly();
  }

  /**
   * Get the current token (from memory or storage)
   */
  getToken(): string | null {
    const inMemory = this.tokenSubject.value;
    if (inMemory) return inMemory;
    return this.getStoredToken();
  }

  /**
   * Set token (e.g., after login/refresh)
   */
  setToken(token: string | null): void {
    if (token) {
      localStorage.setItem('auth_token', token);
      this.tokenSubject.next(token);
      // Update authentication signal
      const authenticated = !this.isTokenExpired(token);
      this.authenticatedSignal.set(authenticated);
    } else {
      this.clearAuth();
    }
  }

  /**
   * Refresh the access token using refresh token
   */
  refreshToken(): Observable<string | null> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      return of(null);
    }

    const request: RefreshTokenRequest = { refreshToken };
    
    return this.http.post<RefreshTokenResponse>(`${this.authApiUrl}/refresh`, request).pipe(
      tap(response => {
        this.setToken(response.accessToken);
        this.setRefreshToken(response.refreshToken);
        this.setupAutoRefresh(response.accessToken, response.expiresIn);
      }),
      map(response => response.accessToken),
      catchError(error => {
        // If refresh fails, logout user
        this.clearAuth();
        return of(null);
      })
    );
  }

  /**
   * Setup auto-refresh timer (refresh 5 min before expiry)
   */
  private setupAutoRefresh(token: string, expiresIn?: number): void {
    // Clear existing timer
    if (this.refreshTimerSubscription) {
      this.refreshTimerSubscription.unsubscribe();
    }

    let refreshDelay: number;
    
    if (expiresIn) {
      // Refresh 5 minutes before expiry (or 80% of lifetime, whichever is shorter)
      const fiveMinutes = 5 * 60 * 1000;
      const eightyPercent = expiresIn * 0.8 * 1000;
      refreshDelay = Math.min(fiveMinutes, eightyPercent);
    } else {
      // Decode expiry from token
      const decoded = this.decodeToken(token);
      if (decoded && decoded.exp) {
        const expiresAt = decoded.exp * 1000; // Convert to milliseconds
        const now = Date.now();
        const timeUntilExpiry = expiresAt - now;
        const fiveMinutes = 5 * 60 * 1000;
        refreshDelay = Math.max(0, timeUntilExpiry - fiveMinutes);
      } else {
        // Default to 50 minutes if can't decode
        refreshDelay = 50 * 60 * 1000;
      }
    }

    // Setup timer to refresh token
    this.refreshTimerSubscription = timer(refreshDelay).pipe(
      switchMap(() => this.refreshToken())
    ).subscribe();
  }

  /**
   * Get stored token from localStorage
   */
  private getStoredToken(): string | null {
    try {
      return localStorage.getItem('auth_token');
    } catch {
      return null;
    }
  }

  /**
   * Get refresh token from localStorage
   */
  private getRefreshToken(): string | null {
    try {
      return localStorage.getItem('refresh_token');
    } catch {
      return null;
    }
  }

  /**
   * Store refresh token in localStorage
   */
  private setRefreshToken(token: string): void {
    try {
      localStorage.setItem('refresh_token', token);
    } catch {
      // Ignore storage errors
    }
  }

  /**
   * Decode JWT token to extract user info and expiry
   */
  private decodeToken(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      
      const payload = parts[1];
      const decoded = JSON.parse(atob(payload));
      
      return decoded;
    } catch {
      return null;
    }
  }

  /**
   * Check if token is expired
   */
  private isTokenExpired(token: string): boolean {
    const decoded = this.decodeToken(token);
    if (!decoded || !decoded.exp) return true;
    
    const expiresAt = decoded.exp * 1000; // Convert to milliseconds
    const now = Date.now();
    
    return now >= expiresAt;
  }

  /**
   * Clear all authentication data
   */
  private clearAuth(): void {
    try {
      localStorage.removeItem('auth_token');
      localStorage.removeItem('refresh_token');
    } catch {
      // Ignore storage errors
    }
    
    this.tokenSubject.next(null);
    this.userSignal.set(null);
    this.authenticatedSignal.set(false);
    
    if (this.refreshTimerSubscription) {
      this.refreshTimerSubscription.unsubscribe();
      this.refreshTimerSubscription = null;
    }
  }

  /**
   * Handle authentication errors with user-friendly messages
   */
  private handleAuthError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An error occurred during authentication';
    
    if (error.status === 401) {
      errorMessage = 'Invalid username or password';
    } else if (error.status === 403) {
      errorMessage = 'Access forbidden';
    } else if (error.status === 409) {
      errorMessage = 'Username or email already exists';
    } else if (error.status === 400) {
      // Backend returns ProblemDetails with 'detail' field for duplicate username/email
      errorMessage = error.error?.detail || error.error?.message || 'Invalid request data';
    } else if (error.status === 0) {
      errorMessage = 'Unable to connect to server';
    } else if (error.error?.detail) {
      errorMessage = error.error.detail;
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    }
    
    return throwError(() => new Error(errorMessage));
  }
}
