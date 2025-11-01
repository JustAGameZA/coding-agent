# Multi-Agent Workflow Templates

This document defines standard workflows and handoff templates for the multi-agent team mode.

## Workflow: Feature Development

### 1. PM → BA: Requirements Clarification
**Template:**
```
Feature: [Feature Name]
Goal: [Business objective]
Scope: [Service(s) affected]
Priority: [High/Medium/Low]
Acceptance Criteria: [Initial draft]
Questions: [Open questions for BA]
```

### 2. BA → TechLead: Requirements Handoff
**Template:**
```
Feature: [Feature Name]
Requirements: [Link to requirements doc]
Service Scope: [Service(s)]
API Requirements: [Endpoints needed]
Events: [Event-driven integrations, if any]
Data Model: [Entity/DTO changes]
Security: [Authorization/authentication needs]
Edge Cases: [Known edge cases]
```

### 3. TechLead → Dev: Architecture Handoff
**Template:**
```
Feature: [Feature Name]
Service: [Service name]
Architecture: [Layer structure, design patterns]
API Contract: [Link to OpenAPI spec or endpoint definition]
Integration Points: [Synchronous REST / Async events]
Shared Contracts: [Any SharedKernel changes]
Observability: [Required spans/metrics]
Implementation Notes: [Key patterns to follow]
```

### 4. Dev → QA: Implementation Complete
**Template:**
```
Feature: [Feature Name]
Service: [Service name]
Changes: [Summary of changes]
Tests: [Unit: X, Integration: Y]
Coverage: [Coverage percentage per service]
Documentation: [Docs updated]
Ready for Review: [PR link]
```

### 5. QA → Ops: Release Readiness
**Template:**
```
Feature: [Feature Name]
Status: [PASS/FAIL with blockers]
Test Results: [Unit: X/Y, Integration: Y/Z, E2E: Z/W]
Coverage: [Coverage status]
Documentation: [Complete/Incomplete]
Deployment Notes: [Any special considerations]
```

### 6. Ops → PM: Deployment Summary
**Template:**
```
Feature: [Feature Name]
Deployed: [Service(s), version/commit]
Environment: [Dev/Staging/Prod]
Status: [Success/Failed]
Metrics: [Latency: Xms p95, Error Rate: Y%]
Issues: [Any alerts or problems]
Rollback: [Plan if needed]
```

## Workflow: Bug Fix

### Dev → QA: Bug Fix
```
Bug: [Issue description]
Root Cause: [Analysis]
Fix: [Solution implemented]
Tests: [Regression tests added]
Affected Services: [Service(s)]
Deployment Priority: [Hotfix/Normal]
```

## Workflow: Architecture Decision

### TechLead → Team: ADR Review
```
Decision: [What decision is being made]
Context: [Why this decision is needed]
Options: [Alternatives considered]
Decision: [Chosen approach]
Consequences: [Impact on services/team]
Documentation: [ADR file location]
```

## Escalation Paths

### When to Escalate

**Dev → TechLead:**
- Architectural questions outside scope
- Cross-service integration issues
- Performance concerns requiring design changes

**QA → TechLead:**
- Test failures indicating architectural issues
- Coverage gaps requiring design changes

**TechLead → PM:**
- Scope changes requiring timeline adjustment
- Resource constraints blocking progress

**Ops → PM:**
- Production incidents requiring prioritization
- Infrastructure capacity issues

## Quality Gates

### Code Review
- ✅ Build passes
- ✅ Tests pass (unit + integration)
- ✅ Coverage ≥ 85% (per affected service)
- ✅ Documentation updated
- ✅ No architectural violations
- ✅ Observability instrumentation added

### Deployment
- ✅ All tests pass
- ✅ Docker builds successfully
- ✅ Health checks pass
- ✅ Metrics collection working
- ✅ No critical alerts

