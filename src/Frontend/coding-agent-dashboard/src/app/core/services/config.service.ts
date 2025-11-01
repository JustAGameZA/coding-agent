import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { SystemConfig, UpdateConfigRequest } from '../models/config.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl || 'http://localhost:5000';

  /**
   * Get system configuration (Admin only)
   */
  getConfig(): Observable<SystemConfig> {
    return this.http.get<SystemConfig>(`${this.baseUrl}/api/admin/config`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Update system configuration (Admin only)
   */
  updateConfig(request: UpdateConfigRequest): Observable<SystemConfig> {
    return this.http.put<SystemConfig>(`${this.baseUrl}/api/admin/config`, request).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Get feature flags
   */
  getFeatureFlags(): Observable<SystemConfig['features']> {
    return this.http.get<SystemConfig['features']>(`${this.baseUrl}/api/admin/config/features`).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Update feature flags
   */
  updateFeatureFlags(features: Partial<SystemConfig['features']>): Observable<SystemConfig['features']> {
    return this.http.put<SystemConfig['features']>(
      `${this.baseUrl}/api/admin/config/features`, 
      features
    ).pipe(
      catchError(this.handleError)
    );
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    
    console.error('ConfigService Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}

