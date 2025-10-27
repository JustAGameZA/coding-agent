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
  confirmPassword?: string; // Used for form validation, not sent to backend
}

export interface LoginResponse {
  token: string;
  refreshToken?: string;
  expiresIn: number; // seconds until token expiry
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
  token: string;
  refreshToken: string;
  expiresIn: number;
}
