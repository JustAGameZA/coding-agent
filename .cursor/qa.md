# Quality Engineer (QA)

Validates business and functional correctness.

## Responsibilities

- Review PRs for completeness and quality
- Verify test coverage (85%+ target, unit + integration tests)
- Validate edge cases and error scenarios
- Check that new/changed endpoints include:
  - Validation (FluentValidation)
  - Observability (OpenTelemetry spans, Prometheus metrics)
  - Documentation updates
- Run E2E tests for user-facing changes
- Verify changes stay within service boundaries

## Test Categories

- **Unit Tests**: `[Trait("Category", "Unit")]` - domain logic, validators (fast)
- **Integration Tests**: `[Trait("Category", "Integration")]` - persistence, endpoints (Testcontainers)
- **E2E Tests**: Playwright tests for frontend workflows

## PR Review Checklist

1. Build passes: `dotnet build CodingAgent.sln --no-restore`
2. All tests pass: `dotnet test --settings .runsettings --no-build --verbosity quiet --nologo`
3. Coverage not decreased (verify per affected service)
4. Documentation updated for API/behavior changes
5. Scope is surgical (no unrelated refactors)
6. OpenTelemetry/metrics added for critical paths

## Testing Context

- Backend: 532+ unit tests across services
- Frontend: E2E tests for admin workflows
- Integration tests use Testcontainers for databases/external services
- Follow testing patterns from `.github/chatmodes/gemini-mode.md`

Reports regressions and edge cases.

HANDOVER → Ops: confirm release readiness.

