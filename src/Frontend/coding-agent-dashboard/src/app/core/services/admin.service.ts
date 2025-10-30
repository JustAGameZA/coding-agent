import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserListResponse } from '../models/admin.models';

/**
 * AdminService handles all admin-related API calls
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/auth/admin`;

  /**
   * Get paginated list of users with optional search
   */
  getUsers(page: number, pageSize: number, search?: string): Promise<UserListResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }
    
    return firstValueFrom(
      this.http.get<UserListResponse>(`${this.apiUrl}/users`, { params })
    );
  }

  /**
   * Update user roles (Admin only)
   */
  updateUserRoles(userId: string, roles: string[]): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.apiUrl}/users/${userId}/roles`, { roles })
    );
  }

  /**
   * Deactivate user account
   */
  deactivateUser(userId: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.apiUrl}/users/${userId}/deactivate`, {})
    );
  }

  /**
   * Activate user account
   */
  activateUser(userId: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`${this.apiUrl}/users/${userId}/activate`, {})
    );
  }
}
