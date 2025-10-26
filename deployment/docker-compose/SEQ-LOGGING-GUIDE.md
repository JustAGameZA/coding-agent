# Seq Structured Logging Guide
## Coding Agent - Microservices Platform

## Overview

Seq is configured at `http://localhost:5341` and ingests structured logs from all services via the Serilog HTTP sink. This guide covers how to query, analyze, and troubleshoot using Seq.

## Architecture

```
┌────────────┐     Serilog HTTP     ┌─────────┐
│  Services  │ ──────────────────> │   Seq   │
│ (.NET/Py)  │    Port 5341        │ 2023.4  │
└────────────┘                      └─────────┘
```

### Services Logging to Seq

All 10 application services send structured logs:
- **Gateway** (YARP proxy)
- **Chat Service** (SignalR + conversations)
- **Orchestration Service** (task execution)
- **ML Classifier** (Python FastAPI)
- **Ollama Service** (LLM inference)
- **GitHub Service** (repository operations)
- **Browser Service** (Playwright automation)
- **CI/CD Monitor** (build tracking)
- **Dashboard** (BFF)
- **Angular Dashboard** (frontend errors via API)

## Accessing Seq

**URL**: http://localhost:5341

No authentication required in development mode.

## Common Log Queries

### 1. Service Health Monitoring

#### All Errors (Last Hour)
```
@Level = "Error" and @Timestamp > Now() - 1h
```

#### Critical Errors by Service
```
@Level = "Fatal" or @Level = "Error"
| select Service, @Message, @Timestamp
| sort @Timestamp desc
| take 50
```

#### Service-Specific Errors
```
Service = "CodingAgent.Services.Chat" and @Level = "Error"
```

### 2. Performance Analysis

#### Slow Requests (>1 second)
```
@Properties.ElapsedMilliseconds > 1000
| select Service, @Message, ElapsedMilliseconds, @Timestamp
| sort ElapsedMilliseconds desc
```

#### Task Execution Times
```
@MessageTemplate = "Task {TaskId} completed in {Duration}ms"
| select TaskId, Duration, @Timestamp
| sort Duration desc
| take 100
```

#### ML Classification Latency
```
Service = "CodingAgent.Services.MLClassifier" and @MessageTemplate contains "classified in"
| select @Message, ElapsedMilliseconds, @Timestamp
```

### 3. Business Event Tracking

#### Task Completions
```
@MessageTemplate = "Task {TaskId} completed with status {Status}"
| select TaskId, Status, @Timestamp
| group by Status
| select count(*) as Count, Status
```

#### Pull Request Creations
```
@MessageTemplate contains "Created pull request"
| select @Message, PRNumber, Repository, @Timestamp
| sort @Timestamp desc
```

#### Chat Message Flow
```
Service = "CodingAgent.Services.Chat" and (@MessageTemplate contains "Message" or @MessageTemplate contains "Conversation")
| select ConversationId, UserId, @Message, @Timestamp
| sort @Timestamp desc
```

### 4. Correlation & Distributed Tracing

#### Trace All Operations for a Specific Task
```
TaskId = "abc123-def456"
| select Service, @Message, @Timestamp
| sort @Timestamp asc
```

#### Correlation ID Tracking (Full Request Flow)
```
CorrelationId = "xyz789"
| select Service, @Message, @Level, @Timestamp
| sort @Timestamp asc
```

#### User Activity Tracking
```
UserId = "user-guid-here"
| select Service, @Message, @Timestamp
| sort @Timestamp desc
| take 100
```

### 5. Infrastructure & Dependencies

#### Database Connection Issues
```
@Exception is not null and @Message contains "database"
| select Service, @Message, @Exception, @Timestamp
```

#### RabbitMQ Message Bus Errors
```
@Message contains "RabbitMQ" or @Message contains "MassTransit"
| where @Level = "Error" or @Level = "Warning"
```

#### Redis Cache Misses
```
@MessageTemplate contains "cache miss"
| group by Service
| select count(*) as Misses, Service
```

### 6. Security & Authentication

#### Failed Authentication Attempts
```
@Message contains "authentication failed" or @Message contains "unauthorized"
| select UserId, Service, @Message, @Timestamp
```

#### Rate Limiting Events
```
@MessageTemplate contains "rate limit"
| select UserId, @Message, @Timestamp
```

## Alert Queries (for Seq Alerts)

### High Error Rate (>10 errors/minute)
```
@Level = "Error"
| where @Timestamp > Now() - 1m
| group by Service
| select count(*) as ErrorCount, Service
| where ErrorCount > 10
```

### Slow Task Execution (>30 seconds)
```
@MessageTemplate = "Task {TaskId} completed in {Duration}ms"
| where Duration > 30000
```

### SignalR Connection Failures Spike
```
@MessageTemplate contains "SignalR connection failed"
| where @Timestamp > Now() - 5m
| group by time(1m)
| select count(*) as Failures
| where Failures > 10
```

## Structured Logging Best Practices

### .NET Services (Serilog)

#### Context Properties
All .NET services log with these contextual properties:
- `Service`: Service name (e.g., "CodingAgent.Services.Chat")
- `CorrelationId`: Request correlation ID (propagated across services)
- `Environment`: "Development" | "Staging" | "Production"
- `MachineName`: Container/host name

#### Example Log Statements
```csharp
// Good: Structured with properties
_logger.LogInformation("Task {TaskId} completed with status {Status} in {Duration}ms",
    task.Id, result.Status, stopwatch.ElapsedMilliseconds);

// Bad: String interpolation (not queryable)
_logger.LogInformation($"Task {task.Id} completed");
```

### Python Services (structlog)

#### ML Classifier Logging
```python
import structlog

logger = structlog.get_logger()

# Structured logging with context
logger.info("classification_completed",
    classifier="heuristic",
    task_type="BUG_FIX",
    confidence=0.92,
    duration_ms=5.2)
```

## Incident Response Workflows

### 1. API Error Spike Investigation

**Step 1**: Identify error spike
```
@Level = "Error" and @Timestamp > Now() - 10m
| group by time(1m), Service
| select count(*) as Errors, Service
```

**Step 2**: Find error patterns
```
@Level = "Error" and @Timestamp > Now() - 10m
| group by @MessageTemplate
| select count(*) as Count, @MessageTemplate
| sort Count desc
```

**Step 3**: Drill into specific error
```
@MessageTemplate = "Database connection timeout"
| select Service, @Exception, @Properties, @Timestamp
```

**Step 4**: Trace upstream/downstream
```
CorrelationId in ["id1", "id2", "id3"]
| select Service, @Message, @Level, @Timestamp
| sort @Timestamp asc
```

### 2. Task Execution Failure Analysis

**Step 1**: Find failed tasks
```
@MessageTemplate contains "Task execution failed"
| select TaskId, TaskType, @Exception, @Timestamp
| sort @Timestamp desc
| take 20
```

**Step 2**: Get full task execution trace
```
TaskId = "specific-task-id"
| select Service, @Message, @Level, @Timestamp
| sort @Timestamp asc
```

**Step 3**: Check related services (GitHub, Ollama)
```
(Service = "CodingAgent.Services.GitHub" or Service = "CodingAgent.Services.Ollama")
and TaskId = "specific-task-id"
```

### 3. Performance Degradation

**Step 1**: Identify slow operations
```
@Properties.ElapsedMilliseconds > 1000
| group by Service
| select count(*) as SlowRequests, avg(ElapsedMilliseconds) as AvgLatency, Service
| sort AvgLatency desc
```

**Step 2**: Find bottleneck service
```
Service = "slowest-service"
| where @Properties.ElapsedMilliseconds > 1000
| select @MessageTemplate, ElapsedMilliseconds, @Timestamp
```

**Step 3**: Check dependencies
```
Service = "slowest-service" and (@Message contains "database" or @Message contains "redis" or @Message contains "rabbitmq")
| select @Message, ElapsedMilliseconds, @Timestamp
```

## Dashboard Widgets (Seq UI)

### Recommended Widgets

1. **Error Rate by Service** (Signal: count by Service where @Level = "Error")
2. **Task Completion Rate** (Signal: count where @MessageTemplate contains "completed")
3. **Average Task Duration** (Signal: avg(Duration) where @MessageTemplate contains "completed in")
4. **Active Users** (Signal: distinct count of UserId)
5. **Top Slow Requests** (Query: ElapsedMilliseconds > 1000, sorted desc)

## Retention & Storage

**Current Configuration**:
- **Retention**: 14 days (configurable via `SEQ_DATA_RETENTION` env var)
- **Storage**: Docker volume `seq-data` at `/data`
- **Backup**: Not configured (manual export recommended for production)

**Storage Management**:
```bash
# Check storage usage
docker exec seq du -sh /data

# Export logs to JSON (last 7 days)
curl "http://localhost:5341/api/events?count=10000&fromDateUtc=$(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%SZ)" > logs-export.json
```

## Integration with Alertmanager

While Prometheus handles real-time alerting, Seq is valuable for:
- **Root cause analysis** after alerts fire
- **Historical log queries** for incident post-mortems
- **Business event tracking** (task completions, user actions)
- **Debug-level logs** not exposed as metrics

### Linking Alerts to Seq

When an alert fires, use correlation ID to find logs:
1. Get `CorrelationId` from alert labels
2. Query Seq: `CorrelationId = "value-from-alert"`
3. View full request trace across all services

## Production Considerations

### Security
- **Enable authentication**: Set `SEQ_FIRSTRUN_ADMINUSERNAME` and `SEQ_FIRSTRUN_ADMINPASSWORDHASH`
- **Use API keys**: Restrict log ingestion to authenticated services
- **TLS**: Enable HTTPS for production deployments

### Performance
- **Ingestion rate**: Monitor `seq_events_ingested_total` metric
- **Query optimization**: Use indexed properties (Service, @Level, @Timestamp)
- **Storage alerts**: Set up alerts when storage >80% full

### High Availability
- **Backup**: Schedule daily exports of critical logs
- **Replication**: Consider Seq HA license for production
- **Log shipping**: Send critical logs to long-term storage (S3, Azure Blob)

## Troubleshooting Seq

### Logs Not Appearing

**Check 1**: Verify Serilog configuration in service
```csharp
// appsettings.json
"Serilog": {
  "WriteTo": [
    {
      "Name": "Seq",
      "Args": {
        "serverUrl": "http://seq:5341"
      }
    }
  ]
}
```

**Check 2**: Test connectivity
```bash
docker exec chat-service curl -I http://seq:5341/api
```

**Check 3**: Check Seq container logs
```bash
docker logs seq
```

### Slow Queries

- Use indexed properties: `Service`, `@Level`, `@Timestamp`, `CorrelationId`, `TaskId`, `UserId`
- Add time bounds: `@Timestamp > Now() - 1h`
- Limit results: `| take 100`
- Avoid wildcard searches: Use `@MessageTemplate` instead of `@Message contains "..."`

### Storage Full

**Immediate fix**:
```bash
# Reduce retention to 7 days
docker compose -f docker-compose.yml up -d seq -e SEQ_DATA_RETENTION=7d
```

**Long-term solution**:
- Export and archive old logs
- Increase volume size
- Implement log sampling for high-volume services

## References

- **Seq Documentation**: https://docs.datalust.co/docs
- **Serilog Sinks**: https://github.com/serilog/serilog-sinks-seq
- **Query Language**: https://docs.datalust.co/docs/the-seq-query-language
- **API Reference**: https://docs.datalust.co/reference/api-overview

## Next Steps

1. **Create Seq alerts** for critical errors
2. **Set up daily log exports** for compliance/audit
3. **Build custom dashboards** for key business metrics
4. **Integrate with incident management** (PagerDuty, Opsgenie)
