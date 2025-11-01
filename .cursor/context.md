# Project Context for Multi-Agent Mode

Quick reference guide for all agents to understand the project structure, standards, and conventions.

## Project Overview

**Name**: Coding Agent - AI-Powered Microservices Platform
**Architecture**: Microservices (10 services) with API Gateway
**Tech Stack**: .NET 9, Angular 20, Python 3.11+, Docker Compose
**Current Phase**: Phase 4 - Frontend & Dashboard (67% complete)

## Microservices Architecture

### Services (10 Total)

| Service | Port | Technology | Purpose |
|---------|------|------------|---------|
| **Gateway** | 5000 | .NET 9 YARP | API Gateway, routing, auth |
| **Auth** | 5007 | .NET 9 | JWT authentication, user management |
| **Chat** | 5001 | .NET 9 + SignalR | Real-time WebSocket chat |
| **Orchestration** | 5002 | .NET 9 | Task execution, agent orchestration |
| **ML Classifier** | 5003 | Python FastAPI | Task classification, ML inference |
| **GitHub** | 5004 | .NET 9 | Repository ops, PR creation |
| **Browser** | 5005 | .NET 9 | Playwright automation |
| **CI/CD Monitor** | 5006 | .NET 9 | Build monitoring, automated fixes |
| **Dashboard** | 5003 | .NET 9 | BFF for Angular frontend |
| **Ollama** | 5008 | .NET 9 | Local LLM provider |

### Infrastructure

- **Database**: PostgreSQL 16 (per-service schemas)
- **Cache**: Redis 7
- **Message Queue**: RabbitMQ 3.12 + MassTransit
- **Observability**: OpenTelemetry → Prometheus + Grafana + Jaeger
- **LLM**: Ollama (codellama:13b, deepseek:6.7b, mistral:7b)

## Directory Structure

```
coding-agent/
├── src/
│   ├── Gateway/              # API Gateway (YARP)
│   ├── Services/             # All microservices
│   │   ├── Auth/
│   │   ├── Chat/
│   │   ├── Orchestration/
│   │   ├── ML/               # Python FastAPI
│   │   ├── GitHub/
│   │   ├── Browser/
│   │   ├── CICDMonitor/
│   │   ├── Dashboard/
│   │   └── Ollama/
│   ├── SharedKernel/         # Shared contracts (NuGet)
│   └── Frontend/              # Angular dashboard
├── deployment/
│   └── docker-compose/       # Docker Compose configs
├── docs/                     # Documentation
│   ├── api/                  # OpenAPI specs
│   ├── ADRs/                 # Architecture Decision Records
│   └── runbooks/             # Operational procedures
└── test/                     # E2E tests
```

## Development Standards

### Testing

- **Target Coverage**: 85%+ per service
- **Unit Tests**: `[Trait("Category", "Unit")]` - domain logic, validators
- **Integration Tests**: `[Trait("Category", "Integration")]` - persistence, endpoints (Testcontainers)
- **E2E Tests**: Playwright for frontend workflows

### Commands

```powershell
# Build
dotnet build CodingAgent.sln --no-restore

# Test (all)
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo

# Test (unit only - fast)
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Unit"

# Test (integration only)
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Integration"
```

### Code Quality

- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → Api)
- **Validation**: FluentValidation for all requests
- **Observability**: OpenTelemetry spans + Prometheus metrics
- **Error Handling**: Consistent error responses
- **Documentation**: Update docs in same PR as code changes

### Commits

- **Format**: Conventional Commits
- **Example**: `feat(chat): add typing indicator events`
- **Format**: `type(scope): description`
- **Types**: feat, fix, docs, refactor, test, chore

## Documentation Standards

### Style Guide

Follow `docs/STYLEGUIDE.md`:
- Use kebab-case for filenames
- Include metadata header (Status, Version, Last Updated)
- Use relative links within repo
- Follow Markdown lint rules

### Key Documents

- **Architecture**: `docs/00-OVERVIEW.md`, `docs/01-SERVICE-CATALOG.md`
- **APIs**: `docs/api/*.yaml` (OpenAPI 3.0 specs)
- **Roadmap**: `docs/02-IMPLEMENTATION-ROADMAP.md`
- **Structure**: `docs/03-SOLUTION-STRUCTURE.md`
- **ADRs**: `docs/ADRs/*.md`
- **Runbooks**: `docs/runbooks/*.md`

## Communication Patterns

### Synchronous (REST)
- Client-initiated requests
- Query operations (GET)
- Immediate responses needed

### Asynchronous (Events)
- Domain events (task completed, message sent)
- Cross-service notifications
- Use RabbitMQ + MassTransit

### Event Examples
- `TaskCreated` (Orchestration → ML Classifier)
- `MessageSent` (Chat → Dashboard)
- `BuildFailed` (CI/CD Monitor → Orchestration)
- `PullRequestCreated` (GitHub → Chat)

## Quality Metrics

- **API Latency**: p95 < 500ms
- **Test Coverage**: 85%+
- **Build Time**: < 5 min per service
- **Availability**: 99.5%+ uptime
- **Deployment**: Daily per service

## Service Boundaries

- Each service has its own schema in PostgreSQL
- Services communicate via REST (synchronous) or Events (asynchronous)
- Shared contracts defined in `src/SharedKernel`
- No direct database access across services

## Deployment

### Development
```bash
cd deployment/docker-compose
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up
```

### Production
```bash
cd deployment/docker-compose
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d
```

### Access Points
- Gateway API: http://localhost:5000
- Angular UI: http://localhost:4200
- Grafana: http://localhost:3000 (admin/admin)
- Jaeger: http://localhost:16686

## Current Status

**Phase**: Phase 4 - Frontend & Dashboard (Weeks 19-22)
**Progress**: 67% complete (4 of 6 phases done)
**Target**: Q2 2026 completion

**Completed**: Phases 0-3 (Architecture, Infrastructure, Core Services, Integration Services)
**In Progress**: Phase 4 (Dashboard Service, Angular Dashboard)
**Upcoming**: Phases 5-6 (Migration & Cutover, Stabilization & Docs)

