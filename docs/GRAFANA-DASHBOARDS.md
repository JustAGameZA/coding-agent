# Grafana Dashboards Documentation

Guide for accessing and configuring Grafana dashboards for monitoring the CodingAgent platform.

## Overview

Grafana provides operational dashboards for monitoring services, infrastructure, and application metrics.

## Access

- **URL**: http://localhost:3000
- **Default Credentials**: admin/admin
- **Change password** on first login

## Pre-configured Dashboards

### 1. Service Health Dashboard

**Location**: General → Service Health

**Metrics:**
- Service health status (UP/DOWN)
- Response time (p50, p95, p99)
- Request rate (requests/second)
- Error rate (errors/second)

**Use Case**: Quick overview of all service health status

### 2. Gateway Metrics

**Location**: Services → Gateway

**Metrics:**
- Request rate by endpoint
- Response time percentiles
- Rate limit hits
- Authentication failures
- Circuit breaker status

**Use Case**: Monitor API Gateway performance and throttling

### 3. Database Performance

**Location**: Infrastructure → Database

**Metrics:**
- Connection pool usage
- Query duration (p95, p99)
- Active connections
- Database size
- Slow query count

**Use Case**: Database performance monitoring and capacity planning

### 4. RabbitMQ Metrics

**Location**: Infrastructure → RabbitMQ

**Metrics:**
- Queue depth per queue
- Message rate (published/consumed)
- Consumer utilization
- Message acknowledgment rate

**Use Case**: Monitor message queue health and backlogs

### 5. Redis Cache Metrics

**Location**: Infrastructure → Redis

**Metrics:**
- Cache hit rate
- Memory usage
- Key count
- Operations per second

**Use Case**: Cache performance optimization

### 6. Container Metrics

**Location**: Infrastructure → Containers

**Metrics:**
- CPU usage per container
- Memory usage per container
- Network I/O
- Container restart count

**Use Case**: Container resource monitoring

## Creating Custom Dashboards

### Step 1: Create Dashboard

1. Click "+" → "Create" → "Dashboard"
2. Add panels as needed

### Step 2: Add Metrics

Example PromQL queries:

**Request Rate:**
```promql
rate(http_requests_total[5m])
```

**Error Rate:**
```promql
rate(http_requests_total{status=~"5.."}[5m])
```

**Response Time (p95):**
```promql
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

**Database Connections:**
```promql
pg_stat_database_numbackends
```

**RabbitMQ Queue Depth:**
```promql
rabbitmq_queue_messages{queue="chat-messages"}
```

### Step 3: Configure Alerts

1. Edit panel → "Alert" tab
2. Define alert condition
3. Set notification channel
4. Configure alert message

### Example Alert Rules

**High Error Rate:**
```
error_rate > 0.01  # > 1% error rate
```

**Slow Response Time:**
```
p95_latency > 500ms
```

**Database Connection Exhaustion:**
```
active_connections / max_connections > 0.8
```

**RabbitMQ Queue Depth:**
```
queue_depth > 10000
```

## Notification Channels

Configure notification channels for alerts:

1. Navigate to "Alerting" → "Notification channels"
2. Add channel (Email, Slack, PagerDuty, etc.)
3. Test channel
4. Link to alert rules

### Slack Integration

1. Create Slack webhook URL
2. Add Slack notification channel in Grafana
3. Configure channel with webhook URL
4. Test notification

## Dashboard Best Practices

1. **Keep dashboards focused** - One dashboard per concern (service, infrastructure, etc.)
2. **Use variables** - Create variables for service names, environments
3. **Set refresh intervals** - Configure auto-refresh (30s for real-time, 5m for trends)
4. **Add descriptions** - Document what each panel shows
5. **Organize panels** - Group related metrics together
6. **Use thresholds** - Color-code metrics (green/yellow/red)

## Operational Dashboards

### Real-time Monitoring Dashboard

**Panels:**
- Service health status
- Request rate
- Error rate
- Response time (p95)
- Active users

**Refresh**: 30 seconds

### Capacity Planning Dashboard

**Panels:**
- CPU usage trends
- Memory usage trends
- Database growth
- Queue depth trends

**Refresh**: 5 minutes

### Troubleshooting Dashboard

**Panels:**
- Error log entries
- Failed request details
- Slow query analysis
- RabbitMQ dead letters

**Refresh**: 1 minute

## Export/Import Dashboards

### Export Dashboard

1. Open dashboard
2. Click "Dashboard settings"
3. Click "JSON" tab
4. Copy JSON or download file

### Import Dashboard

1. Click "+" → "Import"
2. Upload JSON file or paste JSON
3. Configure data sources
4. Click "Import"

## Grafana Configuration

### Data Sources

Configured data sources:

- **Prometheus**: http://prometheus:9090
- **Jaeger**: http://jaeger:16686
- **PostgreSQL**: (for database metrics)

### Adding New Data Source

1. Navigate to "Configuration" → "Data sources"
2. Click "Add data source"
3. Select type (Prometheus, PostgreSQL, etc.)
4. Configure connection details
5. Test connection
6. Save

## Troubleshooting

### Dashboard Not Loading

1. Check Grafana is running: `docker ps | grep grafana`
2. Verify Prometheus is accessible
3. Check data source connection
4. Review Grafana logs

### Missing Metrics

1. Verify metrics are exported from services
2. Check Prometheus targets: http://localhost:9090/targets
3. Verify scrape configuration in `prometheus.yml`
4. Check metric names in service code

---

**Last Updated**: December 2025
**Maintained By**: Platform Team

