# Docker Quick Start Guide

Fast reference for common Docker Compose operations.

## 🚀 Common Commands

### Start Everything (Development)
```bash
# From repo root
cd deployment/docker-compose

# Infrastructure + Dev Apps (hot reload)
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up -d

# View logs
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml logs -f

# Just infrastructure (databases, monitoring)
docker compose up -d
```

### Start Everything (Production)
```bash
# Build and start production images
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d

# Check status
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml ps

# View logs (all services)
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml logs -f

# View logs (specific service)
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml logs -f gateway
```

### Stop Services
```bash
# Stop all (keep data)
docker compose down

# Stop apps only (keep infrastructure running)
docker compose -f docker-compose.apps.dev.yml down

# Stop all + delete volumes (WARNING: deletes data!)
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down -v

# Stop all + delete images
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down --rmi all
```

### Restart Services
```bash
# Restart specific service
docker compose restart postgres

# Restart all services
docker compose restart

# Rebuild and restart (dev mode)
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up --build -d
```

### View Status
```bash
# Check running containers
docker compose ps

# Check all containers (including stopped)
docker compose ps -a

# Check resource usage
docker stats

# Check container health
docker inspect coding-agent-gateway | grep -A 10 "Health"
```

### Logs
```bash
# Tail logs (all services)
docker compose logs -f

# Tail logs (specific service)
docker compose logs -f postgres

# Last 100 lines
docker compose logs --tail=100 chat-service

# Logs since timestamp
docker compose logs --since 2025-10-27T10:00:00 orchestration-service

# Search logs
docker compose logs | grep -i "error"
```

### Execute Commands in Containers
```bash
# PostgreSQL
docker compose exec postgres psql -U codingagent -d codingagent

# Redis CLI
docker compose exec redis redis-cli -a devPassword123!

# RabbitMQ shell
docker compose exec rabbitmq rabbitmqctl list_queues

# .NET service shell
docker compose exec chat-service bash

# Python service shell
docker compose exec ml-classifier bash
```

## 🔍 Debugging

### Check Service Health
```bash
# All health endpoints
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # Chat
curl http://localhost:5002/health  # Orchestration
curl http://localhost:5006/health  # CI/CD Monitor

# Detailed health check
docker compose exec gateway curl -v http://localhost:5500/health
```

### Inspect Container
```bash
# Full container details
docker inspect coding-agent-gateway

# Just IP address
docker inspect -f '{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}' coding-agent-gateway

# Environment variables
docker inspect -f '{{.Config.Env}}' coding-agent-chat
```

### Database Operations
```bash
# Connect to PostgreSQL
docker compose exec postgres psql -U codingagent -d codingagent

# List schemas
docker compose exec postgres psql -U codingagent -d codingagent -c '\dn'

# List tables in schema
docker compose exec postgres psql -U codingagent -d codingagent -c '\dt chat.*'

# Run query
docker compose exec postgres psql -U codingagent -d codingagent -c 'SELECT COUNT(*) FROM chat.conversations;'

# Backup database
docker compose exec postgres pg_dump -U codingagent codingagent > backup.sql

# Restore database
docker compose exec -T postgres psql -U codingagent codingagent < backup.sql
```

### Redis Operations
```bash
# Connect to Redis
docker compose exec redis redis-cli -a devPassword123!

# Check keys
docker compose exec redis redis-cli -a devPassword123! KEYS '*'

# Get value
docker compose exec redis redis-cli -a devPassword123! GET conversation:abc123

# Flush cache (WARNING: deletes all cache)
docker compose exec redis redis-cli -a devPassword123! FLUSHALL
```

### RabbitMQ Operations
```bash
# List queues
docker compose exec rabbitmq rabbitmqctl list_queues

# List exchanges
docker compose exec rabbitmq rabbitmqctl list_exchanges

# List bindings
docker compose exec rabbitmq rabbitmqctl list_bindings

# Purge queue
docker compose exec rabbitmq rabbitmqctl purge_queue task-execution-queue
```

## 🧹 Cleanup

### Remove Unused Resources
```bash
# Remove stopped containers
docker container prune -f

# Remove unused images
docker image prune -a -f

# Remove unused volumes
docker volume prune -f

# Remove unused networks
docker network prune -f

# Clean everything (WARNING: removes all stopped containers, unused networks, images, build cache)
docker system prune -a --volumes -f
```

### Fresh Start
```bash
# Stop and remove everything for this project
cd deployment/docker-compose
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down -v --rmi all

# Start fresh
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up --build -d
```

## 📊 Monitoring

### Prometheus Queries
```bash
# Check if services are up
curl 'http://localhost:9090/api/v1/query?query=up'

# Chat service request rate
curl 'http://localhost:9090/api/v1/query?query=rate(http_server_requests_total{service="chat"}[5m])'

# Orchestration service memory usage
curl 'http://localhost:9090/api/v1/query?query=process_working_set_bytes{service="orchestration"}'
```

### Grafana Dashboards
```bash
# Access Grafana
open http://localhost:3000

# Default credentials
# Username: admin
# Password: admin (change on first login)
```

### Jaeger Traces
```bash
# Access Jaeger UI
open http://localhost:16686

# Query via API
curl 'http://localhost:16686/api/traces?service=coding-agent-orchestration&limit=10'
```

### Seq Logs
```bash
# Access Seq UI
open http://localhost:5341

# Query via API (last 100 errors)
curl 'http://localhost:5341/api/events?filter=@Level=%27Error%27&count=100'
```

## 🔧 Development Workflow

### Typical Dev Session
```bash
# 1. Start infrastructure
docker compose up -d

# 2. Start dev apps with hot reload
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up

# 3. Make code changes (auto-reload triggers)

# 4. View logs
docker compose -f docker-compose.apps.dev.yml logs -f chat-service

# 5. Stop when done
docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml down
```

### Building for Production
```bash
# 1. Build all images
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml build

# 2. Tag images
docker tag coding-agent/gateway:latest coding-agent/gateway:v1.0.0

# 3. Push to registry (if needed)
docker push coding-agent/gateway:v1.0.0

# 4. Start production stack
docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up -d
```

## 📦 Port Reference

| Service | Port | Purpose |
|---------|------|---------|
| Gateway | 5000 | HTTP API |
| Gateway | 5500 | Health/Metrics |
| Chat | 5001 | HTTP API + SignalR |
| Chat | 5501 | Health/Metrics |
| Orchestration | 5002 | HTTP API |
| Orchestration | 5502 | Health/Metrics |
| Ollama Service | 5003 | HTTP API |
| Ollama Service | 5503 | Health/Metrics |
| GitHub | 5004 | HTTP API |
| GitHub | 5504 | Health/Metrics |
| Browser | 5005 | HTTP API |
| Browser | 5505 | Health/Metrics |
| CI/CD Monitor | 5006 | HTTP API |
| CI/CD Monitor | 5506 | Health/Metrics |
| Dashboard BFF | 5007 | HTTP API |
| Dashboard BFF | 5507 | Health/Metrics |
| ML Classifier | 8000 | Python FastAPI |
| Angular UI | 4200 | Web UI |
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Cache |
| RabbitMQ | 5672 | AMQP |
| RabbitMQ | 15672 | Management UI |
| Prometheus | 9090 | Metrics |
| Grafana | 3000 | Dashboards |
| Jaeger | 16686 | Tracing UI |
| Jaeger | 4317 | OTLP gRPC |
| Jaeger | 4318 | OTLP HTTP |
| Seq | 5341 | Logs UI |
| Ollama | 11434 | LLM Inference |

## 🆘 Emergency Commands

### Service is Unresponsive
```bash
# Force restart
docker compose restart gateway

# Kill and restart
docker compose kill gateway
docker compose up -d gateway

# Remove and recreate
docker compose rm -f gateway
docker compose up -d gateway
```

### Database is Corrupted
```bash
# Stop all services
docker compose down

# Remove database volume
docker volume rm coding-agent_postgres_data

# Restart (will reinitialize)
docker compose up -d postgres
```

### Out of Disk Space
```bash
# Check disk usage
docker system df

# Clean build cache
docker builder prune -a -f

# Remove old images
docker image prune -a -f --filter "until=24h"

# Remove all unused volumes
docker volume prune -f
```

### Container Keeps Crashing
```bash
# Check crash logs
docker compose logs --tail=100 crashing-service

# Try starting without restart policy
docker run --rm -it coding-agent/crashing-service:latest bash

# Debug with override
docker compose run --rm crashing-service bash
```

## 📚 More Resources

- [Full README](./README.md) - Complete documentation
- [Alerting Summary](./ALERTING-SUMMARY.md) - Alert configuration
- [Runbooks](../../docs/runbooks/) - Operational guides
- [Architecture](../../docs/00-OVERVIEW.md) - System design
