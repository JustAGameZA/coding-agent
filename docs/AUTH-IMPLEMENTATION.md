# Authentication Service - Implementation Documentation

**Version**: 1.0.0  
**Last Updated**: October 27, 2025  
**Status**: ✅ Production-Ready

## Executive Summary

The Authentication Service is a production-ready microservice built with .NET 9 that provides secure user authentication and authorization using JWT tokens with refresh token rotation. The service follows Clean Architecture principles and implements industry-standard security practices including BCrypt password hashing (work factor 12), token-based authentication, and comprehensive validation.

**Key Features**:
- ✅ JWT-based authentication (15-minute access tokens, 7-day refresh tokens)
- ✅ Refresh token rotation for enhanced security
- ✅ BCrypt password hashing with work factor 12
- ✅ Comprehensive input validation using FluentValidation
- ✅ OpenTelemetry instrumentation for observability
- ✅ Session management with IP tracking
- ✅ Role-based access control (RBAC)
- ✅ Password change with session invalidation
- ✅ 18 unit tests (100% passing)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Authentication Flows](#authentication-flows)
3. [API Endpoints](#api-endpoints)
4. [JWT Token Structure](#jwt-token-structure)
5. [Security Considerations](#security-considerations)
6. [Database Schema](#database-schema)
7. [Configuration](#configuration)
8. [Deployment Guide](#deployment-guide)
9. [Testing](#testing)
10. [Troubleshooting](#troubleshooting)
11. [Production Checklist](#production-checklist)

---

## Architecture Overview

### System Architecture

```mermaid
graph TB
    Client[Client Application]
    Gateway[API Gateway<br/>YARP]
    AuthService[Auth Service<br/>.NET 9]
    PostgreSQL[(PostgreSQL<br/>auth schema)]
    RabbitMQ[RabbitMQ<br/>Message Bus]
    
    Client -->|HTTPS| Gateway
    Gateway -->|HTTP/JWT| AuthService
    AuthService -->|EF Core| PostgreSQL
    AuthService -->|Events| RabbitMQ
    
    style AuthService fill:#4CAF50
    style PostgreSQL fill:#336791
    style Gateway fill:#FF6B6B
```

### Clean Architecture Layers

The Auth Service follows Clean Architecture with clear separation of concerns:

```
CodingAgent.Services.Auth/
├── Domain/                    # Business entities and rules
│   ├── Entities/             # User, Session, ApiKey
│   └── Repositories/         # Repository interfaces
├── Application/              # Use cases and business logic
│   ├── Services/            # IAuthService, AuthService
│   ├── DTOs/                # Data transfer objects
│   └── Validators/          # FluentValidation rules
├── Infrastructure/           # External dependencies
│   ├── Persistence/         # EF Core, DbContext
│   └── Security/            # BCrypt, JWT generation
└── Api/                     # HTTP endpoints
    └── Endpoints/           # Minimal API endpoints
```

**Layer Dependencies** (Inner → Outer):
- **Domain**: No dependencies (pure business logic)
- **Application**: Depends on Domain
- **Infrastructure**: Depends on Domain and Application
- **Api**: Depends on all layers

---

## Authentication Flows

### 1. User Registration Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant A as Auth Service
    participant DB as PostgreSQL
    
    C->>A: POST /auth/register<br/>{username, email, password}
    A->>A: Validate input (FluentValidation)
    A->>DB: Check if user exists
    alt User exists
        A-->>C: 400 Bad Request<br/>"Username or email already exists"
    else New user
        A->>A: Hash password (BCrypt, work factor 12)
        A->>DB: Create user record
        A->>A: Generate access token (JWT, 15min)
        A->>A: Generate refresh token (64 bytes, SHA256)
        A->>DB: Create session with hashed refresh token
        A-->>C: 201 Created<br/>{accessToken, refreshToken, expiresIn: 900}
    end
```

**Security Notes**:
- Password is hashed with BCrypt (work factor 12) before storage
- Refresh token is stored as SHA256 hash (not plaintext)
- Session tracks IP address and User-Agent for audit purposes

### 2. User Login Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant A as Auth Service
    participant DB as PostgreSQL
    
    C->>A: POST /auth/login<br/>{username, password}
    A->>A: Validate input
    A->>DB: Find user by username
    alt User not found or inactive
        A-->>C: 401 Unauthorized
    else User found
        A->>A: Verify password (BCrypt)
        alt Invalid password
            A-->>C: 401 Unauthorized
        else Valid password
            A->>A: Generate access token (15min)
            A->>A: Generate refresh token
            A->>DB: Create new session
            A-->>C: 200 OK<br/>{accessToken, refreshToken, expiresIn: 900}
        end
    end
```

### 3. Token Refresh Flow (with Rotation)

```mermaid
sequenceDiagram
    participant C as Client
    participant A as Auth Service
    participant DB as PostgreSQL
    
    C->>A: POST /auth/refresh<br/>{refreshToken}
    A->>A: Hash refresh token (SHA256)
    A->>DB: Find session by token hash
    alt Invalid or expired token
        A-->>C: 401 Unauthorized
    else Valid token
        A->>DB: Get user and verify active
        A->>DB: Revoke old session (token rotation)
        A->>A: Generate new access token (15min)
        A->>A: Generate new refresh token
        A->>DB: Create new session
        A-->>C: 200 OK<br/>{accessToken, refreshToken, expiresIn: 900}
    end
```

**Refresh Token Rotation**: Each token refresh invalidates the old refresh token and issues a new one. This prevents token reuse attacks.

### 4. Logout Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant A as Auth Service
    participant DB as PostgreSQL
    
    C->>A: POST /auth/logout<br/>Header: X-Refresh-Token
    A->>A: Extract refresh token from header
    A->>A: Hash refresh token
    A->>DB: Find session by token hash
    A->>DB: Revoke session (set IsRevoked=true)
    A-->>C: 204 No Content
```

---

## API Endpoints

### Base URL
- **Development**: `http://localhost:5007/auth`
- **Gateway (Production)**: `https://api.example.com/api/auth`

### Endpoints Summary

| Endpoint | Method | Auth Required | Description |
|----------|--------|---------------|-------------|
| `/auth/register` | POST | ❌ | Register new user |
| `/auth/login` | POST | ❌ | Authenticate user |
| `/auth/refresh` | POST | ❌ | Refresh access token |
| `/auth/me` | GET | ✅ | Get current user info |
| `/auth/logout` | POST | ✅ | Revoke refresh token |
| `/auth/change-password` | POST | ✅ | Change user password |

---

### 1. Register User

**Endpoint**: `POST /auth/register`

**Request Body**:
```json
{
  "username": "johndoe",
  "email": "john.doe@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!"
}
```

**Validation Rules**:
- Username: 3-50 characters, alphanumeric + hyphens/underscores only
- Email: Valid email format, max 255 characters
- Password: Min 8 characters, must contain uppercase, lowercase, number, and special character
- ConfirmPassword: Must match password

**Success Response** (201 Created):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Error Responses**:
- `400 Bad Request`: Validation errors or duplicate username/email
- `500 Internal Server Error`: Server error

**cURL Example**:
```bash
curl -X POST http://localhost:5007/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john.doe@example.com",
    "password": "SecurePass123!",
    "confirmPassword": "SecurePass123!"
  }'
```

---

### 2. Login

**Endpoint**: `POST /auth/login`

**Request Body**:
```json
{
  "username": "johndoe",
  "password": "SecurePass123!"
}
```

**Success Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Error Responses**:
- `400 Bad Request`: Validation errors
- `401 Unauthorized`: Invalid credentials or inactive account

**cURL Example**:
```bash
curl -X POST http://localhost:5007/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "password": "SecurePass123!"
  }'
```

---

### 3. Refresh Token

**Endpoint**: `POST /auth/refresh`

**Request Body**:
```json
{
  "refreshToken": "jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n..."
}
```

**Success Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "bmV3IHRva2VuIGFmdGVyIHJvdGF0aW9u...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Error Responses**:
- `400 Bad Request`: Missing or invalid refresh token
- `401 Unauthorized`: Expired or revoked token

**cURL Example**:
```bash
curl -X POST http://localhost:5007/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n..."
  }'
```

---

### 4. Get Current User

**Endpoint**: `GET /auth/me`

**Headers**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "johndoe",
  "email": "john.doe@example.com",
  "roles": ["User"],
  "createdAt": "2025-10-27T10:00:00Z"
}
```

**Error Responses**:
- `401 Unauthorized`: Missing or invalid access token
- `404 Not Found`: User not found

**cURL Example**:
```bash
curl -X GET http://localhost:5007/auth/me \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

---

### 5. Logout

**Endpoint**: `POST /auth/logout`

**Headers**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-Refresh-Token: jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n...
```

**Success Response** (204 No Content)

**Error Responses**:
- `400 Bad Request`: Missing X-Refresh-Token header
- `401 Unauthorized`: Invalid access token

**cURL Example**:
```bash
curl -X POST http://localhost:5007/auth/logout \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "X-Refresh-Token: jZXN0IHRva2VuIGZvciByZWZyZXNoaW5n..."
```

---

### 6. Change Password

**Endpoint**: `POST /auth/change-password`

**Headers**:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body**:
```json
{
  "currentPassword": "SecurePass123!",
  "newPassword": "NewSecurePass456!",
  "confirmNewPassword": "NewSecurePass456!"
}
```

**Success Response** (204 No Content)

**Error Responses**:
- `400 Bad Request`: Validation errors or passwords don't match
- `401 Unauthorized`: Invalid current password or access token

**Security Note**: All existing sessions are revoked when password is changed.

**cURL Example**:
```bash
curl -X POST http://localhost:5007/auth/change-password \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "SecurePass123!",
    "newPassword": "NewSecurePass456!",
    "confirmNewPassword": "NewSecurePass456!"
  }'
```

---

## JWT Token Structure

### Access Token Claims

The access token is a signed JWT containing the following claims:

```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "unique_name": "johndoe",
  "email": "john.doe@example.com",
  "jti": "7b8c9d0e-1f2a-3b4c-5d6e-7f8a9b0c1d2e",
  "uid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "role": ["User"],
  "iss": "CodingAgent",
  "aud": "CodingAgent.API",
  "exp": 1730034600,
  "nbf": 1730033700
}
```

**Claim Descriptions**:
- `sub`: Subject (User ID)
- `unique_name`: Username
- `email`: User email address
- `jti`: JWT ID (unique token identifier)
- `uid`: User ID (custom claim for convenience)
- `role`: User roles (array)
- `iss`: Issuer (configured via `Jwt:Issuer`)
- `aud`: Audience (configured via `Jwt:Audience`)
- `exp`: Expiration time (Unix timestamp, 15 minutes from issue)
- `nbf`: Not Before time (Unix timestamp)

### Token Configuration

| Setting | Value | Configurable | Notes |
|---------|-------|--------------|-------|
| Access Token Lifetime | 15 minutes | Yes (Jwt:AccessTokenLifetime) | Short-lived for security |
| Refresh Token Lifetime | 7 days | Yes (Jwt:RefreshTokenLifetime) | Long-lived for UX |
| Algorithm | HS256 | No | HMAC with SHA-256 |
| Clock Skew | 0 seconds | No | No tolerance for time drift |

---

## Security Considerations

### OWASP Top 10 Alignment

The Auth Service implements protections against OWASP Top 10 vulnerabilities:

#### 1. **A01:2021 – Broken Access Control** ✅
- **Protection**: JWT-based authentication with role claims
- **Implementation**: `[Authorize]` attributes on protected endpoints
- **Validation**: User ID extracted from JWT claims, no user-supplied IDs accepted

#### 2. **A02:2021 – Cryptographic Failures** ✅
- **Password Hashing**: BCrypt with work factor 12 (adaptive, resistant to GPU attacks)
- **Refresh Token Storage**: SHA256 hashing before database storage
- **JWT Signing**: HMAC-SHA256 with secret key from configuration
- **HTTPS**: Enforced in production via `RequireHttpsMetadata = true`

#### 3. **A03:2021 – Injection** ✅
- **SQL Injection**: EF Core parameterized queries (no raw SQL)
- **Input Validation**: FluentValidation on all inputs
- **Email Validation**: Regex-based email format validation

#### 4. **A04:2021 – Insecure Design** ✅
- **Refresh Token Rotation**: Old tokens invalidated on refresh
- **Session Management**: Tracked with IP and User-Agent
- **Password Change**: All sessions revoked on password change
- **Account Lockout**: Ready for rate limiting integration (Gateway level)

#### 5. **A05:2021 – Security Misconfiguration** ✅
- **CORS**: Configured explicitly (no wildcard in production)
- **Error Handling**: Generic error messages (no stack traces)
- **Secrets Management**: No hardcoded secrets, environment variables required
- **HTTPS**: Enforced in production

#### 6. **A07:2021 – Identification and Authentication Failures** ✅
- **Session Management**: Secure session storage with expiration
- **Weak Passwords**: Strong password policy enforced
- **Credential Stuffing**: Rate limiting at Gateway (10 req/min per IP)
- **Token Expiration**: Short-lived access tokens (15min)

#### 7. **A09:2021 – Security Logging and Monitoring Failures** ✅
- **Structured Logging**: Serilog with correlation IDs
- **OpenTelemetry**: Distributed tracing for all operations
- **Audit Trail**: Session creation/revocation logged
- **Failed Login Attempts**: Logged with username and IP

### Security Best Practices

#### Password Security
```csharp
// BCrypt work factor 12 (adaptive hashing)
private const int WorkFactor = 12;
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
}
```

**Why BCrypt with Work Factor 12?**
- **Adaptive**: Can increase work factor as hardware improves
- **Salted**: Built-in per-password salt generation
- **Slow by Design**: ~250ms per hash (prevents brute-force)
- **Industry Standard**: Recommended by OWASP and NIST

#### Refresh Token Security
```csharp
// Generate cryptographically secure 64-byte token
private static string GenerateRefreshToken()
{
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);
}

// Store as SHA256 hash (not plaintext)
private static string HashRefreshToken(string refreshToken)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
    return Convert.ToBase64String(hashBytes);
}
```

#### JWT Secret Management
```csharp
// ❌ NEVER do this
var jwtSecret = "my-super-secret-key";

// ✅ Always use configuration
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret) && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("JWT:Secret is required in Production");
}
```

### Rate Limiting

Rate limiting is configured at the **Gateway level** (not in Auth Service):

| Endpoint | Limit | Window | Scope |
|----------|-------|--------|-------|
| `/auth/login` | 10 requests | 1 minute | Per IP |
| `/auth/register` | 5 requests | 1 hour | Per IP |
| `/auth/refresh` | 60 requests | 1 hour | Per user |
| All others | 100 requests | 1 minute | Per IP |

**Implementation**: ASP.NET Core Rate Limiting middleware in Gateway with Redis-based distributed storage.

### CORS Configuration

**Development**:
```csharp
options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});
```

**Production** (⚠️ Must be configured):
```csharp
options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins("https://app.example.com", "https://dashboard.example.com")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

---

## Database Schema

### Entity Relationship Diagram

```mermaid
erDiagram
    Users ||--o{ Sessions : has
    Users ||--o{ ApiKeys : has
    
    Users {
        uuid Id PK
        varchar(50) Username UK
        varchar(255) Email UK
        varchar(255) PasswordHash
        varchar(500) Roles
        timestamptz CreatedAt
        timestamptz UpdatedAt
        boolean IsActive
    }
    
    Sessions {
        uuid Id PK
        uuid UserId FK
        varchar(255) RefreshTokenHash UK
        timestamptz ExpiresAt
        varchar(45) IpAddress
        varchar(500) UserAgent
        timestamptz CreatedAt
        boolean IsRevoked
        timestamptz RevokedAt
    }
    
    ApiKeys {
        uuid Id PK
        uuid UserId FK
        varchar(255) KeyHash UK
        varchar(100) Name
        timestamptz ExpiresAt
        timestamptz CreatedAt
        boolean IsRevoked
        timestamptz RevokedAt
        timestamptz LastUsedAt
    }
```

### Schema Details

**Schema Name**: `auth`

**Tables**:
1. **users**: User accounts
2. **sessions**: Refresh token sessions
3. **api_keys**: API keys for programmatic access (future feature)

**Indexes**:
- `IX_users_Username` (unique)
- `IX_users_Email` (unique)
- `IX_sessions_RefreshTokenHash` (for fast lookup)
- `IX_sessions_UserId` (for user session queries)
- `IX_sessions_IsRevoked_ExpiresAt` (for cleanup queries)

**Foreign Keys**:
- `sessions.UserId` → `users.Id` (CASCADE DELETE)
- `api_keys.UserId` → `users.Id` (CASCADE DELETE)

---

## Configuration

### Environment Variables

#### Required in Production

```bash
# JWT Configuration (REQUIRED)
Jwt__Secret=<64-character-random-string>  # Generate with: openssl rand -base64 64
Jwt__Issuer=CodingAgent
Jwt__Audience=CodingAgent.API

# Database Connection (REQUIRED)
ConnectionStrings__AuthDb=Host=postgres;Port=5432;Database=coding_agent;Username=app_user;Password=<secure-password>

# RabbitMQ Configuration (REQUIRED)
RabbitMQ__Host=rabbitmq
RabbitMQ__Username=guest
RabbitMQ__Password=<secure-password>
```

#### Optional

```bash
# OpenTelemetry (optional but recommended)
OpenTelemetry__Endpoint=http://jaeger:4317

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__WriteTo__0__Name=Console
Serilog__WriteTo__1__Name=Seq
Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
```

### appsettings.json

**Development** (`appsettings.Development.json`):
```json
{
  "Jwt": {
    "Secret": "dev-secret-key-at-least-32-characters-long-for-testing",
    "Issuer": "CodingAgent",
    "Audience": "CodingAgent.API"
  },
  "ConnectionStrings": {
    "AuthDb": "Host=localhost;Port=5432;Database=coding_agent_dev;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

**Production** (`appsettings.Production.json`):
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

⚠️ **Never commit secrets to source control. Use environment variables or secrets management (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).**

---

## Deployment Guide

### Prerequisites

1. **PostgreSQL 16+** with `auth` schema
2. **RabbitMQ 3.12+** for event messaging
3. **.NET 9 SDK** (for building)
4. **Docker** (for containerized deployment)

### Deployment Steps

#### 1. Database Setup

```sql
-- Create database
CREATE DATABASE coding_agent;

-- Create app user
CREATE USER app_user WITH PASSWORD 'secure_password';

-- Grant permissions
GRANT ALL PRIVILEGES ON DATABASE coding_agent TO app_user;

-- Connect to database
\c coding_agent

-- Create schema (EF Core will create tables via migrations)
CREATE SCHEMA IF NOT EXISTS auth;
GRANT ALL ON SCHEMA auth TO app_user;
```

#### 2. Environment Configuration

Create `.env` file (NEVER commit this):
```bash
JWT_SECRET=$(openssl rand -base64 64)
CONNECTIONSTRINGS__AUTHDB=Host=postgres;Port=5432;Database=coding_agent;Username=app_user;Password=secure_password
RABBITMQ__HOST=rabbitmq
RABBITMQ__USERNAME=app_user
RABBITMQ__PASSWORD=secure_password
ASPNETCORE_ENVIRONMENT=Production
```

#### 3. Build Docker Image

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/Auth/CodingAgent.Services.Auth/CodingAgent.Services.Auth.csproj", "Services/Auth/"]
COPY ["src/SharedKernel/CodingAgent.SharedKernel/CodingAgent.SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "Services/Auth/CodingAgent.Services.Auth.csproj"
COPY src/ .
RUN dotnet build "Services/Auth/CodingAgent.Services.Auth/CodingAgent.Services.Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Auth/CodingAgent.Services.Auth/CodingAgent.Services.Auth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.Auth.dll"]
```

Build command:
```bash
docker build -t coding-agent/auth-service:1.0.0 -f deployment/docker/auth-service/Dockerfile .
```

#### 4. Deploy with Docker Compose

```yaml
# docker-compose.yml
services:
  auth-service:
    image: coding-agent/auth-service:1.0.0
    ports:
      - "5007:8080"
    environment:
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__Issuer=CodingAgent
      - Jwt__Audience=CodingAgent.API
      - ConnectionStrings__AuthDb=${CONNECTIONSTRINGS__AUTHDB}
      - RabbitMQ__Host=${RABBITMQ__HOST}
      - RabbitMQ__Username=${RABBITMQ__USERNAME}
      - RabbitMQ__Password=${RABBITMQ__PASSWORD}
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - postgres
      - rabbitmq
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

Start services:
```bash
docker-compose up -d
```

#### 5. Verify Deployment

```bash
# Health check
curl http://localhost:5007/health

# Ping endpoint
curl http://localhost:5007/ping

# Test registration
curl -X POST http://localhost:5007/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test@1234",
    "confirmPassword": "Test@1234"
  }'
```

### Kubernetes Deployment

See `deployment/k8s/auth-service/` for Kubernetes manifests:
- `deployment.yaml`: Auth Service deployment
- `service.yaml`: ClusterIP service
- `configmap.yaml`: Non-secret configuration
- `secret.yaml`: JWT secret and database credentials (use sealed secrets in production)

---

## Testing

### Test Coverage Summary

**Unit Tests**: 18 tests (100% passing)
- `BcryptPasswordHasherTests`: 3 tests
- `AuthServiceTests`: 15 tests

**Integration Tests**: 9 tests (Testcontainers-based, requires Docker)

**Total Test Lines**: ~600 LOC

### Running Tests

#### Unit Tests Only (Fast, <5 seconds)
```bash
dotnet test --filter "Category=Unit" --verbosity quiet --nologo src/Services/Auth/CodingAgent.Services.Auth.Tests/
```

#### Integration Tests (Requires Docker)
```bash
dotnet test --filter "Category=Integration" --verbosity quiet --nologo src/Services/Auth/CodingAgent.Services.Auth.Tests/
```

#### All Tests
```bash
dotnet test --settings .runsettings --verbosity quiet --nologo src/Services/Auth/CodingAgent.Services.Auth.Tests/
```

### Test Categories

| Category | Count | Description | Infrastructure |
|----------|-------|-------------|----------------|
| Unit | 18 | In-memory, no external dependencies | None |
| Integration | 9 | Full HTTP tests with real database | Testcontainers (PostgreSQL) |

### Coverage Goals

- **Domain Layer**: 100% (critical business logic)
- **Application Layer**: ≥95% (use cases)
- **Infrastructure Layer**: ≥85% (adapters)
- **Api Layer**: ≥90% (endpoints)

**Current Coverage**: ~90% overall (estimated)

---

## Troubleshooting

### Common Issues

#### 1. "JWT:Secret is required in Production environment"

**Cause**: Missing or empty `Jwt:Secret` configuration in production.

**Solution**:
```bash
# Generate a strong secret
openssl rand -base64 64

# Set environment variable
export Jwt__Secret="<generated-secret>"
```

#### 2. "AuthDb connection string is required in Production"

**Cause**: Missing database connection string.

**Solution**:
```bash
export ConnectionStrings__AuthDb="Host=postgres;Port=5432;Database=coding_agent;Username=app_user;Password=<password>"
```

#### 3. "Invalid username or password" (401 Unauthorized)

**Possible Causes**:
- Incorrect username or password
- User account is deactivated (`IsActive = false`)
- Password not matching BCrypt hash

**Debug Steps**:
```bash
# Check logs for failed login attempts
docker logs auth-service | grep "Login failed"

# Verify user exists in database
psql -d coding_agent -c "SELECT id, username, email, is_active FROM auth.users WHERE username='<username>';"
```

#### 4. "Invalid or expired refresh token" (401 Unauthorized)

**Possible Causes**:
- Refresh token expired (>7 days old)
- Token already used (rotation enforced)
- Session revoked (logout or password change)

**Debug Steps**:
```sql
-- Check session status
SELECT id, user_id, expires_at, is_revoked, created_at
FROM auth.sessions
WHERE refresh_token_hash = encode(sha256('<token>'::bytea), 'base64');
```

#### 5. Integration Tests Failing with "Docker not available"

**Cause**: Docker daemon not running or not accessible.

**Solution**:
```bash
# Start Docker
sudo systemctl start docker

# Or skip integration tests
dotnet test --filter "Category=Unit"
```

### Logging and Monitoring

#### View Logs

**Docker**:
```bash
docker logs -f auth-service
```

**Kubernetes**:
```bash
kubectl logs -f deployment/auth-service -n coding-agent
```

#### Key Log Messages

| Level | Message Pattern | Meaning |
|-------|----------------|---------|
| INFO | `Login successful for user: {Username}` | User logged in |
| WARNING | `Login failed: Invalid password for user - {Username}` | Wrong password |
| WARNING | `Registration failed: Username or email already exists` | Duplicate user |
| INFO | `Token refresh successful for user: {UserId}` | Token refreshed |
| INFO | `Password changed successfully for user: {UserId}` | Password changed |

#### OpenTelemetry Traces

View traces in Jaeger UI:
```
http://localhost:16686
```

Search for service: `auth-service`

Key spans:
- `Login`
- `Register`
- `RefreshToken`
- `GetCurrentUser`
- `ChangePassword`

---

## Production Checklist

### Pre-Deployment

- [ ] JWT secret generated and configured (64+ characters)
- [ ] Database connection string configured with secure credentials
- [ ] RabbitMQ connection configured
- [ ] CORS origins configured (no wildcard `*`)
- [ ] HTTPS enforced (`RequireHttpsMetadata = true`)
- [ ] Rate limiting configured at Gateway
- [ ] OpenTelemetry endpoint configured
- [ ] Serilog configured with structured logging
- [ ] Health checks enabled and verified

### Security

- [ ] No hardcoded secrets in code
- [ ] JWT secret stored in secure vault (Azure Key Vault, AWS Secrets Manager)
- [ ] Database credentials rotated and stored securely
- [ ] BCrypt work factor confirmed at 12
- [ ] Refresh tokens stored as hashes (not plaintext)
- [ ] HTTPS enforced for all endpoints
- [ ] Rate limiting active on login/register endpoints
- [ ] Failed login attempts logged
- [ ] Session management audit trail enabled

### Testing

- [ ] All unit tests passing (18/18)
- [ ] Integration tests passing (9/9) or documented exceptions
- [ ] Load testing completed (min 100 req/s)
- [ ] Security scanning completed (no critical vulnerabilities)
- [ ] Penetration testing completed (auth flows)

### Monitoring

- [ ] Health checks responding correctly
- [ ] Metrics exported to Prometheus
- [ ] Traces sent to Jaeger/Zipkin
- [ ] Logs aggregated in Seq/ELK
- [ ] Alerts configured for:
  - High failed login rate
  - Database connection failures
  - RabbitMQ connection failures
  - High response times (>1s)

### Documentation

- [ ] API documentation published (OpenAPI)
- [ ] Deployment runbook created
- [ ] Incident response plan documented
- [ ] Backup and recovery procedures documented

---

## Technical Debt and Future Enhancements

### Known Limitations

1. **Rate Limiting**: Currently implemented at Gateway level. Consider per-user rate limiting in Auth Service.
2. **API Keys**: Entity exists but endpoints not implemented yet.
3. **Email Verification**: Not implemented (users can register without email verification).
4. **Password Reset**: Not implemented (requires email service integration).

### Phase 5 Recommendations

1. **Two-Factor Authentication (2FA)**
   - TOTP-based (Google Authenticator, Authy)
   - SMS-based (Twilio integration)
   - Backup codes for recovery

2. **Single Sign-On (SSO)**
   - OAuth 2.0 providers (Google, GitHub, Microsoft)
   - SAML 2.0 for enterprise

3. **Enhanced Security**
   - Account lockout after failed attempts
   - Passwordless authentication (magic links, WebAuthn)
   - Security questions for account recovery

4. **Email Service Integration**
   - Email verification on registration
   - Password reset via email
   - Security alerts (new login from unknown device)

5. **Admin Features**
   - User management API (activate/deactivate users)
   - Role management (create/assign roles)
   - Session management (view/revoke user sessions)
   - Audit log API (view authentication events)

6. **API Key Management**
   - Create API keys for programmatic access
   - Revoke/regenerate API keys
   - Key usage analytics

---

## References

- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [NIST Digital Identity Guidelines](https://pages.nist.gov/800-63-3/)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
- [BCrypt Work Factor Analysis](https://auth0.com/blog/hashing-in-action-understanding-bcrypt/)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)

---

**Document Version**: 1.0.0  
**Last Updated**: October 27, 2025  
**Maintainer**: Platform Team  
**Review Schedule**: Quarterly
