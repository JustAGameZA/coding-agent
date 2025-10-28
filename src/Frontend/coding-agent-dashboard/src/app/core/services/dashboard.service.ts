import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, retry, throwError } from 'rxjs';
import { DashboardStats, EnrichedTask, ActivityEvent } from '../models/dashboard.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private baseUrl = environment.dashboardServiceUrl || 'http://localhost:5003';

  /**
   * Get dashboard statistics (aggregated from Chat + Orchestration services)
   */
  public getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.baseUrl}/stats`).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Get paginated list of enriched tasks
   * @param page - Page number (1-based)
   * @param pageSize - Number of items per page
   */
  public getTasks(page: number = 1, pageSize: number = 20): Observable<EnrichedTask[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<EnrichedTask[]>(`${this.baseUrl}/tasks`, { params }).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Get recent activity events
   * @param limit - Maximum number of events to return
   */
  public getActivity(limit: number = 50): Observable<ActivityEvent[]> {
    const params = new HttpParams().set('limit', limit.toString());

    return this.http.get<ActivityEvent[]>(`${this.baseUrl}/activity`, { params }).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    
    console.error('DashboardService Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
