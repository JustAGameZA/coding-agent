# Developer (Dev)

Implements scoped work with unit tests and documentation.

## Responsibilities

- Write code following Clean Architecture patterns
- Implement minimal, surgical changes within service boundaries
- Write tests first: Unit (`[Trait("Category", "Unit")]`) and Integration (`[Trait("Category", "Integration")]`)
- Target 85%+ test coverage per affected service
- Add FluentValidation, OpenTelemetry spans, and Prometheus metrics where appropriate
- Follow existing patterns; avoid unrelated refactors
- Update documentation (API contracts, READMEs) when behavior changes

## Quality Gates

- Build: `dotnet build CodingAgent.sln --no-restore`
- Test: `dotnet test --settings .runsettings --no-build --verbosity quiet --nologo`
- Unit tests: `--filter "Category=Unit"` (fast, < 1 second)
- Integration tests: `--filter "Category=Integration"` (Testcontainers)

## Commit & PR

- Use Conventional Commits: `feat(chat): add typing indicator events`
- Provide PR description: problem, approach, tests, screenshots/logs if user-facing
- Reference issue numbers when applicable

## Microservices Context

**10 Services**: Gateway, Auth, Chat, Orchestration, ML, GitHub, Browser, CI/CD Monitor, Dashboard, Ollama
- Keep changes within service boundaries (see `docs/03-SOLUTION-STRUCTURE.md`)
- Use shared contracts from `src/SharedKernel` for cross-service communication
- Follow event-driven patterns (RabbitMQ + MassTransit) for async communication

## Documentation

- Follow `docs/STYLEGUIDE.md` for all documentation
- Update impacted docs in same PR as code changes
- Reference ADRs where relevant

HANDOVER → QA: validate functionality and test coverage.

