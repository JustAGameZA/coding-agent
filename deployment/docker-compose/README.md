# 🚀 Docker Compose Deployment Guide

Production-ready Docker Compose configuration for the Coding Agent microservices platform.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Services](#services)
- [Configuration](#configuration)
- [Health Checks](#health-checks)
- [Monitoring & Observability](#monitoring--observability)
- [Observability Stack](#observability-stack)
- [Troubleshooting](#troubleshooting)
- [Production Considerations](#production-considerations)
- [Documentation](#documentation)

# Docker Compose Deployment

Complete Docker Compose setup for the Coding Agent microservices platform.

## 📦 What's Included

### Infrastructure Services (`docker-compose.yml`)
- **PostgreSQL 16**: Multi-schema database (chat, orchestration, cicd_monitor)
- **Redis 7**: Cache and session storage
- **RabbitMQ 3.12**: Message queue with management UI
- **Prometheus**: Metrics collection and alerting
- **Alertmanager**: Alert routing and notifications
- **Grafana**: Metrics visualization dashboards
- **Jaeger**: Distributed tracing (OpenTelemetry)
- **Seq**: Structured logging and log search
- **Ollama**: Local LLM inference engine
- **Exporters**: PostgreSQL, Redis, Node, cAdvisor metrics

### Application Services

#### Development Mode (`docker-compose.apps.dev.yml`)
Hot-reload enabled services with volume mounts:
- Gateway (YARP) - Port 5000
- Chat Service - Port 5001
- Orchestration Service - Port 5002
- Ollama Service - Port 5003
- GitHub Service - Port 5004
- Browser Service - Port 5005
- CI/CD Monitor - Port 5006
- Dashboard BFF - Port 5007
- ML Classifier (Python) - Port 8000
- Angular Dashboard - Port 4200

#### Production Mode (`docker-compose.apps.prod.yml`)
Optimized builds from Dockerfiles:
- All services built as multi-stage Docker images
- Nginx-served Angular production build
- Health checks and resource limits
- Automatic restarts

## 🚀 Quick Start

### Prerequisites
- Docker 24+ with BuildKit enabled
- Docker Compose v2.20+
- 8GB RAM minimum (16GB recommended)
- 20GB free disk space

### 1. Infrastructure Only (Start Services First)

```bash
# From repo root
cd deployment/docker-compose

# Start infrastructure
docker compose up -d

# Verify all healthy
docker compose ps
```

**Access Points:**
- Grafana: http://localhost:3000 (admin/admin)
- Prometheus: http://localhost:9090
- Jaeger UI: http://localhost:16686
- RabbitMQ: http://localhost:15672 (codingagent/devPassword123!)
- Seq: http://localhost:5341
- Ollama: http://localhost:11434

**Observability:**
- See [OBSERVABILITY-SUMMARY.md](./OBSERVABILITY-SUMMARY.md) for complete stack details
- 13 Prometheus scrape jobs, 53 alert rules, 8 Grafana dashboards

### 2. Development Mode (Hot Reload)

```bash
# Start infrastructure + dev apps
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up

# Or detached
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up -d

# Watch logs
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml logs -f gateway chat-service
```

**Key Features:**
- Code changes trigger auto-reload (dotnet watch, uvicorn --reload, ng serve)
- NuGet/pip/npm caches persist across restarts
- Source code mounted for hot reload

#### Playwright Browsers for CI/E2E

If you run E2E or Browser service integration tests in CI, ensure Playwright browsers are installed before tests:

```bash
npx playwright install --with-deps chromium
# optionally
npx playwright install firefox
```

In containerized CI, add the install step before running the test job.

### 3. Production Mode (Optimized Builds)

```bash
# Build and start all services
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d

# Check health
./health-check.sh

# View logs
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml logs -f
```

**Key Features:**
- Multi-stage Docker builds (SDK → Runtime)
- Health checks with automatic restarts
- Resource limits (CPU/memory)
- No development dependencies
- Nginx-served Angular build

**Observability Stack:**
- **13 Prometheus Targets**: All services + infrastructure
- **53 Alert Rules**: 6 categories (API, Infrastructure, ML/AI, Database, Message Bus, Application)
- **8 Grafana Dashboards**: API Gateway, System Health, Database, Cache, Backend Services, Alerts/SLOs, ML Classifier, Ollama
- **Jaeger Tracing**: OTLP distributed tracing
- **Seq Logging**: 14-day structured log retention

## 📊 Observability Stack

### Complete Monitoring Coverage

**Metrics (Prometheus)**:
- 13 scrape jobs (10 apps + 3 infrastructure components)
- 15-second scrape interval
- 30-day retention

**Alerts (Alertmanager)**:
- 53 alert rules across 6 categories
- Severity-based routing (critical/warning/info)
- Webhook receivers for Slack/PagerDuty/Email

**Dashboards (Grafana)**:
- 8 pre-configured dashboards
- Auto-refresh every 10 seconds
- Links to Seq for log correlation

**Tracing (Jaeger)**:
- OTLP endpoints (gRPC 4317, HTTP 4318)
- All .NET services instrumented
- W3C Trace Context propagation

**Logging (Seq)**:
- Structured logging from all services
- 14-day retention
- Correlation ID tracking

### Quick Access

```bash
# Prometheus UI
open http://localhost:9090

# Grafana Dashboards
open http://localhost:3000

# Jaeger Traces
open http://localhost:16686

# Seq Logs
open http://localhost:5341

# Alertmanager
open http://localhost:9093
```

### Verification Scripts

```bash
# Check all service health
./health-check.sh

# Validate alert rules
./validate-alerts.sh

# Verify Jaeger integration
./verify-jaeger.sh
```

### Documentation

- **[OBSERVABILITY-SUMMARY.md](./OBSERVABILITY-SUMMARY.md)**: Complete observability stack details
- **[SEQ-LOGGING-GUIDE.md](./SEQ-LOGGING-GUIDE.md)**: Structured logging queries and workflows
- **[ALERTING-SUMMARY.md](./ALERTING-SUMMARY.md)**: Alert rules and runbooks

### Key Metrics by Service

**Gateway (YARP)**:
- `http_requests_total`, `http_request_duration_seconds`
- `circuit_breaker_state`, `rate_limiter_throttled_total`

**Chat Service**:
- `signalr_active_connections`, `chat_message_delivery_failed_total`

**Orchestration**:
- `task_execution_total`, `task_queue_pending_count`
- `task_execution_duration_seconds` (by strategy)

**ML Classifier**:
- `ml_classification_requests_total{classifier}` (heuristic/ml/llm)
- `ml_model_accuracy`, `ml_confidence_score`

**Ollama**:
- `ollama_inference_requests_total{model}`
- `ollama_tokens_generated_total`, `ollama_cost_total`

**GitHub**:
- `github_api_remaining_requests`, `github_pr_creation_duration_seconds`

**Browser**:
- `browser_automation_total`, `browser_timeout_total`

### Alert Categories

1. **API Alerts** (5 rules): Error rate, latency, request rate, circuit breaker, rate limiter
2. **Infrastructure Alerts** (8 rules): CPU, memory, restarts, disk, service health
3. **Message Bus Alerts** (8 rules): Queue depth, consumers, utilization, memory
4. **ML/AI Alerts** (8 rules): Classifier health, latency, accuracy, errors
5. **Database Alerts** (10 rules): Connections, queries, deadlocks, replication
6. **Application Alerts** (14 rules): Task execution, SignalR, GitHub, browser

## 📚 Documentation

### Primary Documents
- **[DOCKER-QUICK-START.md](./DOCKER-QUICK-START.md)**: Fast command reference
- **[DOCKER-IMPLEMENTATION-SUMMARY.md](./DOCKER-IMPLEMENTATION-SUMMARY.md)**: Technical details
- **[OBSERVABILITY-SUMMARY.md](./OBSERVABILITY-SUMMARY.md)**: Complete observability stack
- **[SEQ-LOGGING-GUIDE.md](./SEQ-LOGGING-GUIDE.md)**: Structured logging guide
- **[ALERTING-SUMMARY.md](./ALERTING-SUMMARY.md)**: Alert rules and runbooks

### Configuration Files
- `.env.template`: Environment variable template
- `docker-compose.override.yml.template`: Local customization template
- `prometheus.yml`: Prometheus scrape configuration (13 jobs)
- `alertmanager.yml`: Alert routing and notification
- `alerts/*.yml`: Alert rule definitions (53 rules)
- `grafana/provisioning/dashboards/*.json`: Dashboard definitions (8 dashboards)

### Operational Runbooks
Located in `docs/runbooks/`:
- `api-error-rate-high.md`
- `api-latency-high.md`
- `container-cpu-high.md`
- `container-memory-high.md`
- `rabbitmq-queue-depth-high.md`
- Source mounted as volumes (../../src/)

**Application URLs:**
- Gateway: http://localhost:5000
- Chat API: http://localhost:5001
- Orchestration API: http://localhost:5002
- Ollama Service: http://localhost:5003
- GitHub API: http://localhost:5004
- Browser API: http://localhost:5005
- CI/CD Monitor: http://localhost:5006
- Dashboard BFF: http://localhost:5007
- ML Classifier: http://localhost:8000
- Angular UI: http://localhost:4200

### 3. Production Mode (Optimized Builds)

```bash
# Build and start all services
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d

# Check status
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml ps

# View logs
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml logs -f
```

**Production Features:**
- Multi-stage Dockerfile builds (SDK → Runtime)
- Optimized Angular production build via Nginx
- Health checks on all services
- Structured logging to Seq
- OpenTelemetry traces to Jaeger
- Prometheus metrics scraping

## 🔧 Configuration

### Environment Variables

Create `.env` file in `deployment/docker-compose/`:

```bash
# Database
POSTGRES_USER=codingagent
POSTGRES_PASSWORD=your-secure-password
POSTGRES_DB=codingagent

# Redis
REDIS_PASSWORD=your-redis-password

# RabbitMQ
RABBITMQ_USER=codingagent
RABBITMQ_PASSWORD=your-rabbitmq-password
RABBITMQ_VHOST=/

# Grafana
GRAFANA_USER=admin
GRAFANA_PASSWORD=your-grafana-password

# GitHub Integration (optional)
GITHUB_APP_ID=your-app-id
GITHUB_INSTALLATION_ID=your-installation-id
GITHUB_PRIVATE_KEY=your-private-key-base64
```

### Port Mappings

| Service | Dev Port | Prod Port | Health Port |
|---------|----------|-----------|-------------|
| Gateway | 5000 | 5000 | 5500 |
| Chat | 5001 | 5001 | 5501 |
| Orchestration | 5002 | 5002 | 5502 |
| Ollama Service | 5003 | 5003 | 5503 |
| GitHub | 5004 | 5004 | 5504 |
| Browser | 5005 | 5005 | 5505 |
| CI/CD Monitor | 5006 | 5006 | 5506 |
| Dashboard BFF | 5007 | 5007 | 5507 |
| ML Classifier | 8000 | 8000 | - |
| Angular UI | 4200 | 4200 | - |

## 📊 Observability

### Metrics (Prometheus + Grafana)

1. Open Grafana: http://localhost:3000
2. Default credentials: `admin/admin`
3. Pre-configured dashboards:
   - ASP.NET Core Metrics
   - PostgreSQL Performance
   - RabbitMQ Queues
   - Container Resource Usage

**Custom Metrics:**
```bash
# Query Prometheus directly
curl http://localhost:9090/api/v1/query?query=up

# Service-specific metrics
curl http://localhost:5001/metrics  # Chat service
curl http://localhost:5002/metrics  # Orchestration
```

### Tracing (Jaeger)

1. Open Jaeger: http://localhost:16686
2. Select service (e.g., `coding-agent-chat`)
3. View distributed traces across services

**OpenTelemetry endpoints:**
- gRPC: `http://jaeger:4317`
- HTTP: `http://jaeger:4318`

### Logging (Seq)

1. Open Seq: http://localhost:5341
2. Query structured logs:
```
@Level = 'Error'
@MessageTemplate LIKE '%Task%'
ServiceName = 'CodingAgent.Services.Orchestration'
```

## 🧪 Health Checks

### Check All Services

```bash
# Infrastructure health
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # Chat
curl http://localhost:5002/health  # Orchestration
curl http://localhost:5006/health  # CI/CD Monitor

# Infrastructure services
curl http://localhost:9090/-/healthy  # Prometheus
curl http://localhost:14269/  # Jaeger
```

### Automated Health Check Script

```bash
# From deployment/docker-compose/
./health-check.sh

# Expected output:
# ✓ Gateway: healthy
# ✓ Chat Service: healthy
# ✓ Orchestration: healthy
# ✓ PostgreSQL: healthy
# ✓ Redis: healthy
# ✓ RabbitMQ: healthy
```

## 🐛 Troubleshooting

### Service Won't Start

```bash
# Check logs
docker compose logs <service-name>

# Example
docker compose logs postgres
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml logs chat-service

# Check resource usage
docker stats

# Restart specific service
docker compose restart <service-name>
```

### Database Connection Issues

```bash
# Verify PostgreSQL is running
docker compose exec postgres pg_isready -U codingagent

# Check database schemas
docker compose exec postgres psql -U codingagent -d codingagent -c '\dn'

# Reinitialize database
docker compose down -v  # WARNING: Deletes data!
docker compose up -d postgres
```

### RabbitMQ Queue Backlog

```bash
# Check queue depth
curl -u codingagent:devPassword123! http://localhost:15672/api/queues

# Purge queue (dev only!)
curl -u codingagent:devPassword123! -X DELETE \
  http://localhost:15672/api/queues/%2F/queue-name/contents
```

### Hot Reload Not Working (Dev Mode)

```bash
# Ensure polling is enabled (Windows/Mac)
# Already set in docker-compose.apps.dev.yml:
# DOTNET_USE_POLLING_FILE_WATCHER: "true"
# CHOKIDAR_USEPOLLING: "true" (Angular)

# Rebuild volumes
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml down
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up --build
```

### Clean Restart

```bash
# Stop all services
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down

# Remove volumes (WARNING: deletes data!)
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down -v

# Remove all images
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down --rmi all

# Start fresh
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d
```

## 📈 Performance Tuning

### Resource Limits (Production)

Add to services in `docker-compose.apps.prod.yml`:

```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 2G
    reservations:
      cpus: '0.5'
      memory: 512M
```

### Database Performance

```sql
-- Connect to PostgreSQL
docker compose exec postgres psql -U codingagent -d codingagent

-- Check connection pool usage
SELECT count(*) FROM pg_stat_activity;

-- Enable query timing
ALTER DATABASE codingagent SET log_min_duration_statement = 100;
```

### Ollama GPU Support

Uncomment in `docker-compose.yml`:

```yaml
ollama:
  deploy:
    resources:
      reservations:
        devices:
          - driver: nvidia
            count: all
            capabilities: [gpu]
```

Requires NVIDIA Container Toolkit installed.

## 🔐 Security

### Production Checklist

- [ ] Change all default passwords in `.env`
- [ ] Use secrets management (Docker Swarm secrets, Kubernetes secrets)
- [ ] Enable HTTPS with valid certificates
- [ ] Configure firewall rules (only expose Gateway port)
- [ ] Enable RabbitMQ TLS
- [ ] Use read-only volumes where possible
- [ ] Scan images for vulnerabilities (`docker scan`)
- [ ] Implement rate limiting at Gateway
- [ ] Rotate JWT signing keys regularly
- [ ] Enable Grafana authentication
- [ ] Restrict Prometheus/Jaeger access

### Example: Docker Secrets (Swarm Mode)

```bash
# Create secrets
echo "supersecretpassword" | docker secret create postgres_password -
echo "supersecretredis" | docker secret create redis_password -

# Update docker-compose.yml
services:
  postgres:
    secrets:
      - postgres_password
    environment:
      POSTGRES_PASSWORD_FILE: /run/secrets/postgres_password

secrets:
  postgres_password:
    external: true
  redis_password:
    external: true
```

## 📚 Additional Resources

- [Architecture Overview](../../docs/00-OVERVIEW.md)
- [Service Catalog](../../docs/01-SERVICE-CATALOG.md)
- [Implementation Roadmap](../../docs/02-IMPLEMENTATION-ROADMAP.md)
- [Alerting Summary](./ALERTING-SUMMARY.md)
- [Runbooks](../../docs/runbooks/)

## 🤝 Support

For issues or questions:
1. Check logs: `docker compose logs <service>`
2. Review [troubleshooting section](#-troubleshooting)
3. Consult [runbooks](../../docs/runbooks/)
4. Open GitHub issue with logs attached

- **Database**: PostgreSQL 16 with pre-configured schemas
- **Cache**: Redis 7 with persistence
- **Messaging**: RabbitMQ 3.12 with management UI
- **Observability**: Prometheus, Grafana, Jaeger, Seq

## 📦 Prerequisites

### Required Software

- **Docker Desktop** 4.25+ or **Docker Engine** 24+
- **Docker Compose** 2.23+ (included with Docker Desktop)
- **Git** for cloning the repository
- **4GB+ RAM** available for containers
- **10GB+ Disk Space** for volumes

### Operating System

- ✅ Windows 10/11 with WSL2
- ✅ macOS 12+ (Intel or Apple Silicon)
- ✅ Linux (Ubuntu 20.04+, Debian 11+, etc.)

### Verification

```bash
# Check Docker version
docker --version
# Should be: Docker version 24.0.0 or higher

# Check Docker Compose version
docker compose version
# Should be: Docker Compose version 2.23.0 or higher

# Verify Docker is running
docker ps
# Should show: CONTAINER ID, IMAGE, COMMAND, etc. (may be empty)
```

## 🚀 Quick Start

### 1. Clone Repository

```bash
git clone https://github.com/JustAGameZA/coding-agent.git
cd coding-agent/deployment/docker-compose
```

### 2. Configure Environment

```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your configuration
# IMPORTANT: Change default passwords!
nano .env  # or use your preferred editor
```

### 3. Start Services

```bash
# Start all infrastructure services
docker compose up -d

# View logs
docker compose logs -f

# Check service status
docker compose ps


### 4. Verify Services

All services should show status as "healthy" after ~30 seconds:

```bash
docker compose ps

# Expected output:
# NAME                       STATUS              PORTS
# coding-agent-postgres      Up (healthy)        0.0.0.0:5432->5432/tcp
# coding-agent-redis         Up (healthy)        0.0.0.0:6379->6379/tcp
# coding-agent-rabbitmq      Up (healthy)        0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
# coding-agent-prometheus    Up (healthy)        0.0.0.0:9090->9090/tcp
# coding-agent-grafana       Up (healthy)        0.0.0.0:3000->3000/tcp
# coding-agent-jaeger        Up (healthy)        multiple ports
# coding-agent-seq           Up (healthy)        0.0.0.0:5341->80/tcp
```

### 5. Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **PostgreSQL** | `localhost:5432` | user: `codingagent`<br>password: `devPassword123!` |
| **Redis** | `localhost:6379` | password: `devPassword123!` |
| **RabbitMQ Management** | http://localhost:15672 | user: `codingagent`<br>password: `devPassword123!` |
| **RabbitMQ Prometheus** | http://localhost:15692 | No auth (metrics) |
| **Grafana** | http://localhost:3000 | user: `admin`<br>password: `admin` |
| **Prometheus** | http://localhost:9090 | No auth |
| **PostgreSQL Exporter** | http://localhost:9187 | No auth (metrics) |
| **Redis Exporter** | http://localhost:9121 | No auth (metrics) |
| **Jaeger UI** | http://localhost:16686 | No auth |
| **Seq** | http://localhost:5341 | No auth (first run) |

## 🏗️ Services

### PostgreSQL Database

**Purpose**: Primary data store for all microservices

**Schemas**:
- `chat` - Conversations, messages, attachments
- `orchestration` - Tasks, executions, results
- `github` - Repositories, pull requests, issues
- `cicd` - Workflow runs, build jobs, deployments
- `auth` - Users, roles, permissions

**Connection String**:
```
Host=localhost;Port=5432;Database=codingagent;Username=codingagent;Password=devPassword123!
```

**Management Tools**:
```bash
# Connect via psql
docker exec -it coding-agent-postgres psql -U codingagent -d codingagent

# List schemas
\dn

# List tables in a schema
\dt chat.*

# View table structure
\d chat.conversations
```

### Redis Cache

**Purpose**: High-performance caching and session storage

**Features**:
- Persistence enabled (AOF)
- Password-protected
- Connection pooling ready

**Connection String**:
```
localhost:6379,password=devPassword123!
```

**Management**:
```bash
# Connect to Redis CLI
docker exec -it coding-agent-redis redis-cli -a devPassword123!

# Test connection
PING
# Should return: PONG

# View cache keys
KEYS *

# Monitor real-time commands
MONITOR
```

### RabbitMQ Message Queue

**Purpose**: Asynchronous communication between microservices

**Features**:
- Management UI enabled
- Default vhost configured
- Ready for MassTransit integration

**Ports**:
- `5672` - AMQP protocol
- `15672` - Management UI

**Management UI**: http://localhost:15672
- Create exchanges, queues, bindings
- Monitor message rates
- View connection statistics

### Prometheus Metrics

**Purpose**: Metrics collection and storage

**Features**:
- 30-day retention
- Pre-configured service discovery
- Ready for Grafana integration

**URL**: http://localhost:9090

**Example Queries**:
```promql
# HTTP request rate
rate(http_requests_total[5m])

# 95th percentile response time
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Memory usage by service
container_memory_usage_bytes{service=~".*-service"}
```

### Grafana Dashboards

**Purpose**: Visualization and alerting

**Features**:
- Pre-configured Prometheus datasource
- Jaeger integration for traces
- Auto-provisioned dashboards for Coding Agent services

**URL**: http://localhost:3000

**Credentials**:
- **Username**: `admin`
- **Password**: `admin` (change on first login)

**Pre-configured Dashboards**:

All dashboards are automatically provisioned on startup and available in the "Coding Agent" folder:

1. **System Health** (`system-health`)
   - CPU, memory, disk usage
   - Container status and uptime
   - Network I/O metrics
   - Container restarts monitoring

2. **API Gateway (YARP)** (`api-gateway`)
   - Request rate and error rate
   - Latency percentiles (P50, P95, P99)
   - Circuit breaker state and events
   - Rate limiting counters

3. **Backend Services** (`backend-services`)
   - Per-service request rate and errors
   - Service-level latency metrics
   - EF Core database command duration
   - MassTransit consumer metrics and queue depth
   - Filter by service using dropdown

4. **Database (PostgreSQL)** (`database-postgresql`)
   - Active connections and cache hit ratio
   - Transaction rate and table operations
   - Slow queries (>100ms)
   - Database size and bloat metrics
   - Lock monitoring

5. **Cache (Redis)** (`cache-redis`)
   - Cache hit ratio and operations/sec
   - Memory usage and evictions
   - Command latency (GET/SET)
   - Connected clients and key count
   - Network I/O

**Accessing Dashboards**:
1. Navigate to http://localhost:3000
2. Login with credentials above
3. Go to **Dashboards** → **Browse** → **Coding Agent** folder
4. Select any dashboard to view real-time metrics

**Dashboard Locations**:
```
deployment/docker-compose/grafana/provisioning/dashboards/
├── dashboards.yml              # Provisioning configuration
├── system-health.json          # System metrics
├── api-gateway.json            # Gateway metrics
├── backend-services.json       # Microservices metrics
├── database-postgresql.json    # PostgreSQL metrics
└── cache-redis.json            # Redis metrics
```

**Additional Community Dashboards** (optional):
- Node Exporter Full (ID: 1860)
- Docker Monitoring (ID: 893)
- RabbitMQ Overview (ID: 10991)

### Jaeger Tracing

**Purpose**: Distributed tracing across microservices

**Features**:
- OpenTelemetry compatible
- OTLP gRPC and HTTP endpoints
- In-memory storage (all-in-one deployment)
- Distributed trace correlation with correlation IDs

**URL**: http://localhost:16686

**OTLP Endpoints**:
- gRPC: `http://localhost:4317` (from host) or `http://jaeger:4317` (from containers)
- HTTP: `http://localhost:4318` (from host) or `http://jaeger:4318` (from containers)

**Usage in .NET Services**:

All services are pre-configured to send traces to Jaeger via OTLP gRPC. The endpoint is configurable in `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "Endpoint": "http://jaeger:4317"
  }
}
```

Override at runtime with environment variables:
```bash
-e OpenTelemetry__Endpoint=http://jaeger:4317
```

**Verifying Traces**:

1. **Quick verification using the provided script**:
   ```bash
   cd deployment/docker-compose
   ./verify-jaeger.sh
   ```

2. **Manual verification steps**:

   a. **Start all infrastructure services**:
   ```bash
   docker compose up -d
   ```

   b. **Wait for services to be healthy** (~30 seconds):
   ```bash
   docker compose ps
   # All services should show "Up (healthy)"
   ```

   c. **Access Jaeger UI**: http://localhost:16686

   d. **Generate test traces** by making requests to services:
   ```bash
   # Via Gateway (recommended - shows full trace)
   curl http://localhost:5000/api/chat/ping
   curl http://localhost:5000/api/orchestration/ping

   # Direct to services
   curl http://localhost:5001/health  # Chat service
   curl http://localhost:5002/health  # Orchestration service
   ```

   e. **View traces in Jaeger UI**:
   - Select service from dropdown (e.g., "CodingAgent.Gateway")
   - Click "Find Traces"
   - Click on a trace to see the full span timeline
   - Verify correlation IDs propagate across services (look for `X-Correlation-Id` tag)

**Troubleshooting**:

- **No traces appearing**:
  - Check service logs: `docker compose logs gateway chat orchestration`
  - Verify Jaeger is healthy: `curl http://localhost:14269/`
  - Ensure OpenTelemetry endpoint is configured correctly in service appsettings

- **Traces missing correlation**:
  - Verify Gateway is propagating correlation ID headers
  - Check that downstream services are instrumented with ASP.NET Core instrumentation

### Seq Structured Logging

**Purpose**: Centralized log aggregation and search

**Features**:
- Structured logging with full-text search
- Real-time log streaming
- Query language for filtering

**URL**: http://localhost:5341

**Ingestion**: `http://localhost:5342`

**Usage in .NET**:
```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .WriteTo.Seq("http://seq:5341")
        .Enrich.WithProperty("Application", "ChatService");
});
```

## ⚙️ Configuration

### Environment Variables

Edit `.env` to customize configuration:

```bash
# Database
POSTGRES_USER=codingagent
POSTGRES_PASSWORD=your-secure-password

# Cache
REDIS_PASSWORD=your-redis-password

# Messaging
RABBITMQ_USER=codingagent
RABBITMQ_PASSWORD=your-rabbitmq-password

# Grafana (change default!)
GRAFANA_USER=admin
GRAFANA_PASSWORD=your-grafana-password
```

### Volume Management

**List volumes**:
```bash
docker volume ls | grep coding-agent
```

**Inspect volume**:
```bash
docker volume inspect coding-agent_postgres_data
```

**Backup volume**:
```bash
# Backup PostgreSQL data
docker run --rm \
  -v coding-agent_postgres_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/postgres-backup-$(date +%Y%m%d).tar.gz /data

# Backup Redis data
docker run --rm \
  -v coding-agent_redis_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/redis-backup-$(date +%Y%m%d).tar.gz /data
```

**Restore volume**:
```bash
# Stop services first
docker compose down

# Restore PostgreSQL
docker run --rm \
  -v coding-agent_postgres_data:/data \
  -v $(pwd):/backup \
  alpine sh -c "cd /data && tar xzf /backup/postgres-backup-YYYYMMDD.tar.gz --strip 1"

# Restart services
docker compose up -d
```

## 🏥 Health Checks

### Check All Services

```bash
# View health status
docker compose ps

# Check specific service health
docker inspect --format='{{.State.Health.Status}}' coding-agent-postgres
```

### Manual Health Checks

**PostgreSQL**:
```bash
docker exec coding-agent-postgres pg_isready -U codingagent
```

**Redis**:
```bash
docker exec coding-agent-redis redis-cli -a devPassword123! PING
```

**RabbitMQ**:
```bash
docker exec coding-agent-rabbitmq rabbitmq-diagnostics ping
```

**Prometheus**:
```bash
curl http://localhost:9090/-/healthy
```

**Grafana**:
```bash
curl http://localhost:3000/api/health
```

**Jaeger**:
```bash
curl http://localhost:14269/
```

## 📊 Monitoring & Observability

### Viewing Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f postgres
docker compose logs -f redis
docker compose logs -f rabbitmq

# Last 100 lines
docker compose logs --tail=100 postgres

# Since timestamp
docker compose logs --since 2024-10-24T10:00:00
```

### Resource Usage

```bash
# View container stats
docker stats

# Specific containers
docker stats coding-agent-postgres coding-agent-redis coding-agent-rabbitmq
```

### Metrics Endpoints

Once microservices are running, they will expose metrics:

- Gateway: http://localhost:5000/metrics
- Chat Service: http://localhost:5001/metrics
- Orchestration: http://localhost:5002/metrics
- ML Classifier: http://localhost:5003/metrics

## 🔧 Troubleshooting

### Services Won't Start

**Check logs**:
```bash
docker compose logs [service-name]
```

**Common issues**:
1. **Port already in use**: Change port in `.env` or `docker-compose.yml`
2. **Insufficient memory**: Increase Docker memory limit in Docker Desktop
3. **Volume permissions**: On Linux, ensure proper permissions

### PostgreSQL Connection Failed

```bash
# Check if container is running
docker compose ps postgres

# Check logs
docker compose logs postgres

# Test connection
docker exec -it coding-agent-postgres psql -U codingagent -d codingagent -c "SELECT version();"
```

### Redis Connection Failed

```bash
# Check if running
docker compose ps redis

# Test connection without password
docker exec -it coding-agent-redis redis-cli PING

# Test with password
docker exec -it coding-agent-redis redis-cli -a devPassword123! PING
```

### RabbitMQ Not Accessible

```bash
# Check status
docker compose ps rabbitmq

# View logs
docker compose logs rabbitmq

# Check ports
docker ps | grep rabbitmq

# Restart service
docker compose restart rabbitmq
```

### Health Checks Failing

```bash
# View detailed health check logs
docker inspect coding-agent-postgres | jq '.[0].State.Health'

# Increase health check interval
# Edit docker-compose.yml health check settings
```

### Reset Everything

```bash
# Stop and remove containers
docker compose down

# Remove volumes (WARNING: deletes all data!)
docker compose down -v

# Remove images
docker compose down --rmi all

# Clean start
docker compose up -d
```

## 🏭 Production Considerations

### Security

1. **Change default passwords** in `.env`
2. **Enable SSL/TLS** for external connections
3. **Use secrets management** (Docker Swarm secrets, Kubernetes secrets)
4. **Network isolation** with custom networks
5. **Regular security updates** of base images

### Backup Strategy

```bash
# Automated daily backup script
#!/bin/bash
DATE=$(date +%Y%m%d)
docker exec coding-agent-postgres pg_dump -U codingagent codingagent > backup-$DATE.sql
gzip backup-$DATE.sql
aws s3 cp backup-$DATE.sql.gz s3://your-bucket/backups/
```

### Resource Limits

Add resource limits in `docker-compose.yml`:

```yaml
services:
  postgres:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
```

### Scaling

For production workloads, consider:
- **Kubernetes**: For orchestration and auto-scaling
- **Separate database servers**: Dedicated PostgreSQL clusters
- **Redis Cluster**: For high availability
- **RabbitMQ Cluster**: For message queue HA
- **Load balancing**: Multiple service instances

### Monitoring & Alerts

Configure Grafana alerts:
1. Navigate to Alerting → Alert rules
2. Create alerts for:
   - High memory usage (>80%)
   - High CPU usage (>80%)
   - Disk space low (<10%)
   - Service downtime
   - High error rates

### Maintenance

```bash
# Regular maintenance tasks

# Update images
docker compose pull

# Recreate containers with new images
docker compose up -d

# Prune unused resources
docker system prune -a --volumes

# Vacuum PostgreSQL
docker exec coding-agent-postgres psql -U codingagent -d codingagent -c "VACUUM ANALYZE;"

# Check Redis memory
docker exec coding-agent-redis redis-cli -a "$REDIS_PASSWORD" INFO memory
## 📚 Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)

## 🆘 Getting Help

- **Documentation**: [docs/](../../docs)
- **Issues**: [GitHub Issues](https://github.com/JustAGameZA/coding-agent/issues)
- **Discussions**: [GitHub Discussions](https://github.com/JustAGameZA/coding-agent/discussions)

---

**Last Updated**: October 24, 2025
**Version**: 1.0.0
**Maintainer**: Coding Agent Team
