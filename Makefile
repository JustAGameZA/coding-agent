# Coding Agent - Docker Commands Makefile
# 
# Usage:
#   make dev          - Start development stack
#   make prod         - Start production stack
#   make build        - Build production images
#   make stop         - Stop all services
#   make clean        - Clean all containers and volumes
#   make logs         - View logs (all services)
#   make health       - Check health of all services
#   make test         - Run tests

.PHONY: help dev prod build stop clean logs health test infra

# Default target
help:
	@echo "╔════════════════════════════════════════════════════════════════╗"
	@echo "║          Coding Agent - Docker Commands                       ║"
	@echo "╚════════════════════════════════════════════════════════════════╝"
	@echo ""
	@echo "Development:"
	@echo "  make dev          - Start development stack (hot reload)"
	@echo "  make dev-d        - Start development stack (detached)"
	@echo "  make dev-logs     - View development logs"
	@echo ""
	@echo "Production:"
	@echo "  make prod         - Start production stack"
	@echo "  make prod-d       - Start production stack (detached)"
	@echo "  make prod-logs    - View production logs"
	@echo "  make build        - Build all production images"
	@echo ""
	@echo "Infrastructure:"
	@echo "  make infra        - Start infrastructure only"
	@echo "  make infra-stop   - Stop infrastructure"
	@echo ""
	@echo "Operations:"
	@echo "  make stop         - Stop all services"
	@echo "  make restart      - Restart all services"
	@echo "  make clean        - Clean containers and volumes (⚠️  deletes data)"
	@echo "  make ps           - Show running containers"
	@echo "  make logs         - View logs (all services)"
	@echo "  make health       - Check service health"
	@echo ""
	@echo "Testing:"
	@echo "  make test         - Run all tests"
	@echo "  make test-unit    - Run unit tests only"
	@echo "  make test-int     - Run integration tests only"
	@echo ""
	@echo "Database:"
	@echo "  make db-shell     - Connect to PostgreSQL"
	@echo "  make db-backup    - Backup database"
	@echo "  make db-restore   - Restore database"
	@echo ""
	@echo "Monitoring:"
	@echo "  make grafana      - Open Grafana (http://localhost:3000)"
	@echo "  make jaeger       - Open Jaeger (http://localhost:16686)"
	@echo "  make seq          - Open Seq (http://localhost:5341)"
	@echo ""

# ==========================================
# Development
# ==========================================
dev:
	@echo "🚀 Starting development stack..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up

dev-d:
	@echo "🚀 Starting development stack (detached)..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml up -d
	@echo "✅ Services started. View logs with: make dev-logs"

dev-logs:
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml logs -f

dev-stop:
	@echo "⏹️  Stopping development stack..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.dev.yml down

# ==========================================
# Production
# ==========================================
build:
	@echo "🔨 Building production images..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml build

prod: build
	@echo "🚀 Starting production stack..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up

prod-d: build
	@echo "🚀 Starting production stack (detached)..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml up -d
	@echo "✅ Services started. View logs with: make prod-logs"

prod-logs:
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml logs -f

prod-stop:
	@echo "⏹️  Stopping production stack..."
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down

# ==========================================
# Infrastructure Only
# ==========================================
infra:
	@echo "🚀 Starting infrastructure (databases, monitoring)..."
	@cd deployment/docker-compose && docker compose up -d

infra-stop:
	@echo "⏹️  Stopping infrastructure..."
	@cd deployment/docker-compose && docker compose down

# ==========================================
# Operations
# ==========================================
stop:
	@echo "⏹️  Stopping all services..."
	@cd deployment/docker-compose && docker compose down

restart:
	@echo "🔄 Restarting services..."
	@cd deployment/docker-compose && docker compose restart

clean:
	@echo "⚠️  WARNING: This will delete all containers, volumes, and data!"
	@echo "Press Ctrl+C to cancel, or wait 5 seconds..."
	@sleep 5
	@cd deployment/docker-compose && docker compose -f docker-compose.yml -f docker-compose.apps.prod.yml down -v --rmi all
	@echo "✅ Cleanup complete"

ps:
	@cd deployment/docker-compose && docker compose ps

logs:
	@cd deployment/docker-compose && docker compose logs -f

health:
	@echo "🏥 Checking service health..."
	@echo ""
	@echo "Gateway:         $$(curl -sf http://localhost:5500/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Chat:            $$(curl -sf http://localhost:5501/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Orchestration:   $$(curl -sf http://localhost:5502/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Ollama Service:  $$(curl -sf http://localhost:5503/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "GitHub:          $$(curl -sf http://localhost:5504/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Browser:         $$(curl -sf http://localhost:5505/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "CI/CD Monitor:   $$(curl -sf http://localhost:5506/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Dashboard:       $$(curl -sf http://localhost:5507/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "ML Classifier:   $$(curl -sf http://localhost:8000/health && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo ""
	@echo "Infrastructure:"
	@echo "Prometheus:      $$(curl -sf http://localhost:9090/-/healthy && echo '✅ Healthy' || echo '❌ Unhealthy')"
	@echo "Jaeger:          $$(curl -sf http://localhost:14269/ && echo '✅ Healthy' || echo '❌ Unhealthy')"

# ==========================================
# Testing
# ==========================================
test:
	@echo "🧪 Running all tests..."
	@dotnet test --verbosity quiet --nologo

test-unit:
	@echo "🧪 Running unit tests..."
	@dotnet test --filter "Category=Unit" --verbosity quiet --nologo

test-int:
	@echo "🧪 Running integration tests..."
	@dotnet test --filter "Category=Integration" --verbosity quiet --nologo

# ==========================================
# Database
# ==========================================
db-shell:
	@echo "🗄️  Connecting to PostgreSQL..."
	@cd deployment/docker-compose && docker compose exec postgres psql -U codingagent -d codingagent

db-backup:
	@echo "💾 Backing up database..."
	@cd deployment/docker-compose && docker compose exec postgres pg_dump -U codingagent codingagent > backup_$$(date +%Y%m%d_%H%M%S).sql
	@echo "✅ Backup complete: backup_$$(date +%Y%m%d_%H%M%S).sql"

db-restore:
	@echo "📥 Restoring database from backup..."
	@echo "Usage: cat backup.sql | cd deployment/docker-compose && docker compose exec -T postgres psql -U codingagent codingagent"

# ==========================================
# Monitoring
# ==========================================
grafana:
	@echo "📊 Opening Grafana..."
	@open http://localhost:3000 || xdg-open http://localhost:3000 || start http://localhost:3000

jaeger:
	@echo "🔍 Opening Jaeger..."
	@open http://localhost:16686 || xdg-open http://localhost:16686 || start http://localhost:16686

seq:
	@echo "📝 Opening Seq..."
	@open http://localhost:5341 || xdg-open http://localhost:5341 || start http://localhost:5341

rabbitmq:
	@echo "🐰 Opening RabbitMQ Management..."
	@open http://localhost:15672 || xdg-open http://localhost:15672 || start http://localhost:15672

# ==========================================
# Utilities
# ==========================================
shell-%:
	@echo "🐚 Opening shell in $* service..."
	@cd deployment/docker-compose && docker compose exec $* /bin/bash || docker compose exec $* /bin/sh

restart-%:
	@echo "🔄 Restarting $* service..."
	@cd deployment/docker-compose && docker compose restart $*

logs-%:
	@echo "📋 Viewing logs for $* service..."
	@cd deployment/docker-compose && docker compose logs -f $*
