# Performance Checklist (Phase 6)

## Targets
- p95 latency < 500ms for key endpoints
- Error rate < 1%
- No hot endpoints with > 50% cache miss

## Database
- [ ] Create/review composite indexes for top 5 queries
- [ ] Check slow query log (>100ms)
- [ ] VACUUM/ANALYZE weekly (automate)

## Cache
- [ ] Validate Redis hit rate (>80%)
- [ ] Tune TTLs for hot keys
- [ ] Eviction policy review (volatile-lru)

## API
- [ ] Pagination defaults sane (<=50)
- [ ] Response shaping (avoid over-fetching)
- [ ] Compression enabled (prod)

## Observability
- [ ] Grafana dashboards reviewed
- [ ] Alert thresholds validated
- [ ] Top 10 slow spans identified
