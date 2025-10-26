# Repo Brief — coding-agent (lightweight)

Last updated: <fill on update>

Architecture
- 8-service microservices: Gateway (YARP), Chat (SignalR), Orchestration, ML Classifier (Python FastAPI), GitHub, Browser (Playwright), CI/CD Monitor, Dashboard (BFF/Angular).
- Data: PostgreSQL per service schema, Redis cache.
- Messaging: RabbitMQ + MassTransit.
- Observability: OpenTelemetry → Prometheus + Grafana + Jaeger; logs to Seq.

Key docs (read first)
- docs/00-OVERVIEW.md — system architecture & flows
- docs/01-SERVICE-CATALOG.md — APIs and domain models per service
- docs/03-SOLUTION-STRUCTURE.md — monorepo layout & placements
- docs/04-ML-AND-ORCHESTRATION-ADR.md — hybrid ML strategy
- docs/02-IMPLEMENTATION-ROADMAP.md — current phase plan
- .github/copilot-instructions.md — guardrails, prompts, crash prevention

Build
```pwsh
# solution
dotnet build CodingAgent.sln --no-restore
```

Tests (quiet)
```pwsh
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo
# unit only
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Unit"
# integration only
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Integration"
```

Crash prevention
- Always run tests with `--verbosity quiet --nologo` to prevent IDE freezes.

Common CI fixes
- Ensure workflows use quiet/nologo for dotnet test.
- If analyzers/packages fail to restore, verify `nuget.config` sources and `Directory.Build.props` central versions.

Notes
- Keep this file under 200 lines. Refresh as services evolve.
