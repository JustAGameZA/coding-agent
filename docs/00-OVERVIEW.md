# Coding Agent v2.0 - Microservices Architecture Overview

**Status**: Architecture Design Phase
**Version**: 2.0.0
**Last Updated**: October 24, 2025
**Architecture Style**: Microservices with API Gateway
**Target Completion**: Q2 2026 (6 months)

---

## Executive Summary

This document outlines the complete architectural redesign of the Self-Hosted Coding Agent from a monolithic Clean Architecture to a modular microservices architecture. This rewrite addresses scalability, independent deployment, and feature modularity requirements while maintaining the core business value delivered by v1.0.

### Key Drivers for Rewrite

1. **Modularity**: Enable/disable features independently via configuration
2. **Scalability**: Scale individual services based on load (ML inference vs Chat vs GitHub operations)
3. **Independent Deployment**: Deploy services separately without system-wide downtime
4. **Technology Optimization**: Use optimal tech stack per service (e.g., Python for ML, .NET for core logic)
5. **Team Growth**: Support parallel development by multiple teams on different services
6. **Fault Isolation**: Service failures don't cascade to entire system

### Architecture Principles

- **Domain-Driven Design (DDD)**: Clear bounded contexts per microservice
- **API-First**: OpenAPI-specified contracts before implementation
- **Event-Driven**: Loose coupling via message bus (RabbitMQ + MassTransit)
- **Observability-First**: Built-in tracing, metrics, and structured logging
- **12-Factor App**: Stateless services, externalized configuration, disposable instances
- **DevOps Culture**: Infrastructure as Code, automated testing, CI/CD per service

---

## System Context

### What We're Building

A **modular AI coding assistant platform** that helps developers:

- Execute autonomous coding tasks from GitHub issues
- Chat with AI agents in real-time (WebSocket + REST)
- Monitor CI/CD pipelines and auto-generate fixes
- Automate browser-based testing and data extraction
- Classify and route tasks intelligently using ML
- Integrate with GitHub repositories for PR creation

### Target Users

- **Primary**: Solo developers and small teams (1-5 developers)
- **Secondary**: AI/ML engineers experimenting with agent workflows
- **Tertiary**: DevOps teams automating CI/CD operations

### Non-Functional Requirements

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| **API Latency** | p95 < 500ms | Prometheus histogram |
| **Availability** | 99.5% uptime | Health check logs |
| **Throughput** | 100 concurrent users | Load testing with k6 |
| **Scalability** | Horizontal scaling per service | K8s HPA |
| **Deployment** | < 5 min rollout | CI/CD pipeline metrics |
| **Recovery** | < 30 sec automatic restart | K8s liveness probes |

---

## High-Level Architecture

### Microservices Map

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer                             │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────────┐   │
│  │   Angular   │  │   Mobile    │  │  External Systems    │   │
│  │  Dashboard  │  │   (Future)  │  │  (GitHub Webhooks)   │   │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬───────────┘   │
└─────────┼─────────────────┼──────────────────────┼───────────────┘
          │                 │                      │
          └─────────────────┼──────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API Gateway (YARP)                          │
│  • Authentication & Authorization (JWT)                          │
│  • Rate Limiting & Throttling                                   │
│  • Request Routing & Load Balancing                             │
│  • Circuit Breaking & Retry Logic                               │
│  • OpenTelemetry Integration                                    │
└───┬───────┬───────┬───────┬───────┬───────┬──────────┬─────┬────┘
    │       │       │       │       │       │          │     │
    ▼       ▼       ▼       ▼       ▼       ▼          ▼     ▼
┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐  ┌─────────┐ ┌──────┐
│Chat │ │Orch-│ │ ML/ │ │GitHub│ │Browser│ │CI/CD│  │Dashboard│ │Ollama│
│Svc  │ │estr-│ │Class│ │  Svc │ │  Svc  │ │ Mon │  │   Svc   │ │ Svc  │
│     │ │ation│ │ifier│ │      │ │       │ │     │  │         │ │      │
└──┬──┘ └──┬──┘ └──┬──┘ └──┬───┘ └───┬───┘ └──┬──┘  └────┬────┘ └──┬───┘
   │       │       │       │         │        │          │         │
   │       └───────┴───────┴─────────┴────────┴──────────┼─────────┤
   │                                                       │         │
   └───────────────────────────┬───────────────────────────┘         │
                               │                                     │
                               ▼                                     │
              ┌───────────────────────────────┐                     │
              │    Message Bus (RabbitMQ)     │                     │
              │   • MassTransit Integration   │                     │
              │   • Event Publishing/Subs     │                     │
              │   • Saga Coordination         │                     │
              └───────────────────────────────┘                     │
                               │                                     │
              ┌────────────────┼────────────────┐                   │
              │                │                │                   │
              ▼                ▼                ▼                   │
      ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
      │  PostgreSQL  │  │    Redis     │  │  Event Store │        │
      │  (Per-Schema)│  │   (Cache)    │  │  (Audit Log) │        │
      └──────────────┘  └──────────────┘  └──────────────┘        │
                                                                    │
              ┌─────────────────────────────────────────────────────┘
              │
              ▼
      ┌──────────────────┐
      │  Ollama Backend  │
      │   (LLM Models)   │
      │  • codellama:13b │
      │  • deepseek:6.7b │
      │  • mistral:7b    │
      └──────────────────┘
```

### Service Responsibilities

| Service | Responsibility | Technology | Port |
|---------|---------------|------------|------|
| **API Gateway** | Single entry point, routing, auth | YARP (.NET 9) | 5000 |
| **Auth Service** | JWT authentication, user management | .NET 9 + BCrypt | 5007 |
| **Chat Service** | Real-time WebSocket chat, conversation mgmt | .NET 9 + SignalR | 5001 |
| **Orchestration Service** | Task execution, agent orchestration | .NET 9 | 5002 |
| **ML Classifier** | Task classification, ML inference | Python (FastAPI) | 5003 |
| **GitHub Service** | Repository ops, PR creation, webhooks | .NET 9 | 5004 |
| **Browser Service** | Playwright automation, web scraping | .NET 9 | 5005 |
| **CI/CD Monitor** | Build monitoring, automated fixes | .NET 9 | 5006 |
| **Dashboard Service** | BFF for Angular frontend | .NET 9 | 5003 |
| **Ollama Service** | Local LLM provider, on-premise inference | .NET 9 | 5008 |

---

## Technology Stack

### Backend Services (.NET 9)

- **Framework**: .NET 9 Minimal APIs
- **Communication**: REST (HTTP/2), gRPC (internal), SignalR (WebSocket)
- **Message Bus**: MassTransit + RabbitMQ
- **Data Access**: Entity Framework Core 9
- **Caching**: StackExchange.Redis
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Testing**: xUnit, Moq, FluentAssertions, Testcontainers

### ML Service (Python)

- **Framework**: FastAPI + Uvicorn
- **ML Library**: scikit-learn, XGBoost, ONNX Runtime
- **Async**: asyncio, httpx
- **Testing**: pytest, pytest-asyncio

### Infrastructure

- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Database**: PostgreSQL 16
- **Cache**: Redis 7
- **Message Queue**: RabbitMQ 3.12
- **Observability**: OpenTelemetry → Prometheus + Grafana + Seq
- **Container**: Docker + Docker Compose
- **Orchestration**: Kubernetes (Helm charts)

### Frontend

- **Framework**: Angular 20.3
- **State Management**: NgRx Signal Store
- **UI Components**: Angular Material + Custom library
- **Real-time**: SignalR Client
- **Testing**: Jasmine, Karma, Cypress

---

## Data Architecture

### Database Strategy: Hybrid Approach

**Phase 1 (Months 1-3)**: Logical Separation

- Single PostgreSQL instance
- Separate schema per service
- Shared `auth` schema for users/sessions
- Enables service independence without operational complexity

**Phase 2 (Months 4-6)**: Physical Separation (Optional)

- Separate PostgreSQL instances per service
- Migrate schemas to dedicated databases
- Implement distributed transactions via SAGA pattern

### Service Data Ownership

| Service | Database Schema | Primary Entities |
|---------|----------------|------------------|
| Chat Service | `chat` | Conversations, Messages, Attachments |
| Orchestration | `orchestration` | Tasks, Executions, Results |
| ML Classifier | `ml` | TrainingSamples, Models, Predictions |
| GitHub Service | `github` | Repositories, PullRequests, Issues |
| CI/CD Monitor | `cicd` | BuildLogs, FailureReports, Fixes |
| Auth (Shared) | `auth` | Users, Sessions, ApiKeys |

### Caching Strategy

- **Redis Cluster** (3 nodes for HA)
- **Per-Service Keyspace**: `{service}:*` pattern
- **TTL Strategy**: Short (5 min) for mutable, Long (1 hour) for immutable
- **Cache Invalidation**: Event-driven (listen to domain events)

---

## Communication Patterns

### Synchronous (REST)

**When to Use**:

- Client-initiated requests (user actions)
- Query operations (GET requests)
- Service-to-service calls requiring immediate response

**Example**: Gateway → Chat Service → Get conversation history

### Asynchronous (Events)

**When to Use**:

- Domain events (task completed, message sent)
- Fire-and-forget operations
- Cross-service notifications

**Example**: Orchestration publishes `TaskCompleted` → ML Classifier listens → Updates training data

### Event Types

| Event | Publisher | Subscribers | Purpose |
|-------|-----------|------------|---------|
| `TaskCreated` | Orchestration | ML Classifier | Collect classification feedback |
| `MessageSent` | Chat Service | Dashboard Service | Real-time notifications |
| `BuildFailed` | CI/CD Monitor | Orchestration | Trigger automated fix |
| `PullRequestCreated` | GitHub Service | Chat Service | Notify user in conversation |

---

## Security Architecture

### Authentication Flow

**Implementation**: ✅ Production-ready Auth Service (see `docs/AUTH-IMPLEMENTATION.md`)

```
1. User → Gateway: POST /api/auth/login (username, password)
2. Gateway → Auth Service: Forward request
3. Auth Service: 
   - Verify user exists and is active
   - Verify password (BCrypt work factor 12)
   - Generate JWT access token (15min, HS256)
   - Generate refresh token (7 days, SHA256 hashed)
   - Create session record (IP + User-Agent tracking)
4. Auth Service → Gateway: Return tokens
5. Gateway → User: { accessToken, refreshToken, expiresIn: 900 }
```

**Token Refresh Flow** (with rotation):
```
1. User → Gateway: POST /api/auth/refresh (refreshToken)
2. Auth Service:
   - Validate refresh token (check expiry, revocation)
   - Revoke old refresh token (rotation)
   - Generate new access + refresh tokens
3. Auth Service → User: New token pair
```

### Authorization

- **JWT Claims**: 
  - `sub`: User ID (GUID)
  - `unique_name`: Username
  - `email`: User email
  - `role`: User roles (array, e.g., ["User", "Admin"])
  - `jti`: JWT ID (unique token identifier)
  - `exp`: Expiration (Unix timestamp, 15 minutes from issue)
  - `iss`: "CodingAgent"
  - `aud`: "CodingAgent.API"

- **Role-Based Access Control (RBAC)**: 
  - User: Default role for all registered users
  - Admin: Future feature (user management, system config)
  
- **Service-to-Service**: API Key in `X-API-Key` header (entity exists, endpoints pending)

- **Protected Endpoints**: Use `[Authorize]` attribute, JWT validated by Gateway

### Secrets Management

- **Development**: 
  - JWT secret in appsettings.Development.json (dev-only secret)
  - Database connection string in environment variables
  
- **Production**: 
  - JWT secret from environment variable (required)
  - Secrets stored in Azure Key Vault / AWS Secrets Manager / Kubernetes Secrets
  - No hardcoded secrets in code (enforced by startup validation)
  
- **Rotation**: 
  - JWT secret: Manual rotation (requires service restart)
  - Refresh tokens: Automatic rotation on each refresh (security best practice)
  - Database credentials: Automated via infrastructure (90-day rotation recommended)

---

## Observability

### Three Pillars

1. **Tracing**: OpenTelemetry → Jaeger (distributed tracing)
2. **Metrics**: Prometheus → Grafana (dashboards)
3. **Logging**: Serilog → Seq (structured logs)

### Key Metrics

- **Request Duration**: Histogram per service (`http_request_duration_seconds`)
- **Request Count**: Counter by status code (`http_requests_total`)
- **Error Rate**: Gauge (`error_rate_percent`)
- **Queue Depth**: Gauge per service (`rabbitmq_queue_depth`)
- **Cache Hit Rate**: Gauge (`redis_cache_hit_rate`)

### Correlation IDs

- Generated at Gateway: `X-Correlation-Id` header
- Propagated through all services
- Logged in every log entry
- Included in event metadata

---

## Deployment Strategy

### Local Development

```bash
# Start all services with hot reload
docker compose -f docker-compose.dev.yml up -d

# Services auto-reload on code changes
# PostgreSQL, Redis, RabbitMQ run in containers
# .NET services run on host with `dotnet watch`
```

### CI/CD Pipeline (Per Service)

```yaml
Trigger: PR to main branch
1. Build: dotnet build
2. Unit Tests: dotnet test (coverage > 85%)
3. Integration Tests: Testcontainers
4. Contract Tests: Pact verification
5. Docker Build: Build + push to registry
6. Deploy to Staging: Helm upgrade
7. E2E Tests: Run full suite
8. Deploy to Production: Blue-green deployment
```

### Kubernetes Deployment

- **Namespace per Environment**: `dev`, `staging`, `prod`
- **Helm Charts**: One per service
- **Auto-Scaling**: HPA based on CPU (70%) and memory (80%)
- **Health Checks**: `/health/live` (liveness), `/health/ready` (readiness)

---

## Migration Strategy

### Parallel Run Approach

**Months 1-3**: Build new services while old system runs
**Months 4-5**: Feature flags route traffic to new services
**Month 6**: Full cutover, decommission old system

### Feature Flags

```json
{
  "Features": {
    "UseLegacyChat": false,
    "UseLegacyOrchestration": false,
    "UseLegacyML": false
  }
}
```

### Data Migration

- **Phase 1**: Dual writes (write to both old and new DBs)
- **Phase 2**: Backfill historical data (background job)
- **Phase 3**: Cutover reads to new DB
- **Phase 4**: Retire old DB

---

## Success Metrics

### Technical Metrics

- ✅ **Zero-downtime deployments**: All services support rolling updates
- ✅ **< 5 min build times**: Per-service CI/CD pipelines
- ✅ **85%+ code coverage**: Unit + integration tests
- ✅ **p95 latency < 500ms**: Prometheus tracking
- ✅ **99.5% uptime**: Health check monitoring

### Business Metrics

- ✅ **Feature velocity**: 2x faster feature delivery (parallel development)
- ✅ **Onboarding time**: < 1 day for new developers (clear service boundaries)
- ✅ **Incident recovery**: < 5 min (auto-scaling + circuit breakers)
- ✅ **Cost efficiency**: 30% reduction (scale services independently)

---

## Next Steps

1. **Review & Approve Architecture** (Week 1)
2. **Detailed Service Design** (Weeks 2-3)
3. **Proof of Concept** (Week 4): Gateway + Chat Service
4. **Full Implementation** (Months 2-5)
5. **Migration & Cutover** (Month 6)

---

## Related Documents

- [01-SERVICE-CATALOG.md](./01-SERVICE-CATALOG.md) - Detailed service specifications
- [02-API-CONTRACTS.md](./02-API-CONTRACTS.md) - OpenAPI definitions
- [03-DATA-MODELS.md](./03-DATA-MODELS.md) - Database schemas and entities
- [04-DEPLOYMENT-GUIDE.md](./04-DEPLOYMENT-GUIDE.md) - Infrastructure setup
- [05-TESTING-STRATEGY.md](./05-TESTING-STRATEGY.md) - Testing approach
- [06-MIGRATION-PLAN.md](./06-MIGRATION-PLAN.md) - Cutover strategy
- [ADRs/](./ADRs/) - Architecture Decision Records

---

**Document Owner**: Architecture Team
**Review Cycle**: Monthly
**Approval Required**: Technical Lead, Product Owner
