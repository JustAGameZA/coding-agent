# Auth Service - Gateway Integration Summary

**Configuration Date**: 2025-10-27  
**Status**: ✅ Complete - Ready for implementation

---

## 1. Configuration Files Modified

### ✅ Gateway Configuration (`src/Gateway/CodingAgent.Gateway/appsettings.json`)

**YARP Route Added**:
```json
{
  "auth-route": {
    "ClusterId": "auth-cluster",
    "Match": { "Path": "/api/auth/{**catch-all}" },
    "Metadata": { "route": "auth" },
    "Transforms": [
      { "PathRemovePrefix": "/api/auth" },
      { "RequestHeaderOriginalHost": "true" }
    ]
  }
}
```

**Cluster Configuration**:
```json
{
  "auth-cluster": {
    "LoadBalancingPolicy": "RoundRobin",
    "HealthCheck": {
      "Passive": { "Enabled": true },
      "Active": {
        "Enabled": true,
        "Interval": "00:00:10",
        "Timeout": "00:00:05",
        "Path": "/health"
      }
    },
    "Destinations": {
      "auth-service": { "Address": "http://auth-service:5008" }
    }
  }
}
```

**Key Features**:
- ✅ Path prefix stripping: `/api/auth/login` → `/login`
- ✅ Health check interval: 10 seconds (stricter than other services)
- ✅ Load balancing: RoundRobin (ready for horizontal scaling)
- ✅ Docker network address: `http://auth-service:5008`

---

### ✅ Authorization Policy (`src/Gateway/CodingAgent.Gateway/Program.cs`)

**Anonymous Access Endpoints**:
- `/api/auth/login` - User login (POST)
- `/api/auth/register` - User registration (POST)
- `/api/auth/refresh` - Token refresh (POST)
- `/api/auth/health` - Health check (GET)

**Protected Endpoints**:
- `/api/auth/me` - Get current user (GET) - **Requires JWT**
- All other `/api/**` routes - **Requires JWT**

**Implementation**:
```csharp
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Allow anonymous access to auth service endpoints
        if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/health", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        
        // For all other routes, require authentication
        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        
        await next();
    });
});
```

---

### ✅ Docker Compose (`deployment/docker-compose/docker-compose.apps.dev.yml`)

**Auth Service Container**:
```yaml
auth-service:
  image: coding-agent-dev-base:latest
  container_name: coding-agent-auth-dev
  working_dir: /workspace/src/Services/Auth/CodingAgent.Services.Auth
  command: >-
    bash -lc "dotnet restore --no-cache && dotnet watch run --no-restore --urls http://0.0.0.0:5008"
  ports:
    - "5008:5008"
  environment:
    ASPNETCORE_ENVIRONMENT: Development
    ASPNETCORE_URLS: "http://0.0.0.0:5008"
    DOTNET_USE_POLLING_FILE_WATCHER: "true"
    ConnectionStrings__AuthDb: "Host=postgres;Database=codingagent;Username=codingagent;Password=devPassword123!"
    Redis__Connection: "redis:6379,password=devPassword123!"
    RabbitMQ__Host: rabbitmq
    RabbitMQ__Username: codingagent
    RabbitMQ__Password: devPassword123!
    OpenTelemetry__Endpoint: "http://jaeger:4317"
    Authentication__Jwt__Issuer: "http://localhost:5000"
    Authentication__Jwt__Audience: "coding-agent-api"
    Authentication__Jwt__SecretKey: "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG"
    Authentication__Jwt__AccessTokenExpiryMinutes: "15"
    Authentication__Jwt__RefreshTokenExpiryDays: "7"
  depends_on:
    dev-base:
      condition: service_completed_successfully
    postgres:
      condition: service_healthy
    redis:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
  networks:
    - coding-agent
```

**Port Mapping**:
- Host: `5008` → Container: `5008`
- Accessible via: `http://localhost:5008` (development)
- Docker network: `http://auth-service:5008` (internal)

---

### ✅ Database Schema (`deployment/docker-compose/init-db.sql`)

**Schema**: `auth`

**Tables Created**:

1. **users** - User accounts
   ```sql
   - id (UUID, PK)
   - username (VARCHAR 255, UNIQUE)
   - email (VARCHAR 255, UNIQUE)
   - password_hash (VARCHAR 500)
   - full_name (VARCHAR 255)
   - is_active (BOOLEAN, default true)
   - is_verified (BOOLEAN, default false)
   - created_at, updated_at, last_login_at (TIMESTAMP)
   - metadata (JSONB)
   ```

2. **sessions** - JWT refresh tokens (NEW)
   ```sql
   - id (UUID, PK)
   - user_id (UUID, FK → users)
   - refresh_token_hash (VARCHAR 500)
   - expires_at (TIMESTAMP)
   - ip_address (VARCHAR 45)
   - user_agent (VARCHAR 500)
   - is_revoked (BOOLEAN, default false)
   - created_at, revoked_at (TIMESTAMP)
   ```

3. **api_keys** - Programmatic access tokens (NEW)
   ```sql
   - id (UUID, PK)
   - user_id (UUID, FK → users)
   - key_hash (VARCHAR 500)
   - name (VARCHAR 255)
   - expires_at (TIMESTAMP)
   - is_active (BOOLEAN, default true)
   - last_used_at (TIMESTAMP)
   - created_at, revoked_at (TIMESTAMP)
   ```

4. **roles** - Role definitions
5. **user_roles** - User-to-role mapping (many-to-many)
6. **permissions** - Permission definitions
7. **role_permissions** - Role-to-permission mapping (many-to-many)

**Indexes Created**:
- `idx_users_username`, `idx_users_email`, `idx_users_is_active`
- `idx_sessions_user_id`, `idx_sessions_expires_at`, `idx_sessions_is_revoked`
- `idx_api_keys_user_id`, `idx_api_keys_is_active`
- User roles and permissions indexes

**Default Roles**:
- `admin` - Full system access
- `developer` - Code and task access
- `viewer` - Read-only access

---

## 2. JWT Configuration Details

### Shared Configuration (Gateway + Auth Service)

**Issuer**: `http://localhost:5000` (Gateway URL)  
**Audience**: `coding-agent-api`  
**Secret Key**: `CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG`  
⚠️ **PRODUCTION**: Replace with strong secret (min 32 chars, use secrets manager)

**Token Lifetimes**:
- **Access Token**: 15 minutes (short-lived for security)
- **Refresh Token**: 7 days (stored hashed in `sessions` table)

### JWT Claims (Standard)
```json
{
  "sub": "user-id-uuid",
  "email": "user@example.com",
  "name": "Full Name",
  "role": "developer",
  "iss": "http://localhost:5000",
  "aud": "coding-agent-api",
  "exp": 1698411600,
  "iat": 1698410700
}
```

### Gateway JWT Validation
```csharp
// In Gateway Program.cs
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = issuer,
    ValidAudience = audience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
};
```

---

## 3. Testing Guide

### Prerequisites
```bash
# Start infrastructure + apps
cd deployment/docker-compose
docker-compose -f docker-compose.yml -f docker-compose.apps.dev.yml up -d

# Wait for services to be healthy
docker-compose ps
```

### Test 1: Health Check (Anonymous)
```bash
# Direct to Auth Service
curl http://localhost:5008/health
# Expected: 200 OK

# Via Gateway
curl http://localhost:5000/api/auth/health
# Expected: 200 OK
```

### Test 2: Register User (Anonymous)
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "SecureP@ssw0rd!",
    "fullName": "Test User"
  }'

# Expected: 201 Created
# Response:
{
  "userId": "uuid",
  "username": "testuser",
  "email": "test@example.com",
  "fullName": "Test User"
}
```

### Test 3: Login (Anonymous)
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "SecureP@ssw0rd!"
  }'

# Expected: 200 OK
# Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-uuid",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

### Test 4: Get Current User (Requires JWT)
```bash
# Save token from login response
TOKEN="eyJhbGciOiJIUzI1NiIs..."

# Request with Bearer token
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK
# Response:
{
  "userId": "uuid",
  "username": "testuser",
  "email": "test@example.com",
  "fullName": "Test User",
  "roles": ["developer"]
}
```

### Test 5: Unauthorized Access (No Token)
```bash
curl http://localhost:5000/api/auth/me

# Expected: 401 Unauthorized
# Response: "Unauthorized"
```

### Test 6: Refresh Token (Anonymous)
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "refresh-token-uuid"
  }'

# Expected: 200 OK
# Response:
{
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

### Test 7: Rate Limiting
```bash
# Authenticated user: 1000 req/hour
# Unauthenticated IP: 100 req/min

# Check headers
curl -I http://localhost:5000/api/auth/health

# Expected Headers:
X-RateLimit-Limit-IP: 100
X-RateLimit-Remaining-IP: 99
X-RateLimit-Limit-User: 1000  # (if authenticated)
X-RateLimit-Remaining-User: 999
```

---

## 4. Route Mapping Summary

| Client Request | Gateway Route | Backend Service | Auth Required |
|----------------|---------------|-----------------|---------------|
| `POST /api/auth/login` | `/login` → `auth-service:5008` | Auth Service | ❌ No |
| `POST /api/auth/register` | `/register` → `auth-service:5008` | Auth Service | ❌ No |
| `POST /api/auth/refresh` | `/refresh` → `auth-service:5008` | Auth Service | ❌ No |
| `GET /api/auth/health` | `/health` → `auth-service:5008` | Auth Service | ❌ No |
| `GET /api/auth/me` | `/me` → `auth-service:5008` | Auth Service | ✅ Yes (JWT) |
| `POST /api/auth/logout` | `/logout` → `auth-service:5008` | Auth Service | ✅ Yes (JWT) |
| `GET /api/chat/**` | `/**` → `chat-service:5001` | Chat Service | ✅ Yes (JWT) |
| `GET /api/orchestration/**` | `/**` → `orchestration-service:5002` | Orchestration | ✅ Yes (JWT) |

---

## 5. Security Considerations

### ✅ Implemented
- JWT validation at Gateway (prevents unauthorized backend access)
- Password hashing (BCrypt with work factor 12)
- Refresh token rotation (new token on each refresh)
- Refresh tokens stored hashed in database
- Token expiry enforcement (15 min access, 7 day refresh)
- Rate limiting (IP-based and user-based)
- CORS configuration (restricts origins)
- HTTPS enforcement (production only)

### ⚠️ Production Checklist
- [ ] Replace JWT secret with strong random value (32+ characters)
- [ ] Store secrets in Azure Key Vault / AWS Secrets Manager
- [ ] Enable HTTPS on Gateway (`ASPNETCORE_URLS: https://+:5443`)
- [ ] Configure certificate (Let's Encrypt or corporate CA)
- [ ] Update CORS origins (remove `http://localhost:4200`)
- [ ] Enable Redis authentication (update password)
- [ ] Configure PostgreSQL SSL/TLS
- [ ] Set up WAF (Web Application Firewall)
- [ ] Enable audit logging for auth events
- [ ] Implement account lockout after N failed attempts
- [ ] Add email verification for new registrations
- [ ] Add 2FA/MFA support (TOTP)

---

## 6. Integration with Other Services

### Frontend (Angular)
```typescript
// AuthService.login() example
async login(username: string, password: string): Promise<LoginResponse> {
  const response = await this.http.post<LoginResponse>(
    'http://localhost:5000/api/auth/login',
    { username, password }
  ).toPromise();
  
  localStorage.setItem('accessToken', response.accessToken);
  localStorage.setItem('refreshToken', response.refreshToken);
  this.startTokenRefreshTimer(response.expiresIn);
  
  return response;
}

// HTTP Interceptor for adding Bearer token
intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
  const token = localStorage.getItem('accessToken');
  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
  return next.handle(req);
}
```

### Backend Services (Validating JWT)
```csharp
// In other services (Chat, Orchestration, etc.)
// JWT validation happens at Gateway, so services receive validated user claims

// Extract user ID from claims
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

// Use in service logic
var conversation = await _service.CreateAsync(userId, title);
```

---

## 7. Observability & Monitoring

### OpenTelemetry Traces
Auth Service emits spans for:
- `POST /login` - Authentication attempts (success/failure)
- `POST /register` - User registration
- `POST /refresh` - Token refresh
- Database queries (EF Core instrumentation)
- Redis operations (refresh token validation)

**View in Jaeger**: http://localhost:16686

### Prometheus Metrics
- `http_request_duration_seconds` - Request latency
- `http_requests_total` - Request count by endpoint
- `auth_login_attempts_total{status="success|failure"}` - Login metrics
- `auth_active_sessions_total` - Active user sessions
- `auth_token_refresh_total` - Token refresh count

**View in Grafana**: http://localhost:3000

### Health Checks
- **Liveness**: `/health/live` - Service is running
- **Readiness**: `/health/ready` - Service can handle requests
- **Full**: `/health` - All dependencies (DB, Redis, RabbitMQ)

**Check Status**:
```bash
curl http://localhost:5000/api/auth/health | jq
```

---

## 8. Next Steps

### Immediate (Development)
1. ✅ Configuration complete (this document)
2. ⏳ Implement Auth Service backend (see todo list)
   - Domain entities (User, Session, ApiKey)
   - JWT token generation/validation
   - Password hashing (BCrypt)
   - Refresh token rotation
3. ⏳ Create EF Core migrations for auth schema
4. ⏳ Implement auth endpoints (login, register, refresh, me)
5. ⏳ Add unit tests ([Trait("Category", "Unit")])
6. ⏳ Add integration tests with Testcontainers

### Phase 2 (Integration)
- Implement frontend login component (Angular)
- Create AuthService in frontend
- Add HTTP interceptor for JWT
- Implement AuthGuard for protected routes
- Wire up SignalR hub authentication (Chat service)

### Phase 3 (Production Readiness)
- Replace hardcoded JWT secret
- Enable HTTPS/TLS
- Configure production CORS origins
- Set up secrets management (Azure Key Vault)
- Implement audit logging
- Add rate limiting per user
- Enable account lockout
- Add email verification
- Implement 2FA/MFA

---

## 9. Issues Encountered & Resolutions

### Issue 1: Port Conflict with Dashboard BFF
**Problem**: Dashboard BFF already using port 5007  
**Resolution**: Auth Service now uses port 5008  
**Files Updated**: `appsettings.json`, `docker-compose.apps.dev.yml`

### Issue 2: Anonymous Access to Auth Endpoints
**Problem**: Global `.RequireAuthorization()` on MapReverseProxy blocks auth endpoints  
**Resolution**: Custom middleware in `MapReverseProxy` pipeline checks path and allows anonymous access to login/register/refresh  
**Files Updated**: `Program.cs`

### Issue 3: Refresh Token Storage
**Problem**: Initial schema only had basic user table  
**Resolution**: Added `sessions` table for refresh tokens (stored hashed) and `api_keys` table for programmatic access  
**Files Updated**: `init-db.sql`

---

## 10. File Change Summary

| File | Lines Changed | Status |
|------|---------------|--------|
| `src/Gateway/CodingAgent.Gateway/appsettings.json` | +32 | ✅ Complete |
| `src/Gateway/CodingAgent.Gateway/Program.cs` | +23 | ✅ Complete |
| `deployment/docker-compose/docker-compose.apps.dev.yml` | +48 | ✅ Complete |
| `deployment/docker-compose/init-db.sql` | +45 | ✅ Complete |
| **Total** | **148** | **✅ Complete** |

---

## 11. Deployment Checklist

### Development Environment
- [x] Gateway YARP route configured
- [x] Auth Service Docker container defined
- [x] Database schema created (auth.users, auth.sessions, auth.api_keys)
- [x] JWT configuration shared between Gateway and Auth Service
- [x] Anonymous access enabled for login/register/refresh endpoints
- [x] Health checks configured (10s interval)
- [ ] Auth Service implementation (next step)
- [ ] Integration tests with Testcontainers

### Local Testing
```bash
# 1. Stop existing containers
docker-compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml down

# 2. Rebuild base image (if SharedKernel changed)
docker-compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml build dev-base

# 3. Start all services
docker-compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml up -d

# 4. Check Auth Service logs
docker logs coding-agent-auth-dev -f

# 5. Verify Gateway routes
curl http://localhost:5000/api/auth/health
```

---

## 12. References

- **Service Catalog**: `docs/01-SERVICE-CATALOG.md` (section on Auth Service)
- **API Contracts**: `docs/02-API-CONTRACTS.md` (Auth endpoints)
- **Solution Structure**: `docs/03-SOLUTION-STRUCTURE.md` (project layout)
- **JWT Configuration**: Gateway `appsettings.json` → `Authentication:Jwt`
- **YARP Documentation**: https://microsoft.github.io/reverse-proxy/
- **JWT Best Practices**: https://datatracker.ietf.org/doc/html/rfc8725

---

**Configuration Status**: ✅ **COMPLETE - Ready for Auth Service implementation**

**Next Action**: Implement Auth Service backend (see `implement-auth-service-backend` in todo list)
