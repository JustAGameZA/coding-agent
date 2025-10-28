import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { DashboardService } from './dashboard.service';
import { DashboardStats, EnrichedTask, ActivityEvent } from '../models/dashboard.models';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;
  const baseUrl = 'http://localhost:5003';

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [DashboardService]
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getStats', () => {
    it('should fetch dashboard statistics successfully', (done) => {
      const mockStats: DashboardStats = {
        totalConversations: 10,
        totalMessages: 150,
        totalTasks: 25,
        completedTasks: 20,
        failedTasks: 2,
        runningTasks: 3,
        averageTaskDuration: 300,
        lastUpdated: '2025-10-27T12:00:00Z'
      };

      service.getStats().subscribe({
        next: (stats) => {
          expect(stats).toEqual(mockStats);
          expect(stats.totalConversations).toBe(10);
          expect(stats.completedTasks).toBe(20);
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStats);
    });

    it('should handle error when fetching stats fails', (done) => {
      service.getStats().subscribe({
        error: (error: any) => {
          expect(error).toBeTruthy();
          expect(error.message).toContain('Error Code: 500');
          done();
        }
      });

      // Handle retries
      const req1 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req1.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      
      const req2 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req2.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      
      const req3 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req3.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should retry failed requests twice', (done) => {
      let attemptCount = 0;

      service.getStats().subscribe({
        error: (error: any) => {
          expect(attemptCount).toBe(3);
          expect(error).toBeTruthy();
          done();
        }
      });

      for (let i = 0; i < 3; i++) {
        const req = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
        attemptCount++;
        req.flush('Network error', { status: 0, statusText: 'Unknown Error' });
      }
    });
  });

  describe('getTasks', () => {
    it('should fetch tasks with pagination', (done) => {
      const mockTasks: EnrichedTask[] = [
        {
          id: '1',
          title: 'Fix bug in chat service',
          type: 'BugFix',
          complexity: 'Simple',
          status: 'Completed',
          createdAt: '2025-10-27T10:00:00Z',
          completedAt: '2025-10-27T10:30:00Z',
          duration: 1800,
          tokenCost: 5000,
          conversationId: 'conv-1',
          pullRequestNumber: 123
        },
        {
          id: '2',
          title: 'Add new feature',
          type: 'Feature',
          complexity: 'Medium',
          status: 'Running',
          createdAt: '2025-10-27T11:00:00Z',
          conversationId: 'conv-2'
        }
      ];

      service.getTasks(1, 20).subscribe({
        next: (tasks) => {
          expect(tasks).toEqual(mockTasks);
          expect(tasks.length).toBe(2);
          expect(tasks[0].title).toBe('Fix bug in chat service');
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/dashboard/tasks?page=1&pageSize=20`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(mockTasks);
    });

    it('should use default pagination values', (done) => {
      const mockTasks: EnrichedTask[] = [];

      service.getTasks().subscribe({
        next: () => {
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/dashboard/tasks?page=1&pageSize=20`);
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(mockTasks);
    });

    it('should handle error when fetching tasks fails', (done) => {
      service.getTasks(1, 10).subscribe({
        error: (error: any) => {
          expect(error).toBeTruthy();
          done();
        }
      });

      // Handle retries
      const req1 = httpMock.expectOne(`${baseUrl}/dashboard/tasks?page=1&pageSize=10`);
      req1.flush('Not found', { status: 404, statusText: 'Not Found' });
      
      const req2 = httpMock.expectOne(`${baseUrl}/dashboard/tasks?page=1&pageSize=10`);
      req2.flush('Not found', { status: 404, statusText: 'Not Found' });
      
      const req3 = httpMock.expectOne(`${baseUrl}/dashboard/tasks?page=1&pageSize=10`);
      req3.flush('Not found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('getActivity', () => {
    it('should fetch activity events with limit', (done) => {
      const mockActivity: ActivityEvent[] = [
        {
          timestamp: '2025-10-27T12:00:00Z',
          type: 'TaskCreated',
          description: 'New task created',
          userId: 'user-1',
          metadata: { taskId: 'task-1' }
        },
        {
          timestamp: '2025-10-27T12:05:00Z',
          type: 'MessageSent',
          description: 'Message sent in conversation',
          userId: 'user-2',
          metadata: { conversationId: 'conv-1' }
        }
      ];

      service.getActivity(50).subscribe({
        next: (activity) => {
          expect(activity).toEqual(mockActivity);
          expect(activity.length).toBe(2);
          expect(activity[0].type).toBe('TaskCreated');
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/dashboard/activity?limit=50`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('limit')).toBe('50');
      req.flush(mockActivity);
    });

    it('should use default limit value', (done) => {
      const mockActivity: ActivityEvent[] = [];

      service.getActivity().subscribe({
        next: () => {
          done();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/dashboard/activity?limit=50`);
      expect(req.request.params.get('limit')).toBe('50');
      req.flush(mockActivity);
    });

    it('should handle error when fetching activity fails', (done) => {
      service.getActivity(10).subscribe({
        error: (error: any) => {
          expect(error).toBeTruthy();
          done();
        }
      });

      // Handle retries
      const req1 = httpMock.expectOne(`${baseUrl}/dashboard/activity?limit=10`);
      req1.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      
      const req2 = httpMock.expectOne(`${baseUrl}/dashboard/activity?limit=10`);
      req2.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      
      const req3 = httpMock.expectOne(`${baseUrl}/dashboard/activity?limit=10`);
      req3.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('error handling', () => {
    it('should handle client-side errors', (done) => {
      service.getStats().subscribe({
        error: (error: any) => {
          expect(error).toBeTruthy();
          expect(error.message).toBeDefined();
          done();
        }
      });

      // Handle retries
      const req1 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req1.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
      
      const req2 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req2.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
      
      const req3 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req3.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    });

    it('should handle server-side errors with status code', (done) => {
      service.getStats().subscribe({
        error: (error: any) => {
          expect(error.message).toContain('Error Code: 403');
          done();
        }
      });

      // Handle retries
      const req1 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req1.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
      
      const req2 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req2.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
      
      const req3 = httpMock.expectOne(`${baseUrl}/dashboard/stats`);
      req3.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
    });
  });
});
