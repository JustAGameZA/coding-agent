# Lessons Learned

Documentation of key lessons learned during the CodingAgent microservices rewrite project.

## Project Overview

**Duration**: 6 months (Phases 0-6)
**Team Size**: Distributed development team
**Technology Stack**: .NET 9, Python FastAPI, Angular, Docker, Kubernetes

---

## Technical Lessons

### 1. Shared Kernel Design

**Lesson**: Creating a `SharedKernel` early prevented code duplication across services.

**Impact**: Eliminated 112 lines of duplicate code in Week 4.

**Best Practice**: 
- Identify common patterns early
- Extract shared code into reusable libraries
- Keep shared kernel minimal (only truly common code)

### 2. Integration Testing with Testcontainers

**Lesson**: Using Testcontainers for integration tests provides realistic testing without manual setup.

**Impact**: 
- Tests run consistently across environments
- No manual database setup required
- Tests closer to production environment

**Best Practice**:
- Use Testcontainers for all integration tests
- Handle Docker unavailability gracefully (skip tests if needed)
- Clean up containers after tests

### 3. FluentValidation in Minimal APIs

**Lesson**: FluentValidation integration with Minimal APIs requires manual error dictionary conversion.

**Challenge**: `ToDictionary()` extension method not always available or reliable.

**Solution**: Manually group and convert validation errors:
```csharp
var errors = validationResult.Errors
    .GroupBy(e => e.PropertyName ?? "General")
    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Validation error").ToArray());
```

**Best Practice**: Always handle validation errors explicitly in Minimal APIs.

### 4. JWT Authentication Across Services

**Lesson**: Consistent JWT configuration across Gateway and services is critical.

**Challenge**: Different services expected different configuration paths (`Jwt:*` vs `Authentication:Jwt:*`).

**Solution**: Support both paths, prefer `Authentication:Jwt:*` for consistency with Gateway.

**Best Practice**:
- Use consistent configuration paths
- Support legacy paths during migration
- Document JWT configuration clearly

### 5. Database Migration Strategy

**Lesson**: Idempotent migration scripts enable safe re-runs and rollbacks.

**Impact**: 
- Migration scripts can be safely re-executed
- Easier to test migration procedures
- Reduced risk of partial migrations

**Best Practice**:
- Always use `INSERT ... ON CONFLICT DO NOTHING` or similar
- Include verification scripts
- Test migrations on staging first

### 6. Feature Flags for Traffic Routing

**Lesson**: Feature flags enable gradual traffic cutover with minimal risk.

**Impact**:
- Zero-downtime deployments
- Easy rollback if issues detected
- Gradual rollout (10% → 50% → 100%)

**Best Practice**:
- Use feature flags for all major changes
- Monitor error rates during rollout
- Keep rollback plan ready

---

## Process Lessons

### 1. Documentation-Driven Development

**Lesson**: Creating documentation first (ADRs, service catalogs) clarified requirements.

**Impact**:
- Reduced ambiguity during implementation
- Faster onboarding for new team members
- Clearer acceptance criteria

**Best Practice**:
- Document architecture decisions (ADRs)
- Create service specifications before coding
- Keep documentation updated during implementation

### 2. Incremental Delivery

**Lesson**: Delivering features incrementally (Phase 0 → Phase 6) maintained momentum.

**Impact**:
- Early validation of approach
- Regular feedback cycles
- Ability to adjust based on learnings

**Best Practice**:
- Break work into small, deliverable increments
- Deliver working code regularly
- Gather feedback early and often

### 3. Test-First Approach

**Lesson**: Writing tests alongside code (or first) improved quality.

**Impact**:
- Higher test coverage
- Fewer bugs in production
- Confidence in refactoring

**Best Practice**:
- Write tests for all endpoints
- Use integration tests for critical paths
- Maintain high test coverage (>85%)

### 4. Observability from Day One

**Lesson**: Setting up observability (Prometheus, Grafana, Jaeger) early helped debugging.

**Impact**:
- Faster issue resolution
- Better performance insights
- Proactive monitoring

**Best Practice**:
- Set up observability infrastructure early
- Use structured logging (Serilog)
- Export metrics and traces from all services

---

## Architecture Lessons

### 1. Event-Driven Communication

**Lesson**: Using RabbitMQ for async communication improved decoupling.

**Impact**:
- Services can evolve independently
- Better scalability
- Resilience to service failures

**Best Practice**:
- Use events for async operations
- Keep event contracts stable
- Handle event failures gracefully

### 2. API Gateway Pattern

**Lesson**: Centralized API Gateway simplified client integration and provided single point for cross-cutting concerns.

**Impact**:
- Single entry point for clients
- Centralized authentication
- Rate limiting at edge

**Best Practice**:
- Use Gateway for cross-cutting concerns
- Keep service APIs focused
- Route based on feature flags

### 3. Microservice Boundaries

**Lesson**: Clear service boundaries (by domain) made services easier to reason about.

**Impact**:
- Services aligned with business domains
- Easier to understand and modify
- Better team ownership

**Best Practice**:
- Align services with business domains
- Keep services focused (single responsibility)
- Minimize inter-service dependencies

---

## What Worked Well

1. ✅ **Monorepo Structure**: Single repository for all services simplified development
2. ✅ **Docker Compose for Dev**: Easy local development setup
3. ✅ **OpenAPI Specifications**: Clear API contracts
4. ✅ **Health Checks**: Quick health verification
5. ✅ **Testcontainers**: Realistic integration tests

## What Could Be Improved

1. ⚠️ **Early Performance Testing**: Should have load tested earlier
2. ⚠️ **Chat Validation Tests**: Earlier investigation of validation error handling
3. ⚠️ **Cache Strategy**: Could have defined cache strategy earlier
4. ⚠️ **Migration Scripts**: Could have started migration scripts earlier in Phase 5

## Recommendations for Future Projects

1. **Start with Observability**: Set up monitoring from day one
2. **Document Early**: Create ADRs and specs before implementation
3. **Test Integration Early**: Set up integration test infrastructure early
4. **Plan Migration Early**: Start migration planning in previous phase
5. **Performance Test Early**: Include performance testing in Phase 4

---

## Key Metrics

- **Phases Completed**: 6 (Phase 0-5)
- **Services Implemented**: 8 microservices
- **Test Coverage**: >85% average
- **Documentation**: 50+ documents
- **ADRs**: 3 complete
- **Time to First Deployment**: Phase 1 (4 weeks)

---

**Last Updated**: December 2025
**Documented By**: Development Team

