# Deployment Guide

Complete guide for deploying the CodingAgent microservices platform using Docker Compose (development) and Kubernetes (production).

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Development Deployment (Docker Compose)](#development-deployment-docker-compose)
3. [Production Deployment (Kubernetes)](#production-deployment-kubernetes)
4. [Configuration](#configuration)
5. [Health Checks](#health-checks)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools
- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2.0+
- PowerShell 7+ (for Windows) or Bash (for Linux/Mac)
- Git

### Optional Tools
- kubectl (for Kubernetes deployment)
- Helm (for Kubernetes package management)

---

## Development Deployment (Docker Compose)

### Quick Start

1. **Clone Repository**
   ```bash
   git clone <repository-url>
   cd coding-agent
   ```

2. **Start Services**
   ```bash
   cd deployment/docker-compose
   docker-compose up -d
   ```

3. **Verify Services**
   ```bash
   docker-compose ps
   ```

All services should show as "healthy" after startup.

### Service Endpoints

Once running, services are available at:

| Service | URL | Description |
|---------|-----|-------------|
| Gateway | http://localhost:5000 | API Gateway (entry point) |
| Auth Service | http://localhost:5001 | Authentication service |
| Chat Service | http://localhost:5002 | Chat/messaging service |
| Orchestration | http://localhost:5003 | Task orchestration service |
| GitHub Service | http://localhost:5004 | GitHub integration service |
| Browser Service | http://localhost:5005 | Browser automation service |
| CI/CD Monitor | http://localhost:5006 | CI/CD monitoring service |
| Dashboard | http://localhost:5007 | Dashboard/BFF service |
| Frontend | http://localhost:4200 | Angular frontend |

### Infrastructure Services

| Service | URL | Credentials |
|---------|-----|-------------|
| PostgreSQL | localhost:5432 | postgres/postgres |
| Redis | localhost:6379 | (no auth) |
| RabbitMQ | http://localhost:15672 | guest/guest |
| Prometheus | http://localhost:9090 | (no auth) |
| Grafana | http://localhost:3000 | admin/admin |
| Jaeger | http://localhost:16686 | (no auth) |
| Seq | http://localhost:5341 | (no auth) |

### Configuration Files

Configuration files are located in `deployment/docker-compose/`:
- `docker-compose.yml` - Main compose file
- `docker-compose.apps.dev.yml` - Application services (dev)
- `docker-compose.apps.prod.yml` - Application services (prod)
- `docker-compose.override.yml.template` - Override template

### Environment Variables

Create `.env` file or set environment variables:

```env
POSTGRES_PASSWORD=postgres
REDIS_PASSWORD=
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
JWT_SECRET=<generate-secure-secret>
```

### Database Setup

1. **Run Migrations**
   ```powershell
   cd deployment/docker-compose
   ./migrate.ps1
   ```

2. **Seed Admin User** (optional)
   ```powershell
   ./seed-admin-user.ps1
   ```

### Stopping Services

```bash
docker-compose down
```

To also remove volumes (⚠️ deletes data):
```bash
docker-compose down -v
```

---

## Production Deployment (Kubernetes)

### Prerequisites

- Kubernetes cluster (v1.28+)
- kubectl configured
- Helm 3.x installed
- Container registry access

### 1. Build and Push Images

```bash
# Build images
docker build -t <registry>/coding-agent-gateway:latest -f src/Gateway/CodingAgent.Gateway/Dockerfile .
docker build -t <registry>/coding-agent-auth:latest -f src/Services/Auth/CodingAgent.Services.Auth/Dockerfile .
# ... (build other services)

# Push to registry
docker push <registry>/coding-agent-gateway:latest
docker push <registry>/coding-agent-auth:latest
# ... (push other services)
```

### 2. Create Kubernetes Secrets

```bash
# Create namespace
kubectl create namespace coding-agent

# Create secrets
kubectl create secret generic coding-agent-secrets \
  --from-literal=jwt-secret=<secure-secret> \
  --from-literal=postgres-password=<password> \
  --from-literal=redis-password=<password> \
  -n coding-agent
```

### 3. Deploy Infrastructure

```yaml
# postgresql.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:16-alpine
        env:
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: coding-agent-secrets
              key: postgres-password
        volumeMounts:
        - name: postgres-data
          mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
  - metadata:
      name: postgres-data
    spec:
      accessModes: [ReadWriteOnce]
      resources:
        requests:
          storage: 20Gi
```

### 4. Deploy Applications

Deploy each service as a Kubernetes Deployment:

```yaml
# gateway-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gateway
spec:
  replicas: 3
  selector:
    matchLabels:
      app: gateway
  template:
    metadata:
      labels:
        app: gateway
    spec:
      containers:
      - name: gateway
        image: <registry>/coding-agent-gateway:latest
        ports:
        - containerPort: 5000
        env:
        - name: JWT__SecretKey
          valueFrom:
            secretKeyRef:
              name: coding-agent-secrets
              key: jwt-secret
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: gateway
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 5000
  selector:
    app: gateway
```

### 5. Deploy Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: coding-agent-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - api.codingagent.example.com
    secretName: coding-agent-tls
  rules:
  - host: api.codingagent.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: gateway
            port:
              number: 80
```

### 6. Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: gateway-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: gateway
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

---

## Configuration

### Environment Variables

Services support configuration via environment variables or `appsettings.json`:

**Gateway:**
- `JWT__SecretKey` - JWT signing key
- `Redis__Connection` - Redis connection string
- `ReverseProxy__Routes` - YARP route configuration

**Services:**
- `ConnectionStrings__ChatDb` - Database connection
- `Redis__Connection` - Redis connection
- `RabbitMQ__Connection` - RabbitMQ connection

### Configuration Priority

1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

---

## Health Checks

### Service Health Endpoints

All services expose health checks at `/health`:

```bash
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # Auth
curl http://localhost:5002/health  # Chat
```

### Kubernetes Health Checks

Configure liveness and readiness probes:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 5000
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 5
```

---

## Troubleshooting

### Common Issues

1. **Services not starting**: Check logs with `docker-compose logs <service>`
2. **Database connection failures**: Verify PostgreSQL is running and connection strings are correct
3. **Network issues**: Ensure services are on same Docker network

### Useful Commands

```bash
# View all logs
docker-compose logs -f

# Restart a service
docker-compose restart <service>

# View service status
docker-compose ps

# Execute command in container
docker-compose exec <service> <command>

# Check service health
curl http://localhost:<port>/health
```

For more troubleshooting, see `docs/runbooks/common-issues-resolutions.md`.

---

## Next Steps

1. Review `docs/QUICK-START.md` for local development setup
2. Check `docs/runbooks/` for operational procedures
3. See `docs/02-IMPLEMENTATION-ROADMAP.md` for project status

---

**Last Updated**: December 2025
**Maintained By**: Platform Team

