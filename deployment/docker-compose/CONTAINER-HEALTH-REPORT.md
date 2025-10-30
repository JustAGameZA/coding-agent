# Container Health Report
**Date**: October 28, 2025  
**Reporter**: Development Team (DevOps Engineer + Tech Lead)

## Executive Summary

Comprehensive health check of all Docker containers identified **2 critical issues** and **3 services with warnings**. All issues are related to dotnet watch hot reload conflicts and missing healthcheck dependencies.

---

## Critical Issues

### 1. Auth Service (coding-agent-auth-dev) - CRASHED ❌

**Status**: Exited (134) - SIGABRT fatal error  
**Root Cause**: dotnet watch PollingDirectoryWatcher experiencing dictionary key collision  
**Error**: `System.ArgumentException: An item with the same key has already been added. Key: /workspace/src/SharedKernel/CodingAgent.SharedKernel/obj/Debug/refint`

**Impact**: Auth service unavailable; all JWT authentication blocked

**Resolution Steps**:
1. ✅ Stopped and removed container
2. ✅ Cleaned volumes
3. ⏳ Rebuild required with fresh state
4. ⏳ Consider disabling hot reload in production containers

**Recommended Fix**:
```bash
# Rebuild auth service from scratch
docker compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml \
  build --no-cache auth-dev

# Start with clean state
docker compose -f deployment/docker-compose/docker-compose.yml \
  -f deployment/docker-compose/docker-compose.apps.dev.yml \
  up -d auth-dev
```

**Prevention**: Add dotnet watch configuration to exclude problematic paths:
```xml
<!-- Add to .csproj -->
<ItemGroup>
  <Watch Exclude="obj/**/*;bin/**/*" />
</ItemGroup>
```

---

### 2. Ollama Backend (coding-agent-ollama) - UNHEALTHY ⚠️

**Status**: Running but failing healthchecks  
**Root Cause**: Healthcheck uses `curl` which is not installed in ollama/ollama:latest image  
**Error**: `exec: "curl": executable file not found in $PATH`

**Secondary Issue**: Prometheus scraping `/api/metrics` endpoint returns 404 (endpoint doesn't exist)

**Impact**: 
- Health monitoring fails (marked unhealthy)
- Prometheus metrics collection failing
- Container considered degraded by orchestration

**Resolution**:

**Option 1: Use wget (likely available)**
```yaml
healthcheck:
  test: ["CMD-SHELL", "wget --spider --quiet http://localhost:11434/api/tags || exit 1"]
  interval: 30s
  timeout: 10s
  start_period: 60s
  retries: 5
```

**Option 2: Use nc (netcat)**
```yaml
healthcheck:
  test: ["CMD-SHELL", "echo -e 'GET /api/tags HTTP/1.1\\r\\nHost: localhost\\r\\n\\r\\n' | nc localhost 11434 | grep -q HTTP || exit 1"]
```

**Option 3: Use Python (most reliable)**
```yaml
healthcheck:
  test: ["CMD-SHELL", "python3 -c 'import urllib.request; urllib.request.urlopen(\"http://localhost:11434/api/tags\")' || exit 1"]
```

**Fix for Prometheus 404**:
- Ollama doesn't expose `/api/metrics` by default
- Either:
  1. Remove Ollama from Prometheus scrape targets, OR
  2. Add a sidecar exporter if metrics are needed

**File to Update**: `deployment/docker-compose/docker-compose.yml` or where ollama service is defined

---

## Services Running with Warnings

### 3. Chat Service (coding-agent-chat-dev) - RUNNING with ERRORS ⚠️

**Issues**:
1. **Pending EF Core Migrations**
   - Error: `The model for context 'ChatDbContext' has pending changes`
   - Service continues in non-production mode but data model is out of sync

2. **MSBuild Package Missing**
   - `Microsoft.EntityFrameworkCore.Analyzers, version 9.0.0 was not found`
   - Hot reload affected but service functional

**Resolution**:
```bash
# Add migration for pending changes
cd src/Services/Chat/CodingAgent.Services.Chat
dotnet ef migrations add <MigrationName> --context ChatDbContext

# Apply migration
dotnet ef database update --context ChatDbContext

# Or apply on container restart
docker restart coding-agent-chat-dev
```

**Prevention**: Run `dotnet ef migrations add` before deploying model changes

---

### 4. Gateway (coding-agent-gateway-dev) - RUNNING with BUILD FAILURES ⚠️

**Issues**:
1. **Dictionary Key Collision** (same as Auth)
   - `An item with the same key has already been added. Key: /workspace/src/Gateway/CodingAgent.Gateway/bin/Yarp.ReverseProxy.dll`

2. **Missing NuGet Package**
   - `Microsoft.Extensions.Configuration.Binder, version 9.0.10 was not found`

3. **Service Unavailable Connections**
   - Gateway attempting to connect to services during startup

**Status**: Service is running and functional despite build errors (using last successful build)

**Resolution**:
```bash
# Clear build artifacts and rebuild
docker compose exec gateway-dev sh -c "rm -rf /workspace/src/Gateway/*/bin /workspace/src/Gateway/*/obj"
docker restart coding-agent-gateway-dev

# If issue persists, rebuild from scratch
docker compose build --no-cache gateway-dev
docker compose up -d gateway-dev
```

---

### 5. Orchestration Service (coding-agent-orchestration-dev) - RUNNING with WARNINGS ⚠️

**Issues**:
1. **File Locking During Hot Reload**
   - `Could not copy "obj/Debug/CodingAgent.SharedKernel.pdb" ... because it is being used by another process`

2. **Missing Analyzers**
   - `Microsoft.CodeAnalysis.Analyzers, version 3.3.4 was not found`
   - `Microsoft.EntityFrameworkCore.Analyzers, version 9.0.0 was not found`

**Status**: Service functional, hot reload occasionally fails

**Resolution**: Same as Gateway - clear build artifacts if hot reload stops working

---

## Services Healthy ✅

The following services are running without errors:

1. **ML Classifier (coding-agent-ml-classifier-dev)** ✅
   - No errors in logs
   - Python service stable

2. **Dashboard BFF (coding-agent-dashboard-bff-dev)** ✅
   - Clean startup
   - Redis connection healthy

3. **Dashboard UI (coding-agent-dashboard-ui-dev)** ✅
   - Angular dev server running
   - HMR functional

4. **GitHub Service (coding-agent-github-dev)** ✅
   - No errors detected

5. **Browser Service (coding-agent-browser-dev)** ✅
   - Playwright running correctly

6. **CI/CD Monitor (coding-agent-cicd-monitor-dev)** ✅
   - Service operational

**Infrastructure Services** (all healthy):
- ✅ PostgreSQL (healthy)
- ✅ Redis (healthy)
- ✅ RabbitMQ (healthy)
- ✅ Prometheus (healthy)
- ✅ Grafana (healthy)
- ✅ Jaeger (healthy)
- ✅ Alertmanager (healthy)
- ✅ cAdvisor (healthy)
- ✅ Node Exporter (healthy)
- ✅ Postgres Exporter (healthy)
- ✅ Redis Exporter (healthy with minor 404s on Redis metrics endpoint - expected)
- ✅ Seq (running)

---

## Recommended Immediate Actions

### Priority 1 (Critical - Auth Down)
1. **Rebuild Auth Service** from scratch with clean volumes
2. **Disable hot reload** in production/staging containers (use regular restart instead)
3. **Add Watch exclusions** to all .csproj files to prevent obj/bin conflicts

### Priority 2 (High - Healthchecks Failing)
1. **Fix Ollama healthcheck** - use Python or wget instead of curl
2. **Remove Ollama from Prometheus** scrape config (or add proper exporter)

### Priority 3 (Medium - Data Consistency)
1. **Apply pending Chat Service migrations** before next deployment
2. **Clear Gateway build artifacts** if hot reload continues failing

### Priority 4 (Low - Quality of Life)
1. Add `.dockerignore` to exclude obj/bin from build context
2. Consider using multi-stage builds to separate build and runtime layers
3. Add dotnet watch configuration to all services

---

## Long-Term Recommendations

### Development Containers
- **Use separate dev/prod Dockerfiles**: Dev with hot reload, prod without
- **Add volume mounts for obj/bin**: Prevent conflicts with host
- **Configure dotnet watch properly**: Exclude problematic paths

### Healthchecks
- **Standardize healthcheck tools**: Ensure all base images have required binaries
- **Use /health endpoints**: Prefer application-provided health endpoints over process checks
- **Add startup delays**: Increase `start_period` for services with slow startup (ML Classifier, Ollama)

### Monitoring
- **Document expected 404s**: Redis exporter metrics endpoint, Ollama metrics endpoint
- **Add custom exporters**: For services without native Prometheus support
- **Configure log levels**: Reduce noise from expected connection errors during startup

---

## Technical Details

### Root Cause: dotnet watch Hot Reload Conflicts

The primary issue affecting Auth, Gateway, and Orchestration services is **dotnet watch's PollingDirectoryWatcher** maintaining a dictionary of file paths that can collide when:
1. Multiple projects reference SharedKernel
2. obj/bin directories are shared across containers/volumes
3. Hot reload triggers rapid file system changes

**Solution**: Exclude obj/bin from watch or use named volumes per service.

### Ollama Healthcheck Issue

The ollama/ollama:latest image is based on Ubuntu but has minimal tools installed. The healthcheck assumes `curl` is available, which it's not.

**Why 404 on /api/metrics?**
Ollama doesn't expose Prometheus metrics natively. The Prometheus config is attempting to scrape a non-existent endpoint.

---

## Verification Commands

```bash
# Check all container status
docker ps -a --format "table {{.Names}}\t{{.Status}}"

# Check specific service logs
docker logs --tail 50 coding-agent-<service-name>

# Test Ollama API directly
docker exec coding-agent-ollama python3 -c "import urllib.request; print(urllib.request.urlopen('http://localhost:11434/api/tags').read())"

# Check Auth service after rebuild
docker logs -f coding-agent-auth-dev

# Verify healthchecks
docker inspect <container> --format '{{json .State.Health}}' | jq
```

---

## Sign-Off

**Findings**: 2 critical, 3 warnings, 13 healthy  
**Recommendation**: Fix Auth service immediately, address Ollama healthcheck, schedule migration for Chat service  
**Estimated Fix Time**: 30 minutes for critical issues, 1 hour for all issues

**Next Review**: After fixes applied and services restarted
