# Observability Stack Implementation Summary
## Coding Agent - Microservices Platform

**Status**: ✅ **COMPLETE** - Comprehensive monitoring, alerting, and logging configured  
**Last Updated**: 2025-06-01

---

## Architecture Overview

```
┌──────────────┐     Metrics     ┌────────────┐     Alerts      ┌──────────────┐
│   Services   │ ────────────────>│ Prometheus │ ────────────────>│ Alertmanager │
│  (10 apps)   │     /metrics    │   2.48.0   │  Alert Rules   │    0.26.0    │
└──────────────┘                  └────────────┘                 └──────────────┘
       │                                 │                               │
       │                                 │                               │
       │ Traces (OTLP)                  │ Queries                      │ Webhooks
       │                                 │                               │
       ▼                                 ▼                               ▼
┌──────────────┐                  ┌────────────┐               ┌──────────────┐
│    Jaeger    │                  │  Grafana   │               │   Webhooks   │
│    1.52      │                  │   10.2.2   │               │ Slack/Email  │
└──────────────┘                  └────────────┘               └──────────────┘
       ▲                                 │
       │                                 │
       │ Structured Logs                │ Dashboards (8)
       │                                 │
┌──────────────┐                         │
│     Seq      │ <───────────────────────┘
│   2023.4     │    Serilog HTTP
└──────────────┘
```

---

## 1. Metrics Collection (Prometheus)

### Scrape Configuration

**Total Scrape Jobs**: 13  
**Scrape Interval**: 15 seconds  
**Retention**: 30 days  
**Storage**: Docker volume `prometheus-data`

#### Application Services (10 jobs)
| Service | Endpoint | Port | Metrics Library |
|---------|----------|------|-----------------|
| Gateway | `gateway:5000/metrics` | 5000 | OpenTelemetry + Prometheus |
| Chat | `chat-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| Orchestration | `orchestration-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| ML Classifier | `ml-classifier:8000/metrics` | 8000 | prometheus_fastapi_instrumentator |
| Ollama Service | `ollama-service:5003/metrics` | 5003 | OpenTelemetry + Prometheus |
| GitHub | `github-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| Browser | `browser-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| CI/CD Monitor | `cicd-monitor-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| Dashboard | `dashboard-service:8080/metrics` | 8080 | OpenTelemetry + Prometheus |
| Ollama Backend | `ollama:11434/metrics` | 11434 | Native Ollama metrics |

#### Infrastructure Services (4 jobs)
| Service | Endpoint | Port | Exporter |
|---------|----------|------|----------|
| PostgreSQL | `postgres-exporter:9187` | 9187 | postgres_exporter |
| Redis | `redis-exporter:9121` | 9121 | redis_exporter |
| RabbitMQ | `rabbitmq:15692/metrics` | 15692 | Native RabbitMQ Prometheus plugin |
| Node Metrics | `node-exporter:9100` | 9100 | node_exporter |
| Container Metrics | `cadvisor:8080` | 8080 | cAdvisor |

### Key Metrics by Service

#### API Gateway (YARP)
- `http_requests_total` - Total HTTP requests
- `http_request_duration_seconds` - Request latency histogram
- `circuit_breaker_state` - Circuit breaker status (open/closed)
- `rate_limiter_throttled_total` - Rate limiting events

#### Chat Service (SignalR)
- `signalr_active_connections` - Current active WebSocket connections
- `signalr_connection_failures_total` - Connection failure count
- `chat_message_delivery_failed_total` - Failed message deliveries
- `conversation_timeout_total` - Conversation timeouts

#### Orchestration Service
- `task_execution_total` - Total task executions
- `task_execution_failed_total` - Failed task executions
- `task_execution_duration_seconds` - Task execution latency by strategy
- `task_queue_pending_count` - Tasks waiting in queue

#### ML Classifier (Python)
- `ml_classification_requests_total{classifier}` - Classifications by classifier type (heuristic/ml/llm)
- `ml_classification_duration_seconds` - Classification latency
- `ml_classification_errors_total` - Classification errors
- `ml_model_accuracy{classifier}` - Model accuracy metrics
- `ml_confidence_score` - Confidence scores

#### Ollama Service
- `ollama_inference_requests_total{model}` - Inference requests by model
- `ollama_inference_duration_seconds{model}` - Inference latency by model
- `ollama_tokens_generated_total{model}` - Token generation count
- `ollama_cost_total{model}` - Estimated cost by model
- `ollama_queue_depth` - Current queue depth
- `ollama_inference_errors_total` - Inference errors

#### GitHub Service
- `github_api_remaining_requests` - GitHub API rate limit remaining
- `github_operation_total` - GitHub operations count
- `github_operation_failed_total` - Failed GitHub operations
- `github_pr_creation_duration_seconds` - PR creation latency

#### Browser Service (Playwright)
- `browser_automation_total` - Total browser automations
- `browser_automation_failed_total` - Failed automations
- `browser_timeout_total` - Browser timeouts

#### CI/CD Monitor
- `cicd_last_build_check_timestamp_seconds` - Last build check time
- `cicd_fix_attempted_total` - Automated fix attempts
- `cicd_fix_success_total` - Successful automated fixes

---

## 2. Alert Rules (Alertmanager)

### Alert Categories

**Total Alert Rules**: 53 alerts across 6 categories

#### API Alerts (5 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| APIErrorRateHigh | Critical | >5% error rate | 5 minutes |
| APILatencyHigh | Warning | p95 >500ms | 10 minutes |
| APIRequestRateHigh | Warning | >1000 req/s | 5 minutes |
| CircuitBreakerOpen | Critical | Circuit open | 2 minutes |
| RateLimiterActivelyThrottling | Warning | >100 throttled/min | 5 minutes |

#### Infrastructure Alerts (8 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| ContainerCPUHigh | Warning | >80% CPU | 10 minutes |
| ContainerMemoryHigh | Warning | >85% memory | 10 minutes |
| ContainerRestartRateHigh | Critical | >3 restarts/10m | 10 minutes |
| DiskSpaceLow | Warning | <20% free | 15 minutes |
| ServiceDown | Critical | Service unavailable | 2 minutes |
| PostgreSQLDown | Critical | Database unavailable | 1 minute |
| RedisDown | Critical | Cache unavailable | 1 minute |
| RabbitMQDown | Critical | Message bus unavailable | 1 minute |

#### Message Bus Alerts (8 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| RabbitMQQueueDepthHigh | Warning | >1000 messages | 10 minutes |
| RabbitMQQueueDepthCritical | Critical | >10000 messages | 5 minutes |
| RabbitMQNoConsumers | Critical | 0 consumers on queue | 5 minutes |
| RabbitMQHighPublishRate | Warning | >1000 msg/s | 10 minutes |
| RabbitMQConsumerUtilizationHigh | Warning | >90% utilization | 10 minutes |
| RabbitMQConnectionFailures | Critical | Connection failures | 5 minutes |
| RabbitMQMemoryHigh | Warning | >80% memory | 10 minutes |
| RabbitMQDiskSpaceLow | Critical | <1GB free | 5 minutes |

#### ML/AI Alerts (8 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| MLClassifierDown | Critical | Service unavailable | 1 minute |
| MLClassificationLatencyHigh | Warning | >1s latency | 5 minutes |
| OllamaServiceDown | Critical | Service unavailable | 1 minute |
| OllamaBackendDown | Critical | Backend unavailable | 1 minute |
| LLMInferenceLatencyHigh | Warning | >5s latency | 5 minutes |
| MLModelAccuracyLow | Warning | <70% accuracy | 30 minutes |
| LLMInferenceErrors | Critical | >5% error rate | 5 minutes |
| OllamaQueueBacklog | Warning | >100 queued | 10 minutes |

#### Database Alerts (10 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| PostgreSQLConnectionsHigh | Warning | >80% max connections | 5 minutes |
| PostgreSQLConnectionPoolExhausted | Critical | >95% connections | 2 minutes |
| PostgreSQLSlowQueries | Warning | >100ms avg query time | 10 minutes |
| PostgreSQLDeadlocks | Critical | >0.1 deadlocks/s | 5 minutes |
| PostgreSQLReplicationLag | Critical | >10s lag | 5 minutes |
| PostgreSQLCacheMissRateHigh | Warning | >30% cache miss rate | 10 minutes |
| PostgreSQLTransactionRateHigh | Warning | >1000 tx/s | 10 minutes |
| PostgreSQLDiskIOHigh | Warning | >100MB/s disk I/O | 10 minutes |
| PostgreSQLTableBloat | Warning | >50% table bloat | 1 hour |
| PostgreSQLVacuumNotRunning | Warning | No vacuum >24h | N/A |

#### Application Alerts (14 rules)
| Alert | Severity | Threshold | Duration |
|-------|----------|-----------|----------|
| TaskExecutionFailureRateHigh | Critical | >10% failure rate | 10 minutes |
| TaskExecutionLatencyHigh | Warning | >30s execution time | 10 minutes |
| TaskQueueBacklog | Warning | >50 pending tasks | 10 minutes |
| SignalRConnectionFailures | Warning | >10 failures/s | 5 minutes |
| SignalRActiveConnectionsHigh | Warning | >1000 connections | 5 minutes |
| MessageDeliveryFailures | Critical | >5 failures/s | 5 minutes |
| GitHubAPIRateLimitHigh | Warning | <20% rate limit | 5 minutes |
| GitHubOperationFailureRate | Critical | >5% failure rate | 10 minutes |
| PRCreationLatencyHigh | Warning | >10s PR creation | 10 minutes |
| BrowserAutomationFailureRate | Warning | >10% failure rate | 10 minutes |
| BrowserTimeoutRateHigh | Warning | >1 timeout/s | 10 minutes |
| CICDBuildMonitoringLag | Warning | >10m since last check | 5 minutes |
| AutomatedFixSuccessRateLow | Info | <30% success rate | 30 minutes |
| ConversationTimeoutRateHigh | Warning | >0.5 timeouts/s | 10 minutes |

### Alert Routing

**Grouping**: By `alertname`, `severity`, `service`  
**Group Wait**: 10 seconds  
**Group Interval**: 5 minutes  
**Repeat Interval**: 3 hours  

#### Severity-Based Routing
- **Critical**: Immediate webhook notification + repeat every 30 minutes
- **Warning**: Webhook notification + repeat every 3 hours
- **Info**: Webhook notification + repeat every 12 hours

#### Inhibition Rules
- Critical alerts inhibit warning alerts for the same service
- ServiceDown inhibits all other alerts for that service

### Notification Channels (Production)

**Current**: Webhook receivers configured  
**Pending**:
- Slack webhook for team notifications
- PagerDuty integration for critical alerts
- Email SMTP for warning alerts

---

## 3. Visualization (Grafana)

### Dashboards

**Total Dashboards**: 8 provisioned dashboards

| Dashboard | UID | Panels | Key Metrics |
|-----------|-----|--------|-------------|
| **API Gateway** | `api-gateway` | 12 | Request rate, latency, error rate, circuit breaker status |
| **System Health** | `system-health` | 10 | CPU, memory, disk, network, container health |
| **Database (PostgreSQL)** | `database-postgresql` | 14 | Connections, queries, cache, replication, locks |
| **Cache (Redis)** | `cache-redis` | 8 | Hit rate, memory usage, evictions, commands |
| **Backend Services** | `backend-services` | 16 | Per-service metrics, latency, error rate, throughput |
| **Alerts & SLOs** | `alerts-slos` | 6 | Active alerts, alert history, SLO tracking |
| **ML Classifier** | `ml-classifier` | 11 | Classification rate, latency by classifier, accuracy, error rate, task type distribution |
| **Ollama Service** | `ollama-service` | 13 | Inference rate, latency by model, queue depth, token throughput, cost tracking |

### Dashboard Features

- **Auto-refresh**: 10 seconds
- **Time range**: Default 6 hours (configurable)
- **Variables**: Service selection, environment filtering
- **Annotations**: Alert events overlayed on charts
- **Drill-down**: Links to Seq for log correlation

### Grafana Configuration

- **URL**: http://localhost:3000
- **Credentials**: admin / admin (development)
- **Data Sources**: Prometheus (default), Seq (experimental)
- **Provisioning**: All dashboards auto-loaded from `/etc/grafana/provisioning/dashboards/*.json`

---

## 4. Distributed Tracing (Jaeger)

### Configuration

- **URL**: http://localhost:16686
- **Protocol**: OTLP (OpenTelemetry Protocol)
- **Ingestion**: gRPC (4317), HTTP (4318)
- **Retention**: 24 hours (memory storage)
- **Sampling**: All traces (100% sampling rate for development)

### Instrumented Services

All 9 .NET services use OpenTelemetry SDK with:
- **ASP.NET Core Instrumentation**: HTTP request/response tracing
- **HTTP Client Instrumentation**: Outbound HTTP call tracing
- **Entity Framework Core Instrumentation**: Database query tracing
- **Custom Spans**: Business logic operations

ML Classifier (Python) uses:
- **OpenTelemetry Python SDK**
- **FastAPI Instrumentation**
- **Custom spans** for classification operations

### Trace Propagation

- **Context Propagation**: W3C Trace Context standard
- **Correlation**: `traceparent` and `correlation-id` headers
- **Baggage**: User ID, tenant ID propagated across services

### Key Spans by Service

#### Gateway
- `http.request` - Incoming HTTP request
- `yarp.proxy` - YARP proxy operation
- `authentication` - JWT validation

#### Chat Service
- `signalr.connection` - SignalR connection lifecycle
- `chat.send_message` - Message sending operation
- `repository.save` - Database persistence

#### Orchestration Service
- `task.execute` - Task execution
- `strategy.{name}` - Execution strategy spans (SingleShot, Iterative, MultiAgent)
- `llm.call` - LLM inference call

#### ML Classifier
- `classify.hybrid` - Hybrid classification
- `classify.heuristic` - Heuristic classification
- `classify.ml` - ML model classification
- `classify.llm` - LLM fallback classification

#### Ollama Service
- `ollama.inference` - Inference request
- `model.select` - Model selection logic
- `queue.enqueue` - Queue operation

---

## 5. Structured Logging (Seq)

### Configuration

- **URL**: http://localhost:5341
- **Ingestion**: Serilog HTTP sink (port 5341)
- **Retention**: 14 days
- **Storage**: Docker volume `seq-data`
- **Authentication**: Disabled (development), required for production

### Log Levels

| Level | Use Case | Volume |
|-------|----------|--------|
| **Verbose** | Detailed debugging | High |
| **Debug** | Development debugging | Medium |
| **Information** | General operational events | Medium |
| **Warning** | Potential issues, degraded performance | Low |
| **Error** | Errors, exceptions, failures | Low |
| **Fatal** | Critical failures, service crashes | Very Low |

### Structured Properties

#### Standard Properties (All Services)
- `Service` - Service name
- `CorrelationId` - Request correlation ID
- `Environment` - Development/Staging/Production
- `MachineName` - Container/host name
- `@Timestamp` - Log timestamp (UTC)

#### Domain-Specific Properties

**Chat Service**:
- `ConversationId`, `MessageId`, `UserId`

**Orchestration Service**:
- `TaskId`, `TaskType`, `Strategy`, `Duration`

**ML Classifier**:
- `Classifier` (heuristic/ml/llm), `TaskType`, `Confidence`, `Accuracy`

**Ollama Service**:
- `Model`, `TokensGenerated`, `Cost`, `QueuePosition`

**GitHub Service**:
- `Repository`, `PRNumber`, `CommitSha`, `Operation`

### Common Queries (See SEQ-LOGGING-GUIDE.md)

- All errors: `@Level = "Error"`
- Service-specific errors: `Service = "CodingAgent.Services.Chat" and @Level = "Error"`
- Slow requests: `@Properties.ElapsedMilliseconds > 1000`
- Correlation tracking: `CorrelationId = "xyz789"`
- Task execution: `@MessageTemplate contains "Task" and Status = "Completed"`

---

## 6. Exporters & Collectors

### Node Exporter (9100)

**Metrics**: System-level metrics from Docker host
- CPU usage by core
- Memory usage and swap
- Disk I/O and space
- Network interfaces
- Filesystem stats

### cAdvisor (8080)

**Metrics**: Container-level metrics
- Container CPU usage
- Container memory usage
- Container network I/O
- Container filesystem I/O
- Container restart count

### PostgreSQL Exporter (9187)

**Metrics**: PostgreSQL database metrics
- Connection count and states
- Query performance (slow queries, deadlocks)
- Cache hit ratio
- Replication lag
- Table and index sizes
- Vacuum and analyze stats

### Redis Exporter (9121)

**Metrics**: Redis cache metrics
- Memory usage and fragmentation
- Hit/miss ratio
- Evicted keys
- Commands per second
- Connected clients
- Replication status

### RabbitMQ Native Metrics (15692)

**Metrics**: RabbitMQ message bus metrics (native Prometheus plugin)
- Queue depth by queue
- Consumer count
- Publish/deliver rate
- Message acknowledgments
- Connection and channel count
- Memory and disk usage

---

## 7. Health Checks

### Service Health Endpoints

All services expose `/health` endpoint:
- **Healthy**: 200 OK
- **Degraded**: 200 OK with warnings
- **Unhealthy**: 503 Service Unavailable

### Health Check Script

**Location**: `deployment/docker-compose/health-check.sh`

**Usage**:
```bash
./health-check.sh  # Check all services
```

**Checks**:
- Gateway: http://localhost:5000/health
- Chat: http://localhost:5001/health
- Orchestration: http://localhost:5002/health
- Ollama: http://localhost:5003/health
- ML Classifier: http://localhost:8000/health
- GitHub: http://localhost:5004/health
- Browser: http://localhost:5005/health
- CI/CD Monitor: http://localhost:5006/health
- Dashboard: http://localhost:5007/health
- Grafana: http://localhost:3000/api/health
- Prometheus: http://localhost:9090/-/healthy
- Seq: http://localhost:5341/api
- Jaeger: http://localhost:16686

---

## 8. Operational Runbooks

### Alert Runbooks

Located in `docs/runbooks/`:
- `api-error-rate-high.md` - API error spike investigation
- `api-latency-high.md` - Performance degradation troubleshooting
- `container-cpu-high.md` - CPU saturation response
- `container-memory-high.md` - Memory leak investigation
- `rabbitmq-queue-depth-high.md` - Message bus backlog resolution

### Runbook Structure

Each runbook includes:
1. **Symptoms**: What the alert indicates
2. **Impact**: User/business impact
3. **Investigation**: Step-by-step diagnosis
4. **Resolution**: Remediation steps
5. **Prevention**: Long-term fixes

---

## 9. Verification & Testing

### Verify Observability Stack

**Step 1**: Start infrastructure
```bash
docker compose up -d
```

**Step 2**: Check service health
```bash
./health-check.sh
```

**Step 3**: Verify Prometheus targets
- Open http://localhost:9090/targets
- Ensure all 13 targets are "UP"

**Step 4**: Verify Grafana dashboards
- Open http://localhost:3000
- Navigate to Dashboards → Browse
- Verify 8 dashboards loaded

**Step 5**: Verify Alertmanager
```bash
./validate-alerts.sh
```

**Step 6**: Verify Jaeger
```bash
./verify-jaeger.sh
```

**Step 7**: Verify Seq
- Open http://localhost:5341
- Search for `@Level = "Information"` to see logs

### Send Test Alerts

**Prometheus AlertManager**:
```bash
# Test webhook
curl -X POST http://localhost:9093/api/v1/alerts -d '[{
  "labels": {"alertname": "TestAlert", "severity": "critical"},
  "annotations": {"summary": "Test alert"}
}]'
```

**Check Alert Status**:
```bash
# Active alerts
curl http://localhost:9093/api/v1/alerts | jq

# Prometheus rules
curl http://localhost:9090/api/v1/rules | jq
```

---

## 10. Production Readiness Checklist

### ✅ Completed

- [x] Prometheus scraping all 13 targets (10 apps + 3 infrastructure)
- [x] 53 alert rules across 6 categories
- [x] Alertmanager routing with severity-based escalation
- [x] 8 Grafana dashboards for visualization
- [x] Jaeger distributed tracing with OTLP
- [x] Seq structured logging with 14-day retention
- [x] 4 exporters (PostgreSQL, Redis, Node, cAdvisor)
- [x] Health checks for all services
- [x] 5 operational runbooks

### ⚠️ Pending (Production)

- [ ] **Alertmanager Notification Channels**
  - [ ] Configure Slack webhook
  - [ ] Integrate PagerDuty for critical alerts
  - [ ] Set up SMTP for email notifications

- [ ] **Grafana Enhancements**
  - [ ] Enable authentication (OAuth, LDAP)
  - [ ] Set up user roles and permissions
  - [ ] Configure alert notification channels

- [ ] **Seq Production Config**
  - [ ] Enable authentication (API keys)
  - [ ] Configure TLS/HTTPS
  - [ ] Set up long-term log archival (S3/Azure Blob)

- [ ] **Jaeger Production Config**
  - [ ] Switch to persistent storage (Elasticsearch, Cassandra)
  - [ ] Configure sampling strategy (probabilistic, rate-limiting)
  - [ ] Set up trace retention policies

- [ ] **SLO Definitions**
  - [ ] Define Service Level Objectives per service
  - [ ] Create SLI dashboards
  - [ ] Set up error budget tracking

- [ ] **Capacity Planning**
  - [ ] Monitor Prometheus storage growth
  - [ ] Set up automated Seq log exports
  - [ ] Configure Grafana storage quota

---

## 11. Quick Commands

### View Metrics
```bash
# Prometheus UI
open http://localhost:9090

# Query specific metric
curl "http://localhost:9090/api/v1/query?query=up" | jq
```

### View Alerts
```bash
# Alertmanager UI
open http://localhost:9093

# Active alerts
curl http://localhost:9093/api/v1/alerts | jq '.data[] | select(.status.state == "active")'
```

### View Dashboards
```bash
# Grafana UI
open http://localhost:3000
```

### View Traces
```bash
# Jaeger UI
open http://localhost:16686

# Query specific trace
curl "http://localhost:16686/api/traces/{trace-id}"
```

### View Logs
```bash
# Seq UI
open http://localhost:5341

# Query via API
curl "http://localhost:5341/api/events?filter=@Level='Error'" | jq
```

### Restart Observability Stack
```bash
# Restart Prometheus + Alertmanager + Grafana
docker compose restart prometheus alertmanager grafana

# Reload Prometheus config without restart
curl -X POST http://localhost:9090/-/reload
```

---

## 12. Next Steps

1. **Configure Production Notification Channels** (Slack, PagerDuty, Email)
2. **Define Service-Level Objectives (SLOs)** for each service
3. **Set Up Automated Log Archival** for Seq (compliance/audit)
4. **Implement Trace Sampling Strategy** for Jaeger (production scale)
5. **Create Custom Grafana Dashboards** for business metrics
6. **Integrate with Incident Management** (PagerDuty, Opsgenie)
7. **Document On-Call Runbooks** for all critical alerts
8. **Set Up Log Forwarding** to long-term storage (S3, Azure Blob)

---

## 13. References

- **Prometheus Documentation**: https://prometheus.io/docs/
- **Grafana Documentation**: https://grafana.com/docs/
- **Jaeger Documentation**: https://www.jaegertracing.io/docs/
- **Seq Documentation**: https://docs.datalust.co/
- **OpenTelemetry**: https://opentelemetry.io/docs/
- **Alertmanager**: https://prometheus.io/docs/alerting/latest/alertmanager/

---

**Maintained By**: Platform Team  
**Last Review**: 2025-06-01  
**Next Review**: 2025-07-01
