/**
 * Authentication models for login, registration, and user management
 */

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  confirmPassword: string; // Required by backend for password confirmation validation
}

export interface LoginResponse {
  accessToken: string;  // Backend uses PascalCase -> camelCase JSON serialization
  refreshToken?: string;
  expiresIn: number; // seconds until token expiry
  tokenType?: string; // e.g., "Bearer"
  user: User;
}

export interface User {
  id: string;
  username: string;
  email: string;
  roles?: string[];
  createdAt?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType?: string;
  user: User;
}
