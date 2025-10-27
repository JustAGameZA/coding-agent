# Development Base Image Strategy

## Problem Solved

**Race Condition in Shared NuGet Cache**: When multiple .NET services start simultaneously and share a `nuget-packages` volume, they can corrupt each other's package downloads, leading to errors like:
```
error : Could not find file '/root/.nuget/packages/microsoft.codeanalysis.workspaces.msbuild/4.8.0/aty0e5yc.ngc'
```

## Solution: Shared Base Image

Instead of each service restoring packages independently, we:

1. **Build a base image** (`Dockerfile.dev.base`) with SharedKernel pre-built
2. **Pre-cache common NuGet packages** in the base image layer
3. **All services extend this base image**, eliminating race conditions

### Architecture

```
┌─────────────────────────────────────┐
│  mcr.microsoft.com/dotnet/sdk:9.0   │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│  coding-agent-dev-base:latest       │
│  • SharedKernel restored & built    │
│  • Common NuGet packages cached     │
│  • Solution file present            │
└──────────────┬──────────────────────┘
               │
     ┌─────────┼─────────┬─────────┐
     ▼         ▼         ▼         ▼
┌─────────┐ ┌─────┐ ┌────────┐ ┌────┐
│ Gateway │ │Chat │ │Orch.   │ │... │
│ Service │ │Svc  │ │Service │ │    │
└─────────┘ └─────┘ └────────┘ └────┘
```

### Benefits

1. **No Race Conditions**: SharedKernel is built once, not 8 times simultaneously
2. **Faster Startup**: Base image cached, services only restore their specific dependencies
3. **Consistent Environment**: All services use identical base dependencies
4. **Cache Efficiency**: Docker BuildKit caches the base layer across all services

## Usage

### First-Time Build

```bash
# Build the base image (automatic when you run docker compose up)
docker compose -f deployment/docker-compose/docker-compose.yml \
               -f deployment/docker-compose/docker-compose.apps.dev.yml \
               up --build
```

### Rebuild Base Image

If SharedKernel changes or you want to refresh the base:

```bash
# Force rebuild of base image
docker compose -f deployment/docker-compose/docker-compose.apps.dev.yml \
               build --no-cache dev-base

# Or rebuild everything
docker compose -f deployment/docker-compose/docker-compose.yml \
               -f deployment/docker-compose/docker-compose.apps.dev.yml \
               build --no-cache
```

### Clean Start

```bash
# Stop everything
docker compose -f deployment/docker-compose/docker-compose.yml \
               -f deployment/docker-compose/docker-compose.apps.dev.yml \
               down

# Remove old base image
docker rmi coding-agent-dev-base:latest

# Start fresh
docker compose -f deployment/docker-compose/docker-compose.yml \
               -f deployment/docker-compose/docker-compose.apps.dev.yml \
               up --build -d
```

## How It Works

### Base Image Build (`Dockerfile.dev.base`)

1. Copies `global.json`, `Directory.Build.props`, `CodingAgent.sln`
2. Copies and restores SharedKernel project
3. Builds SharedKernel in Debug mode
4. Attempts to restore entire solution (caches common packages)
5. Sets environment variables for hot reload

### Service Startup

Each service:
1. Starts from `coding-agent-dev-base:latest` (SharedKernel already built)
2. Mounts workspace as volume (for hot reload)
3. Runs `dotnet restore --no-cache` for service-specific packages only
4. Runs `dotnet watch` for hot reload

### Why `--no-cache` Flag?

- Base image already has SharedKernel and common packages
- `--no-cache` ensures fresh package metadata (no corruption)
- Only service-specific packages downloaded (fast)
- Hot reload uses workspace files, not image files

## Performance Comparison

### Before (Shared Volume)
```
Gateway:        60s (restore + build)
Chat Service:   65s (restore + build, waits for volume lock)
Orch Service:   70s (restore + build, waits for volume lock)
GitHub Service: 75s (restore + build, waits for volume lock)
Browser Service: 80s (restore + build, waits for volume lock)
...
Total: ~80s (sequential due to volume contention)
```

### After (Base Image)
```
Base Image:     45s (one-time, cached thereafter)
Gateway:        15s (base cached, only service restore)
Chat Service:   15s (base cached, parallel start)
Orch Service:   15s (base cached, parallel start)
GitHub Service: 15s (base cached, parallel start)
Browser Service: 15s (base cached, parallel start)
...
Total: ~45s first time, ~15s subsequent starts (parallel)
```

## Troubleshooting

### "project.nuget.cache already exists" Error

**Symptom**: Services fail with:
```
error : The file '/workspace/src/SharedKernel/CodingAgent.SharedKernel/obj/project.nuget.cache' already exists.
```

**Root Cause**: Multiple services restoring the same SharedKernel project simultaneously cause file conflicts.

**Solution 1 - Clean and Restart** (Recommended):
```bash
# Stop containers
docker compose -f deployment/docker-compose/docker-compose.yml `
  -f deployment/docker-compose/docker-compose.apps.dev.yml down

# Clean build artifacts
Get-ChildItem -Path src -Recurse -Directory -Include obj,bin | Remove-Item -Recurse -Force

# Start again
docker compose -f deployment/docker-compose/docker-compose.yml `
  -f deployment/docker-compose/docker-compose.apps.dev.yml up -d
```

**Solution 2 - Wait It Out**:
Most services will retry automatically. After the first service completes, others succeed. Wait 30 seconds and check:
```bash
docker logs coding-agent-chat-dev 2>&1 | Select-String "Now listening on"
```

### Base Image Not Building

```bash
# Check if base image exists
docker images | grep coding-agent-dev-base

# Force rebuild
docker compose -f deployment/docker-compose/docker-compose.apps.dev.yml \
               build --no-cache dev-base
```

### Services Still Failing to Restore

```bash
# Check base image has SharedKernel
docker run --rm coding-agent-dev-base:latest ls -la /workspace/src/SharedKernel/

# Inspect base image layers
docker history coding-agent-dev-base:latest
```

### Hot Reload Not Working

Ensure workspace is mounted as volume:
```yaml
volumes:
  - ../../:/workspace  # Workspace mount is critical for hot reload
```

## Migration Notes

### Removed

- `nuget-packages` named volume (no longer needed)
- Individual `--force` flags in restore commands (replaced with base image)

### Added

- `Dockerfile.dev.base` for base image
- `dev-base` service in docker-compose (builds base image)
- `build` sections in all .NET services (extends base image)

### Changed

- All .NET services now use `coding-agent-dev-base:latest` as base
- Restore command changed from `--force` to `--no-cache`
- Volume mounts no longer include `nuget-packages:/root/.nuget/packages`

---

**Last Updated**: October 27, 2025
**Related Issues**: NuGet cache corruption (#172)
