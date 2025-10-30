import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, LoginResponse } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: jasmine.SpyObj<Router>;

  const mockLoginResponse: LoginResponse = {
    token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNzMwMDAwMDAwfQ.1234567890',
    refreshToken: 'mock-refresh-token',
    expiresIn: 3600,
    user: {
      id: '123',
      username: 'testuser',
      email: 'test@example.com',
      roles: ['user']
    }
  };

  beforeEach(() => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should send login request to backend', () => {
      const username = 'testuser';
      const password = 'password123';

      service.login(username, password).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        username,
        password,
        rememberMe: false
      } as LoginRequest);

      req.flush(mockLoginResponse);
    });

    it('should store token on successful login', (done) => {
      service.login('testuser', 'password123').subscribe(() => {
        expect(localStorage.getItem('auth_token')).toBe(mockLoginResponse.token);
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockLoginResponse);
    });

    it('should store refresh token when provided', (done) => {
      service.login('testuser', 'password123').subscribe(() => {
        expect(localStorage.getItem('refresh_token')).toBe(mockLoginResponse.refreshToken!);
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockLoginResponse);
    });

    it('should emit token change on login', (done) => {
      service.tokenChanged$.subscribe(token => {
        if (token) {
          expect(token).toBe(mockLoginResponse.token);
          done();
        }
      });

      service.login('testuser', 'password123').subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockLoginResponse);
    });

    it('should handle 401 error with user-friendly message', (done) => {
      service.login('testuser', 'wrongpassword').subscribe({
        error: (error) => {
          expect(error.message).toBe('Invalid username or password');
          done();
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    });

    it('should handle 409 error (duplicate user)', (done) => {
      service.login('testuser', 'password123').subscribe({
        error: (error) => {
          expect(error.message).toBe('Username or email already exists');
          done();
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush({ message: 'Conflict' }, { status: 409, statusText: 'Conflict' });
    });

    it('should handle network error', (done) => {
      service.login('testuser', 'password123').subscribe({
        error: (error) => {
          expect(error.message).toBe('Unable to connect to server');
          done();
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.error(new ProgressEvent('error'), { status: 0 });
    });
  });

  describe('register', () => {
    it('should send register request to backend', () => {
      const registerRequest: RegisterRequest = {
        username: 'newuser',
        email: 'new@example.com',
        password: 'ValidPass123!'
      };

      service.register(registerRequest).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(registerRequest);

      req.flush(mockLoginResponse);
    });

    it('should store token on successful registration', (done) => {
      const registerRequest: RegisterRequest = {
        username: 'newuser',
        email: 'new@example.com',
        password: 'ValidPass123!'
      };

      service.register(registerRequest).subscribe(() => {
        expect(localStorage.getItem('auth_token')).toBe(mockLoginResponse.token);
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      req.flush(mockLoginResponse);
    });
  });

  describe('logout', () => {
    it('should clear token from localStorage', () => {
      localStorage.setItem('auth_token', 'test-token');
      localStorage.setItem('refresh_token', 'test-refresh');

      service.logout();

      expect(localStorage.getItem('auth_token')).toBeNull();
      expect(localStorage.getItem('refresh_token')).toBeNull();
    });

    it('should emit null token on logout', (done) => {
      // Set initial token so we can detect the change to null
      service.setToken('initial-token');
      
      let emissionCount = 0;
      service.tokenChanged$.subscribe(token => {
        emissionCount++;
        if (emissionCount === 2 && token === null) {
          // First emission is from setToken, second is from logout
          done();
        }
      });

      service.logout();
    });

    it('should navigate to login page', () => {
      service.logout();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no token exists', () => {
      expect(service.isAuthenticated()).toBe(false);
    });

    it('should return true when valid token exists', () => {
      // Create a token that expires in the future
      const futureTimestamp = Math.floor(Date.now() / 1000) + 3600;
      const validToken = `header.${btoa(JSON.stringify({ exp: futureTimestamp }))}.signature`;
      
      localStorage.setItem('auth_token', validToken);
      service['initializeAuth']();

      expect(service.isAuthenticated()).toBe(true);
    });

    it('should return false when token is expired', () => {
      // Create a token that expired in the past
      const pastTimestamp = Math.floor(Date.now() / 1000) - 3600;
      const expiredToken = `header.${btoa(JSON.stringify({ exp: pastTimestamp }))}.signature`;
      
      localStorage.setItem('auth_token', expiredToken);
      service['initializeAuth']();

      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('getCurrentUser', () => {
    it('should return null when not authenticated', () => {
      expect(service.getCurrentUser()).toBeNull();
    });

    it('should return user after login', (done) => {
      service.login('testuser', 'password123').subscribe(() => {
        const user = service.getCurrentUser();
        expect(user).toEqual(mockLoginResponse.user);
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockLoginResponse);
    });
  });

  describe('getToken', () => {
    it('should return null when no token stored', () => {
      expect(service.getToken()).toBeNull();
    });

    it('should return token from localStorage', () => {
      const token = 'test-token';
      localStorage.setItem('auth_token', token);

      expect(service.getToken()).toBe(token);
    });
  });

  describe('setToken', () => {
    it('should store token in localStorage', () => {
      const token = 'test-token';
      service.setToken(token);

      expect(localStorage.getItem('auth_token')).toBe(token);
    });

    it('should clear token when null', () => {
      localStorage.setItem('auth_token', 'test-token');
      service.setToken(null);

      expect(localStorage.getItem('auth_token')).toBeNull();
    });
  });

  describe('refreshToken', () => {
    it('should send refresh request with refresh token', () => {
      localStorage.setItem('refresh_token', 'test-refresh-token');

      service.refreshToken().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'test-refresh-token' });

      req.flush({
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600
      });
    });

    it('should return null when no refresh token exists', (done) => {
      service.refreshToken().subscribe(token => {
        expect(token).toBeNull();
        done();
      });
    });

    it('should update token on successful refresh', (done) => {
      localStorage.setItem('refresh_token', 'test-refresh-token');

      service.refreshToken().subscribe(token => {
        expect(token).toBe('new-token');
        expect(localStorage.getItem('auth_token')).toBe('new-token');
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
      req.flush({
        token: 'new-token',
        refreshToken: 'new-refresh-token',
        expiresIn: 3600
      });
    });

    it('should clear auth on refresh failure', (done) => {
      localStorage.setItem('auth_token', 'old-token');
      localStorage.setItem('refresh_token', 'old-refresh-token');

      service.refreshToken().subscribe(token => {
        expect(token).toBeNull();
        expect(localStorage.getItem('auth_token')).toBeNull();
        expect(localStorage.getItem('refresh_token')).toBeNull();
        done();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
      req.error(new ProgressEvent('error'), { status: 401 });
    });
  });
});
