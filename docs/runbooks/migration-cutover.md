# Migration & Cutover Runbook (Phase 5)

## Objectives
- Migrate data from legacy system to new microservices schemas
- Run dual-write period to validate consistency
- Gradually cut traffic to new services and decommission legacy

## Preconditions
- Staging environment available with production-like data
- Backups verified and restorable
- Observability stack healthy (Prometheus, Grafana, Jaeger, Seq)

## Data Migration
1. Export legacy data dumps (users, conversations, tasks)
2. Run SQL migration scripts against Postgres:
   ```powershell
   cd deployment/docker-compose
   ./migrate.ps1
   ```
   - Users → `auth.users` (001-migrate-users.sql)
   - Conversations/Messages → `chat.*` (002-migrate-conversations.sql)
   - Tasks/Executions → `orchestration.*` (003-migrate-tasks.sql)
3. Validate counts and samples:
   ```powershell
   ./verify-migration.ps1
   ```
   - Row counts match within tolerance
   - Spot-check 20 random entities per table

## Dual-Write Period (48–72h)
- Enable application dual-writes (feature toggles)
- Verify lag dashboards (writes/errors)
- Reconcile drift every 6h

## Traffic Cutover
1. Enable feature flags in Gateway:
   - `Features.UseLegacyChat=false`
   - `Features.UseLegacyOrchestration=false`
2. Route 10% → 50% → 100% traffic (monitor p95 latency, error rate)

## Verification Checklist
Run verification script:
```powershell
cd deployment/docker-compose
./cutover-verify.ps1
```

Manual checks:
- Health checks 200 across services
- p95 latency < 500ms for key endpoints (see `docs/PERFORMANCE-CHECKLIST.md`)
- Error rate < 1%
- No growing RabbitMQ backlogs
- Gateway rate limits respected (see `docs/GATEWAY-RATE-LIMIT-TIMEOUTS.md`)

## Rollback Plan
- Flip feature flags back to legacy
- Restore backups if data corruption is detected
- Capture diagnostics and open incident with timeline

## Post-Cutover
- Disable dual-writes
- Archive legacy artifacts
- Update documentation and runbooks


