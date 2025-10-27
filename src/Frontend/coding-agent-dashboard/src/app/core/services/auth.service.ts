import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';

/**
 * Simple AuthService for JWT retrieval/refresh.
 * In production, wire it to your real auth provider (e.g., OAuth/OIDC).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenSubject = new BehaviorSubject<string | null>(null);

  /**
   * Emit token changes for re-auth flows
   */
  public readonly tokenChanged$: Observable<string | null> = this.tokenSubject.asObservable();

  /**
   * Get the current token (from memory or storage)
   */
  getToken(): string | null {
    const inMemory = this.tokenSubject.value;
    if (inMemory) return inMemory;
    try {
      const stored = localStorage.getItem('auth_token');
      return stored;
    } catch {
      return null;
    }
  }

  /**
   * Set token (e.g., after login/refresh)
   */
  setToken(token: string | null): void {
    if (token) {
      localStorage.setItem('auth_token', token);
    } else {
      localStorage.removeItem('auth_token');
    }
    this.tokenSubject.next(token);
  }

  /**
   * Placeholder refresh implementation; replace with real call
   */
  refreshToken(): Observable<string | null> {
    // TODO: implement refresh with your auth backend
    return of(this.getToken());
  }
}
