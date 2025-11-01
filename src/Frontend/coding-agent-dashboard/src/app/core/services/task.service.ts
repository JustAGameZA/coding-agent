import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, retry, throwError } from 'rxjs';
import { 
  CreateTaskRequest, 
  UpdateTaskRequest, 
  ExecuteTaskRequest, 
  ExecuteTaskResponse,
  TaskDto,
  TaskDetailDto
} from '../models/task.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl || 'http://localhost:5000';

  /**
   * Create a new task
   */
  createTask(request: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(`${this.baseUrl}/api/orchestration/tasks`, request).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Get task by ID
   */
  getTask(id: string): Observable<TaskDetailDto> {
    return this.http.get<TaskDetailDto>(`${this.baseUrl}/api/orchestration/tasks/${id}`).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Update task
   */
  updateTask(id: string, request: UpdateTaskRequest): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${this.baseUrl}/api/orchestration/tasks/${id}`, request).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Delete task
   */
  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/orchestration/tasks/${id}`).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Execute a task
   */
  executeTask(id: string, request?: ExecuteTaskRequest): Observable<ExecuteTaskResponse> {
    return this.http.post<ExecuteTaskResponse>(
      `${this.baseUrl}/api/orchestration/tasks/${id}/execute`, 
      request || {}
    ).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Cancel a running task
   */
  cancelTask(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/orchestration/tasks/${id}/cancel`, {}).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Retry a failed task
   */
  retryTask(id: string, request?: ExecuteTaskRequest): Observable<ExecuteTaskResponse> {
    return this.http.post<ExecuteTaskResponse>(
      `${this.baseUrl}/api/orchestration/tasks/${id}/retry`, 
      request || {}
    ).pipe(
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
      errorMessage = `Error: ${error.error.message}`;
    } else {
      errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
    }
    
    console.error('TaskService Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}

