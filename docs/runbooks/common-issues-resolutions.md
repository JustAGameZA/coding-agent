# Common Issues & Resolutions Runbook

This document provides solutions to common operational issues encountered in the CodingAgent microservices platform.

## Table of Contents

1. [Database Connection Issues](#database-connection-issues)
2. [RabbitMQ Connection Problems](#rabbitmq-connection-problems)
3. [JWT Authentication Failures](#jwt-authentication-failures)
4. [Redis Cache Issues](#redis-cache-issues)
5. [Service Health Check Failures](#service-health-check-failures)
6. [Validation Error Responses](#validation-error-responses)
7. [Gateway Rate Limiting](#gateway-rate-limiting)
8. [Performance Issues](#performance-issues)
9. [Migration Issues](#migration-issues)
10. [Docker Container Issues](#docker-container-issues)

---

## Database Connection Issues

### Symptom
- Services failing to start
- Health checks returning unhealthy
- Error messages: "Connection string is required" or "Connection timeout"

### Diagnosis
1. Check service logs: `docker logs <service-name>`
2. Verify connection string in `appsettings.json` or environment variables
3. Test database connectivity: `docker exec -it <postgres-container> psql -U postgres`

### Resolution

**PostgreSQL Connection String Format:**
```
Host=postgres;Port=5432;Database=<service_db>;Username=postgres;Password=postgres
```

**Common Fixes:**
- Ensure PostgreSQL container is running: `docker ps | grep postgres`
- Verify network connectivity: `docker network ls` and ensure services are on same network
- Check connection string environment variable is set correctly
- Restart PostgreSQL container if needed: `docker restart postgres`

### Prevention
- Use health checks to monitor database connectivity
- Configure connection pooling limits in `appsettings.json`
- Set up database backup and restore procedures

---

## RabbitMQ Connection Problems

### Symptom
- Event consumers not processing messages
- Error: "RabbitMQ connection refused"
- Messages accumulating in queues

### Diagnosis
1. Check RabbitMQ management UI: `http://localhost:15672` (guest/guest)
2. Verify connection string: `amqp://guest:guest@rabbitmq:5672/`
3. Check service logs for connection errors

### Resolution

**Connection String:**
```
amqp://guest:guest@rabbitmq:5672/
```

**Common Fixes:**
- Verify RabbitMQ container is running: `docker ps | grep rabbitmq`
- Check RabbitMQ health: `curl http://localhost:15672/api/healthchecks/node`
- Restart RabbitMQ if needed: `docker restart rabbitmq`
- Verify plugins are enabled: `docker exec -it rabbitmq rabbitmq-plugins list`

### Prevention
- Monitor queue depths via Prometheus/Grafana
- Set up RabbitMQ alerts for queue depth thresholds
- Use circuit breaker patterns in MassTransit configuration

---

## JWT Authentication Failures

### Symptom
- 401 Unauthorized responses
- Error: "Token validation failed"
- Gateway rejecting authenticated requests

### Diagnosis
1. Check JWT secret configuration in Gateway and services
2. Verify token expiration time
3. Check token claims in JWT debugger: `https://jwt.io`

### Resolution

**JWT Configuration:**
```json
{
  "Authentication": {
    "Jwt": {
      "SecretKey": "<secret-key-at-least-32-chars>",
      "Issuer": "CodingAgent",
      "Audience": "CodingAgent.API"
    }
  }
}
```

**Common Fixes:**
- Ensure same secret key used across Gateway and services
- Check token expiration - tokens expire after configured time
- Verify issuer and audience match configuration
- Clear browser localStorage if frontend token cache is stale

### Prevention
- Use environment variables for secrets (never commit to repo)
- Implement token refresh mechanism
- Monitor authentication failure rates in logs

---

## Redis Cache Issues

### Symptom
- Cache misses increasing
- Service startup errors about Redis
- Performance degradation

### Diagnosis
1. Check Redis container: `docker ps | grep redis`
2. Test Redis connection: `docker exec -it redis redis-cli ping`
3. Check service logs for Redis errors

### Resolution

**Redis Connection:**
```
redis:6379
```

**Common Fixes:**
- Restart Redis container: `docker restart redis`
- Clear Redis cache if corrupted: `docker exec -it redis redis-cli FLUSHALL`
- Verify Redis is on same Docker network as services
- Check Redis memory limits: `docker exec -it redis redis-cli INFO memory`

### Prevention
- Monitor Redis memory usage
- Configure Redis persistence for production
- Set appropriate TTL values for cached data

---

## Service Health Check Failures

### Symptom
- `/health` endpoints returning unhealthy
- Docker Compose reporting unhealthy containers
- Load balancer removing services from rotation

### Diagnosis
1. Check health endpoint directly: `curl http://localhost:<port>/health`
2. Review service logs for dependency failures
3. Verify all dependencies (DB, Redis, RabbitMQ) are healthy

### Resolution

**Health Check Endpoints:**
- Gateway: `http://localhost:5000/health`
- Auth Service: `http://localhost:5001/health`
- Chat Service: `http://localhost:5002/health`
- Orchestration Service: `http://localhost:5003/health`

**Common Fixes:**
- Verify database connectivity
- Check Redis/RabbitMQ connections
- Review service configuration for missing values
- Restart unhealthy service containers

### Prevention
- Configure health check intervals appropriately
- Set up alerting for health check failures
- Use health checks in deployment pipelines

---

## Validation Error Responses

### Symptom
- API requests returning 500 instead of 400 for invalid input
- Validation errors not properly formatted
- Frontend unable to parse error responses

### Diagnosis
1. Check service logs for validation exceptions
2. Verify FluentValidation validators are properly registered
3. Test with known invalid input to reproduce

### Resolution

**Expected Behavior:**
- Invalid request should return `400 Bad Request`
- Response body should contain `ValidationProblemDetails` format
- Error dictionary should map property names to error messages

**Common Fixes:**
- Ensure validators are registered: `builder.Services.AddValidatorsFromAssemblyContaining<Program>()`
- Check validator logic correctly rejects invalid input
- Verify error serialization is working correctly

---

## Gateway Rate Limiting

### Symptom
- 429 Too Many Requests responses
- API calls being throttled unexpectedly
- Rate limit headers missing

### Diagnosis
1. Check Gateway logs for rate limit events
2. Verify Redis is available (rate limiting uses Redis)
3. Review rate limit configuration in `appsettings.json`

### Resolution

**Rate Limits:**
- IP-based: 100 requests/minute
- User-based: 1000 requests/hour

**Common Fixes:**
- Wait for rate limit window to reset
- Verify Redis connectivity (rate limiting requires Redis)
- Check `X-RateLimit-Remaining` header to see remaining requests
- Adjust rate limits in configuration if needed

### Prevention
- Monitor rate limit hit rates
- Adjust limits based on usage patterns
- Implement client-side rate limiting awareness

---

## Performance Issues

### Symptom
- Slow API responses (>500ms p95)
- High database CPU usage
- Slow query execution

### Diagnosis
1. Check Prometheus metrics for p95/p99 latencies
2. Review database slow query logs
3. Analyze APM traces in Jaeger

### Resolution

**Common Performance Fixes:**
- Add database indexes for frequently queried columns
- Optimize N+1 queries (use Include() for related entities)
- Tune cache TTLs for optimal hit rates
- Scale services horizontally if needed

**Database Indexes:**
- Check `init-db.sql` for composite indexes
- Add indexes for foreign keys
- Index columns used in WHERE clauses

---

## Migration Issues

### Symptom
- Migration scripts failing
- Data inconsistency after migration
- Rollback needed

### Diagnosis
1. Check migration logs: `./deployment/docker-compose/migrate.ps1`
2. Verify migration scripts are idempotent
3. Review data verification results

### Resolution

**Migration Procedure:**
```powershell
cd deployment/docker-compose
./migrate.ps1
./verify-migration.ps1
```

**Common Fixes:**
- Ensure database backups before migration
- Verify migration scripts are idempotent (safe to re-run)
- Check for constraint violations
- Use transaction rollback if migration fails

---

## Docker Container Issues

### Symptom
- Containers not starting
- Network connectivity problems
- Volume mount issues

### Diagnosis
1. Check container status: `docker ps -a`
2. Review container logs: `docker logs <container-name>`
3. Verify Docker Compose network: `docker network inspect <network-name>`

### Resolution

**Common Fixes:**
- Restart Docker Compose: `docker-compose down && docker-compose up -d`
- Rebuild containers: `docker-compose build --no-cache`
- Check Docker resource limits
- Verify volume mounts are accessible

**Network Issues:**
- Ensure all services are on same Docker network
- Check network connectivity: `docker exec -it <container> ping <other-container>`
- Verify service discovery using container names

---

## Getting Help

### Escalation Path

1. **Check Logs**: Review service logs for error messages
2. **Check Documentation**: Review relevant runbooks and ADRs
3. **Check Metrics**: Review Prometheus/Grafana dashboards
4. **Contact Team**: Escalate to team lead if issue persists

### Useful Commands

```bash
# View all container logs
docker-compose logs -f

# Restart a specific service
docker-compose restart <service-name>

# View service health
curl http://localhost:<port>/health

# Check Redis connectivity
docker exec -it redis redis-cli ping

# Check RabbitMQ management
open http://localhost:15672
```

---

**Last Updated**: December 2025
**Maintained By**: Platform Team

