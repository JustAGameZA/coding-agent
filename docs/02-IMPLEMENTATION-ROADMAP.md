# Implementation Roadmap - Microservices Rewrite

**Project Duration**: 6.5 months (26 weeks)
**Start Date**: October 2025
**Target Completion**: May 2026
**Team Size**: 1 developer + AI assistance (GitHub Copilot)
**Current Phase**: Phase 3 Complete → Phase 4 Starting (Frontend & Dashboard)
**Last Updated**: October 27, 2025

---

## 🎯 Current Sprint Status

**Phase 1 Infrastructure Complete!** All major infrastructure components delivered:
- ✅ Gateway with YARP routing, JWT auth, CORS, Polly resilience, and distributed rate limiting (Redis)
- ✅ PostgreSQL schemas with EF Core migrations (Chat, Orchestration) and startup migration helpers
- ✅ MassTransit + RabbitMQ wired in services with SharedKernel configuration extensions
- ✅ SharedKernel infrastructure extensions (DbContext migrations, RabbitMQ host/health)
- ✅ Testcontainers-based integration tests with Docker fallback (Chat service)
- ✅ Angular 20.3 dashboard scaffold (Material + SignalR dep)
- ✅ Observability stack configured end-to-end (OpenTelemetry → Prometheus/Grafana/Jaeger + Seq)

**Phase 2 Complete!** ✅ (All Core Services Operational)

**Chat Service** ✅
- ✅ API enhancements: pagination (#86), full-text search (#91), Redis caching (#94)
- ✅ 141 unit tests passing with trait categorization
- ✅ SignalR hub implemented and mapped at `/hubs/chat`
- ✅ Integration tests with Testcontainers
- 🔜 Outstanding: SignalR hub auth + presence tracking, file attachments

**Orchestration Service** ✅
- ✅ Task domain model implemented (#89)
- ✅ All execution strategies implemented: SingleShot (#95), Iterative (#88), MultiAgent (#121)
- ✅ 214 unit tests passing
- ✅ Rate limiting configured (10 executions/hour/user)
- ✅ Event publishing infrastructure complete
- 🔜 Outstanding: Task CRUD REST endpoints, SSE logs streaming, strategy selector

**Phase 2 ML Classifier — Complete!** ✅ (PRs #122, #123, #127 merged 2025-10-25; Final updates 2025-10-26)
- ✅ Heuristic classifier implemented with comprehensive tests; fixed no-keyword-match bug to derive strategy/tokens from complexity (100% coverage on module)
- ✅ ML Classifier (XGBoost) implemented with 122-feature extractor and model loader; dummy model shipped for dev/testing
- ✅ Performance: average latency ~1.26ms (well under 50ms target); coverage ~98% across ML components
- ✅ REST API production-ready: rate limiting (100 req/min via slowapi), validation (10-10K chars), enhanced health checks with dependency status (PR #127)
- ✅ Testing: 145 tests passing (109 unit + 36 integration) with comprehensive coverage
- ✅ Documentation added: `ML_CLASSIFIER_IMPLEMENTATION.md`, `models/README.md`; model versioning enabled via file naming
- ✅ Hybrid routing fully operational (heuristic → ML → LLM cascade)
- ✅ Training endpoints added (`/train/feedback`, `/train/retrain`, `/train/stats`)
- ✅ Event listener infrastructure for `TaskCompletedEvent` documented and ready for Phase 3 RabbitMQ integration
- ✅ CI workflow added with pytest and coverage enforcement (>=85%)

**Phase 3 Integration Services — Complete!** ✅ (Epic #157 closed 2025-10-27)

**Test Coverage Summary** (532 unit tests total across all services):
- Chat Service: 141 unit tests
- Orchestration Service: 214 unit tests
- GitHub Service: 57 unit tests
- Ollama Service: 32 unit tests
- SharedKernel: 79 unit tests
- CI/CD Monitor: 5 unit tests (20 tests total including integration)
- Browser Service: 4 unit tests

**CI/CD Monitor** ✅ (PR #168 merged 2025-10-27)
- ✅ BuildFailedEvent → AutomatedFixService → Orchestration task → PR creation on success
- ✅ 7 error pattern matchers (compilation, test failure, lint, dependency, syntax, null ref, timeout)
- ✅ Fix statistics endpoints: `/fix-statistics`, `/fix-statistics/by-error-pattern`
- ✅ Entities: BuildFailure, FixAttempt with FK relationships

**GitHub Service** ✅
- ✅ PR management endpoints: create, merge, close with Octokit integration
- ✅ Webhook handling: `/webhooks/github` with HMAC signature validation
- ✅ Repository and branch management operations
- ✅ 57 unit tests covering core functionality

**Browser Service** ✅
- ✅ Playwright automation with browser pool (max 5 concurrent)
- ✅ Screenshot capture, content extraction, form interaction, PDF generation
- ⚠️ Integration tests require Playwright browser installation in CI

**Ollama Service** ✅
- ✅ Hardware-aware model selection with HardwareDetector
- ✅ ML-driven model routing and A/B testing framework
- ✅ Dynamic model registry syncing every 5 minutes
- ✅ 32 unit tests covering hardware detection and model selection

**Integration Status** ✅
- ✅ All services integrated with Gateway (YARP routing configured)
- ✅ Message bus wiring complete (MassTransit consumers registered)
- ✅ Health checks configured for all services
- ✅ OpenTelemetry tracing and metrics enabled

Next up: Phase 4 — Frontend & Dashboard
- Rebuild Angular dashboard with microservices integration
- Implement Dashboard Service (BFF) for data aggregation
- Add real-time SignalR notifications
- E2E testing with Cypress

---

## Project Phases Overview

| Phase | Duration | Focus | Deliverable |
|-------|----------|-------|-------------|
| **Phase 0** | 2 weeks | Architecture & Planning | Complete specifications, ADRs, POC |
| **Phase 1** | 4 weeks | Infrastructure & Gateway | Gateway + Auth + Observability |
| **Phase 2** | 6 weeks | Core Services | Chat + Orchestration + ML Classifier |
| **Phase 3** | 4 weeks | Integration Services | GitHub + Browser + CI/CD Monitor |
| **Phase 4** | 4 weeks | Frontend & Dashboard | Angular dashboard + E2E integration |
| **Phase 5** | 2 weeks | Migration & Cutover | Data migration, traffic routing |
| **Phase 6** | 2 weeks | Stabilization & Docs | Bug fixes, documentation, handoff |

---

## Phase 0: Architecture & Planning (Weeks 1-2) ✅ COMPLETE

### Goal
Complete architectural specifications and validate technical approach.

**Status**: ✅ **COMPLETE** (All deliverables achieved)

### Week 1: Documentation & Design ✅

**Days 1-2: Current System Analysis**

- ✅ Review existing codebase (CodingAgent.Core, Application, Infrastructure)
- ✅ Document feature inventory (what must be migrated)
- ✅ Identify pain points (tight coupling, deployment issues, scalability)
- ✅ Extract reusable patterns (orchestration logic, ML classification)

- **Deliverable**: ✅ Architecture analysis complete

**Days 3-4: Microservice Boundaries**

- ✅ Define 8 microservices with DDD bounded contexts
- ✅ Map current features to new services
- ✅ Design service APIs (OpenAPI specs)
- ✅ Define data ownership per service

- **Deliverable**: ✅ `01-SERVICE-CATALOG.md` with detailed specifications (78 KB)

**Day 5: SharedKernel Design**

- ✅ Identify common domain primitives (Task, User, Result)
- ✅ Design shared contracts (DTOs, events, interfaces)
- ✅ Define versioning strategy (semantic versioning)

- **Deliverable**: ✅ `CodingAgent.SharedKernel` project structure (79 unit tests)

### Week 2: Service Scaffolding ✅

**Days 1-5: Initial Service Setup**

- ✅ Create solution structure for all services
- ✅ Setup project templates and conventions
- ✅ Implement SharedKernel base types (events, abstractions, infrastructure)
- ✅ Setup initial CI/CD workflows

- **Deliverable**: ✅ All service projects scaffolded and building (16 .csproj files)

---

## Phase 1: Infrastructure & Gateway (Weeks 3-6) ✅ COMPLETE

### Goal
Production-ready infrastructure: Gateway, Auth, Databases, Message Bus, Observability.

**Status**: ✅ **COMPLETE** (All infrastructure operational and production-ready)

### Week 3: Infrastructure Setup

**Days 1-2: Docker Compose Stack**
- ✅ Create `docker-compose.yml` stack (PostgreSQL, Redis, RabbitMQ, Prometheus, Grafana, Jaeger, Seq)
- ✅ PostgreSQL service configured with init script and healthcheck
- ✅ Redis service with AOF and healthcheck
- ✅ RabbitMQ with management UI and Prometheus plugin
- ✅ Prometheus + Alertmanager + Grafana provisioning and dashboards
- ✅ Jaeger all-in-one with OTLP enabled
- ✅ Seq for structured logs
- **Deliverable**: `docker compose up` starts full observability + infra stack

**Days 3-4: Database Migrations**
- ✅ Setup EF Core migrations per service (Chat, Orchestration)
- ✅ Create `chat` schema (conversations, messages tables)
- ✅ Create `orchestration` schema (tasks, executions tables)
- ⏳ Seed test data via fixtures (optional)
- ⏳ Cross-service queries (not required in microservices; N/A)
- ✅ Extract migration patterns to SharedKernel (DbContextExtensions)
- **Deliverable**: Database migration scripts in services and applied on startup



**Day 5: CI/CD Pipeline**
- ✅ GitHub Actions workflow per service
- ✅ Build, test, docker build, push to registry
- ✅ Separate pipelines allow parallel deployment
- **Deliverable**: Per-service workflows under `.github/workflows/`



**Message Bus Wiring (Completed)**
- ✅ MassTransit configured across services (Chat, Orchestration)
- ✅ RabbitMQ connection via configuration (host/username/password)
- ✅ SharedKernel extensions for consistent RabbitMQ config and health checks
- ✅ Basic consumer stubs wired; endpoints configured
- **Deliverable**: Services start with bus wired; event logs visible when broker is running

### Week 4: Gateway Implementation

**Days 1-2: YARP Reverse Proxy**
- ✅ Install and configure `Yarp.ReverseProxy` in Gateway project
- ✅ Routes defined in `appsettings.json` for multiple services with active health checks
- ✅ Tested routing via configuration and health endpoints
- **Deliverable**: Gateway routes requests to backend services

**Days 3-4: Authentication & Authorization**
- ✅ JWT token validation middleware
- ✅ User claims extraction (userId, roles)
- ✅ CORS policy configuration
- ✅ Per-route authorization (proxy requires auth)
- **Deliverable**: Protected endpoints require valid JWT

**Day 5: Rate Limiting & Circuit Breaker**
- ✅ Redis-backed distributed rate limiter (per-IP + per-user)
- ✅ Polly: retries with exponential backoff + circuit breaker
- ✅ Observability via Serilog + OpenTelemetry
- **Deliverable**: Gateway resists overload and cascading failures



### Week 5-6: Observability

**Days 1-3: OpenTelemetry Integration**

- ✅ Add OTLP exporters to services (Gateway, Chat, Orchestration)
- ✅ Implement correlation ID propagation
- ✅ Configure Jaeger (OTLP collector + UI)
- ✅ Prometheus metrics endpoints exposed

- **Deliverable**: End-to-end traces visible in Jaeger UI


**Days 4-5: Metrics & Dashboards**

- ✅ Instrument metrics and expose Prometheus endpoints
- ✅ Configure Prometheus scrape targets for services and exporters
- ✅ Grafana dashboards provisioned (system, API, services, database, cache, alerts)
- ✅ Alerting rules configured (API/infrastructure/message bus)

- **Deliverable**: Real-time metrics visible in Grafana




---

## Phase 2: Core Services (Weeks 7-12) ✅ COMPLETE

### Goal
Implement the three most critical services: Chat, Orchestration, ML Classifier.

**Status**: ✅ **COMPLETE** (All three core services operational with 532 total unit tests)
- Chat Service: 141 tests, full API + SignalR hub
- Orchestration Service: 214 tests, all strategies implemented
- ML Classifier Service: 145 tests (Python), production-ready API

Prerequisite: Phase 1 (Infrastructure & Gateway) deliverables complete. ✅

### Week 7-8: Chat Service

**Phase 2 Batch 2 Complete!** ✅ (Issue #105 closed 2025-10-25)
- ✅ PR #113: Full-text search with PostgreSQL GIN indexes
- ✅ PR #114: Redis message caching with cache-aside pattern
- ✅ PR #115: Pagination with HATEOAS Link headers
- ✅ All 113 tests passing (82 unit, 31 integration) with trait tags
- ✅ Test filtering enabled: `dotnet test --filter "Category=Unit"` (382ms)

**Remaining Work:**
- SignalR hub auth + presence tracking
- File attachments (multipart upload + storage)

**Days 1-2: Domain Model & Repository**
- ✅ Implement entities (Conversation, Message)
- [ ] Create repository pattern with EF Core (endpoints currently use DbContext)
- [ ] Add comprehensive validation (FluentValidation)
- [ ] Write unit tests (85%+ coverage) — integration tests exist; add more unit tests
- **Deliverable**: Domain layer largely in place; refine validation/tests

**Days 3-5: REST API**
- ✅ Implement core endpoints (list/get/create/delete conversations)
- ✅ Add pagination (page size: 50) — **PR #115 merged 2025-10-25**
- ✅ Implement search (full-text via PostgreSQL) — **PR #113 merged 2025-10-25**
- ✅ Integration tests (Testcontainers) with in-memory fallback when Docker unavailable
- **Deliverable**: ✅ **COMPLETE** — REST API with pagination + search, 113 tests passing

**Days 6-8: SignalR WebSocket**
- ✅ Implement `/hubs/chat` SignalR hub
- [ ] Add connection authentication (JWT in query string)
- ✅ Implement typing indicators
- [ ] Add presence tracking (online/offline)
- [ ] Write SignalR integration tests
- **Deliverable**: Real-time chat partially complete; add auth/presence/tests

**Days 9-10: File Upload & Cache**
- [ ] Implement multipart file upload
- [ ] Store files in Azure Blob / S3
- ✅ Cache last 100 messages in Redis — **PR #114 merged 2025-10-25**
- ✅ Add cache invalidation on new messages
- **Deliverable**: Cache complete, target hit rate > 80%; file attachments pending

### Week 9-10: Orchestration Service

**Days 1-3: Task Domain Model**
- ✅ Implement entities (CodingTask, TaskExecution, ExecutionResult) — #89
- ✅ Create repository pattern — #89
- ✅ Add state machine for TaskStatus transitions — #89
- ✅ Write unit tests for state transitions — #89
- **Deliverable**: ✅ Completed — Task domain logic implemented and tested (Issue #89 closed)

**Days 4-6: Execution Strategies**
- ✅ Implement `SingleShotStrategy` (simple tasks) — #95 (PR #117 merged)
- ✅ Implement `IterativeStrategy` (medium tasks) — #88
- ✅ Implement `MultiAgentStrategy` (complex tasks) — **PR #121 merged 2025-10-25**
- [ ] Add strategy selector (based on complexity)
- **Deliverable**: ▶ Strategies implemented (SingleShot, Iterative, MultiAgent); selector pending

**Days 7-9: REST API & Integration**
- [ ] Implement task CRUD endpoints
- [ ] Add SSE endpoint for streaming logs (`GET /tasks/{id}/logs`)
- [ ] Integrate with ML Classifier (REST call)
- [ ] Integrate with GitHub Service (create PR)
- **Deliverable**: Full task lifecycle working

**Day 10: Event Publishing**
- ✅ Publish `TaskCreatedEvent`, `TaskCompletedEvent`, `TaskFailedEvent` — #83
- ✅ Configure MassTransit message bus — base wiring in place across services
- ✅ Add retry logic (3 retries with exponential backoff) — #83
- **Deliverable**: ✅ Baseline event publishing implemented (Issue #83 closed); end-to-end validation will be finalized in API & Integration

### Week 11-12: ML Classifier Service

**Days 1-2: Python Project Setup**
- [x] Create FastAPI project structure
- [x] Setup virtual environment (venv) and `requirements.txt`
- [ ] Configure PostgreSQL connection (asyncpg) — not required yet (no persistence in Phase 2)
- [x] Add pytest test framework
- **Deliverable**: `ml_classifier_service/` Python project scaffolded with tests

**Days 3-5: Classification Logic**
- [x] Implement heuristic classifier (keyword matching) — bugfix for no-match strategy/tokens included (PR #123)
- [x] Implement ML classifier (XGBoost model) with feature extractor and model loader (PR #122)
- [ ] Add hybrid approach (heuristic → ML → LLM fallback) — wiring pending in API
- [x] Write unit tests (98–100% coverage across modules)
- **Deliverable**: Classification logic implemented (heuristic + ML); hybrid routing planned next

**Days 6-7: Model Training**
- [ ] Create training data loader (from PostgreSQL)
- [x] Implement feature extraction (TF-IDF, code metrics)
- ⏳ Train XGBoost model (scikit-learn pipeline) — dummy model shipped for dev/testing
- [ ] Export model to ONNX format
- **Deliverable**: Baseline model artifacts available (dummy); training pipeline to be completed in Phase 3

**Days 8-10: REST API & Integration**
- [x] Implement `/classify` endpoint (currently heuristic-first)
- [x] Add rate limiting (100 req/min per IP via slowapi) — **PR #127 merged 2025-10-25**
- [x] Add input validation (10-10K char task descriptions) — **PR #127 merged 2025-10-25**
- [x] Enhance health checks with classifier dependency status — **PR #127 merged 2025-10-25**
- [x] Write integration tests for validation, rate limiting, health — **PR #127 merged 2025-10-25**
- [x] Add `/train` endpoint (trigger retraining) — **Completed 2025-10-26**
- [x] Implement event listener for `TaskCompletedEvent` (training data collection) — **Completed 2025-10-26**
- [x] Add model versioning (save models with versioned filenames)
- [x] Add GitHub Actions workflow for Python tests with coverage enforcement — **Completed 2025-10-26**
- **Deliverable**: ✅ ML service REST API production-ready with training infrastructure (145 tests passing)

---

## Phase 3: Integration Services (Weeks 13-18) ✅ COMPLETE

### Goal
Build GitHub, Browser, CI/CD Monitor, and Ollama services.

**Status**: ✅ **COMPLETE** (Epic #157 closed October 27, 2025)
- All four integration services implemented and tested
- CI/CD Monitor automated fix generation operational (PR #168)
- Services integrated with Gateway and message bus
- Unit tests passing across all services

### Week 13-14: Ollama Service ✅

**Days 1-3: Foundation & Hardware Detection**
- ✅ Create `CodingAgent.Services.Ollama` project structure
- ✅ Implement domain models (OllamaModel, OllamaRequest, OllamaResponse, HardwareProfile, ABTest)
- ✅ Deploy Ollama Backend in Docker Compose (ollama/ollama:latest) with GPU support
- ✅ **Implement HardwareDetector: detect GPU type, VRAM, CPU cores**
- ✅ **Auto-detect hardware on startup, determine appropriate initial models**
- ✅ Implement OllamaHttpClient (wrapper around Ollama REST API)
- **Deliverable**: ✅ Hardware detected, Ollama Backend running, hardware-aware model selection operational

**Days 4-6: Dynamic Model Management**
- ✅ **Implement ModelRegistry as IHostedService (syncs models every 5 minutes)**
- ✅ **Query Ollama backend dynamically for all available models (no hardcoded lists)**
- ✅ **Download hardware-appropriate initial models (13B for 16GB VRAM, 7B for 8GB, etc.)**
- ✅ Implement ModelManager (download, list, delete models via API)
- ✅ Add REST API endpoints (/models, /models/pull, /models/delete)
- ✅ Write unit tests for ModelRegistry and HardwareDetector
- **Deliverable**: ✅ Models dynamically discovered, hardware-aware initialization complete

**Days 7-9: ML-Driven Model Selection & A/B Testing**
- ✅ **Implement MlModelSelector (ML-driven, replaces hardcoded InferenceRouter)**
- ✅ **Extract task features: task_type, complexity, language, context_size**
- ✅ **Integrate with ML Classifier service for model prediction**
- ✅ **Implement ABTestingEngine (create tests, route traffic, record results)**
- ✅ **Add API endpoints: POST /ab-tests, GET /ab-tests/{id}/results**
- ✅ Add REST API endpoint: POST /inference (with ML selection + A/B testing)
- ✅ Implement PromptOptimizer (Redis caching for deterministic prompts)
- ✅ Add UsageTracker with accuracy metrics (success, latency, quality score)
- ✅ Configure OpenTelemetry tracing
- ✅ Add OllamaHealthCheck (validate Ollama Backend availability)
- **Deliverable**: ✅ ML-driven model selection operational, A/B testing framework ready, no hardcoded models

**Day 10: Integration Tests & Cloud API Fallback**
- ✅ Implement ICloudApiClient interface with IsConfigured() and HasTokensAvailableAsync()
- ✅ Add token usage tracking and monthly limit enforcement
- ✅ Add configuration validation on startup
- ✅ Write integration tests with Testcontainers (Ollama Backend)
- ✅ Test streaming generation
- ✅ Test cache hit/miss scenarios
- ✅ Test ML model selection with different task features
- ✅ Test A/B test variant selection and result recording
- ✅ Test circuit breaker fallback (only when cloud API configured with tokens)
- **Deliverable**: ✅ 85%+ test coverage, A/B testing verified, safe fallback mechanism

### Week 15-16: GitHub Service ✅

**Days 1-3: Octokit Integration**
- ✅ Implement repository connection (OAuth flow)
- ✅ Add repository CRUD operations
- ✅ Implement branch management
- ✅ Write unit tests with mocked Octokit
- **Deliverable**: ✅ GitHub repository operations working

**Days 4-6: Pull Request Management**
- ✅ Implement PR creation endpoint
- ✅ Add PR merge/close operations
- ✅ Create PR templates (Markdown)
- ✅ Add automated code review comments
- **Deliverable**: ✅ PR lifecycle complete

**Days 7-10: Webhook Handling**
- ✅ Implement `/webhooks/github` endpoint
- ✅ Validate webhook signatures (HMAC)
- ✅ Handle push, PR, issue events
- ✅ Publish domain events to RabbitMQ
- **Deliverable**: ✅ Webhooks triggering downstream actions

### Week 17: Browser Service ✅

**Days 1-2: Playwright Setup**
- ✅ Install Playwright browsers (Chromium, Firefox)
- ✅ Implement browser pool (max 5 concurrent)
- ✅ Add navigation endpoint (`POST /browse`)
- **Deliverable**: ✅ Basic browsing working

**Days 3-5: Advanced Features**
- ✅ Implement screenshot capture (full page + element)
- ✅ Add content extraction (text, links, images)
- ✅ Implement form interaction (fill, submit)
- ✅ Add PDF generation
- **Deliverable**: ✅ All browser features operational

**Note**: Integration tests require Playwright browsers to be installed on CI runners. Ensure CI includes browser installation step before running integration tests.

### Week 18: CI/CD Monitor Service ✅

**Days 1-3: GitHub Actions Integration**
- ✅ Poll GitHub Actions API for build status
- ✅ Detect build failures
- ✅ Parse build logs for error messages
- **Deliverable**: ✅ Build monitoring working

**Days 4-5: Automated Fix Generation** (PR #168 merged October 27, 2025)
- ✅ Integrate with Orchestration service
- ✅ Generate fix task from build error
- ✅ Create PR with fix via GitHub service
- ✅ Track fix success rate (statistics endpoints)
- ✅ Implement 7 error pattern matchers (compilation, test, lint, dependency, syntax, null ref, timeout)
- ✅ Add BuildFailure and FixAttempt entities with EF Core migrations
- ✅ Publish FixAttemptedEvent and FixSucceededEvent
- ✅ Add fix statistics endpoints: `/fix-statistics`, `/fix-statistics/by-error-pattern`
- ✅ Write 20 tests (17 unit + 3 integration)
- **Deliverable**: ✅ End-to-end automated fix flow operational

---

## Phase 4: Frontend & Dashboard (Weeks 19-22)

### Goal
Rebuild Angular dashboard with microservices integration.

### Week 19-20: Dashboard Service (BFF)

**Current Status**: ⚠️ Minimal scaffold only (health endpoint + observability)
- ✅ Project structure created
- ✅ OpenTelemetry configured (tracing + metrics)
- ✅ Health checks endpoint `/health`
- ✅ Prometheus metrics endpoint
- ❌ No business logic implemented yet

**Days 1-3: Data Aggregation**
- [ ] Implement `/dashboard/stats` (aggregate from all services)
- [ ] Add `/dashboard/tasks` (enrich with GitHub data)
- [ ] Create `/dashboard/activity` (recent events)
- **Deliverable**: Dashboard API returning aggregated data

**Days 4-5: Caching Strategy**
- [ ] Add Redis caching (5 min TTL)
- [ ] Implement cache invalidation on events
- [ ] Add cache warming on startup
- **Deliverable**: Dashboard API response time < 100ms

### Week 21-22: Angular Dashboard

**Current Status**: ⚠️ Basic scaffold with routing and Material UI setup
- ✅ Angular 20.3 project created with standalone components
- ✅ Material UI integrated (toolbar, sidenav, cards, buttons)
- ✅ Routing configured with feature modules structure
- ✅ SignalR service stub created (`core/services/signalr.service.ts`)
- ✅ API service stub created (`core/services/api.service.ts`)
- ✅ Basic components scaffolded: `ChatComponent`, `TasksComponent`
- ❌ No actual integration with backend services yet
- ❌ Components display placeholder content only

**Days 1-5: Component Rewrite**
- [ ] Rebuild task list component (calls Dashboard Service)
- [ ] Rebuild chat component (SignalR integration)
- [ ] Add real-time notifications (via SignalR)
- [ ] Create system health dashboard (metrics from Gateway)
- **Deliverable**: Functional Angular dashboard

**Days 6-10: E2E Testing**
- [ ] Write Cypress E2E tests (full user flows)
- [ ] Test chat conversation flow
- [ ] Test task creation → execution → PR flow
- [ ] Test error handling (network failures, 500 errors)
- **Deliverable**: E2E test suite passing

---

## Phase 5: Migration & Cutover (Weeks 23-24)

### Goal
Migrate data from old system and route production traffic to new system.

### Week 23: Data Migration

**Days 1-2: Migration Scripts**
- [ ] Write PostgreSQL migration (old DB → new schemas)
- [ ] Migrate users (1:1 mapping)
- [ ] Migrate conversations (Chat service schema)
- [ ] Migrate tasks (Orchestration service schema)
- **Deliverable**: Migration scripts tested on staging

**Days 3-5: Dual-Write Period**
- [ ] Enable dual-writes (write to both old and new DBs)
- [ ] Verify data consistency
- [ ] Monitor for write errors
- **Deliverable**: Data consistency validated

### Week 24: Traffic Cutover

**Days 1-2: Feature Flags**
- [ ] Add feature flags in Gateway (`UseLegacyChat`, `UseLegacyOrchestration`)
- [ ] Route 10% of traffic to new services
- [ ] Monitor error rates and latency
- **Deliverable**: Partial traffic routing working

**Days 3-4: Full Cutover**
- [ ] Route 100% traffic to new services
- [ ] Disable writes to old DB
- [ ] Monitor for 24 hours
- **Deliverable**: Old system decommissioned

**Day 5: Cleanup**
- [ ] Remove old monolith code
- [ ] Archive old repositories
- [ ] Update documentation
- **Deliverable**: Clean codebase

---

## Phase 6: Stabilization & Documentation (Weeks 25-26)

### Goal
Fix bugs, optimize performance, complete documentation.

### Week 25: Bug Fixes & Optimization

**Days 1-3: Bug Triage**
- [ ] Review production errors (last 7 days)
- [ ] Fix P0 bugs (crashes, data loss)
- [ ] Fix P1 bugs (functional issues)
- **Deliverable**: Zero P0/P1 bugs

**Days 4-5: Performance Optimization**
- [ ] Identify slow endpoints (p95 > 500ms)
- [ ] Add database indexes
- [ ] Optimize N+1 queries
- [ ] Tune cache TTLs
- **Deliverable**: All endpoints < 500ms p95

### Week 26: Documentation & Handoff

**Days 1-2: Architecture Documentation**
- [ ] Finalize all ADRs (Architecture Decision Records)
- [ ] Complete OpenAPI specs (Swagger UI)
- [ ] Write deployment guide (Docker + K8s)
- **Deliverable**: Complete documentation set

**Days 3-4: Runbooks**
- [ ] Write incident response runbooks
- [ ] Document common issues and resolutions
- [ ] Create operational dashboard (Grafana)
- **Deliverable**: Operations manual

**Day 5: Handoff & Retrospective**
- [ ] Conduct project retrospective
- [ ] Document lessons learned
- [ ] Plan next enhancements (Phase 7+)
- **Deliverable**: Project closure report

---

## Risk Management

### High Risks

| Risk | Probability | Impact | Mitigation | Status (Oct 25, 2025) |
|------|------------|--------|------------|----------------------|
| **Underestimated Complexity** | Medium | High | Add 20% buffer to each phase | ✅ **Mitigated** - Phase 1 completed ahead of schedule |
| **Integration Issues** | High | Medium | POC in Phase 0 validates approach | ✅ **Resolved** - All services wired with MassTransit, tests passing |
| **Data Migration Errors** | Medium | Critical | Dual-write period + rollback plan | ⏳ **Pending** - Phase 5 concern, migrations validated in Phase 1 |
| **Performance Degradation** | Low | High | Load testing in Phase 4 | ⏳ **Pending** - Observability foundation ready |
| **Scope Creep** | High | Medium | Strict scope definition, Phase 7 for extras | ✅ **Under Control** - Focused on core services |

### Mitigation Strategies

1. **Weekly Progress Reviews**: Adjust timeline if falling behind
   - ✅ **Status**: Phase 1 delivered 2 weeks ahead of schedule (4 weeks vs. planned 6 weeks)
2. **Automated Testing**: 85%+ coverage prevents regressions
   - ✅ **Status**: 46/46 tests passing, Testcontainers configured, 100% test pass rate maintained
3. **Feature Flags**: Enable gradual rollout and rollback
   - ⏳ **Status**: Planned for Phase 4 deployment
4. **Rollback Plan**: Keep old system operational until cutover validated
   - ⏳ **Status**: Planned for Phase 5 migration

**Lessons Learned (Phase 1):**
- SharedKernel infrastructure extensions prevent code duplication across services (eliminated 112 lines of duplicate code in Week 4)
- Testcontainers with Docker fallback ensures tests pass in all environments (CI/CD + local dev)
- AI-assisted development (GitHub Copilot) accelerates delivery without sacrificing code quality

---

## Success Criteria

### Technical Metrics

- ✅ **Zero-downtime deployment**: Rolling updates without service interruption
- ✅ **API latency**: p95 < 500ms for all endpoints
- ✅ **Test coverage**: 85%+ for all services
- ✅ **Build time**: < 5 minutes per service
- ✅ **Availability**: 99.5%+ uptime

### Business Metrics

- ✅ **Feature velocity**: 2x faster (parallel development)
- ✅ **Deployment frequency**: Daily deployments per service
- ✅ **MTTR**: < 5 minutes (auto-recovery)
- ✅ **Cost reduction**: 30% (independent scaling)

---

## Resource Requirements

### Development Tools

- **IDE**: Visual Studio Code + GitHub Copilot
- **Database**: PostgreSQL 16, Redis 7
- **Message Queue**: RabbitMQ 3.12
- **Monitoring**: Prometheus, Grafana, Jaeger, Seq
- **Testing**: xUnit, pytest, Cypress, k6 (load testing)

### Infrastructure

- **Development**: Local Docker Compose
- **Staging**: Cloud VMs (2 vCPU, 8GB RAM) × 3
- **Production**: Kubernetes cluster (autoscaling)

### Estimated Costs

- **Development**: $0 (local Docker)
- **Staging**: ~$150/month (cloud VMs)
- **Production**: ~$500/month (K8s + managed services)

---

## Next Steps

**Phase 4: Frontend & Dashboard (Starting Week 19)**

1. **Dashboard Service (BFF)**: Implement data aggregation endpoints (`/dashboard/stats`, `/dashboard/tasks`, `/dashboard/activity`) with Redis caching
2. **Angular Dashboard Rebuild**: Rebuild task list and chat components with microservices integration; add real-time SignalR notifications
3. **E2E Testing**: Write Cypress E2E tests for full user flows (chat conversation, task creation → execution → PR, error handling)
4. **Performance Optimization**: Ensure all endpoints < 500ms p95 latency; add database indexes and cache tuning

**Outstanding Phase 2/3 Items (Low Priority)**
- Chat: SignalR hub authentication + presence tracking, file attachments (multipart + storage)
- Orchestration: Strategy selector (complexity-based routing), task CRUD + SSE logs endpoints
- Browser: Add Playwright browser installation step to CI workflow for integration tests

---

**Document Owner**: Technical Lead
**Last Updated**: October 27, 2025
**Next Review**: November 3, 2025
