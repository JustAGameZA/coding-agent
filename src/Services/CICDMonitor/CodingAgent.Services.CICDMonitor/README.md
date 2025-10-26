# CodingAgent.Services.CICDMonitor

CI/CD monitor service that polls GitHub Actions, persists build metadata, and publishes domain events.

## Configuration

Use appsettings or environment variables to configure the service. Recommended environment variables are shown below (PowerShell):

- `ConnectionStrings__CICDMonitorDb` – PostgreSQL connection string for EF Core
- `GitHub__Token` – Personal Access Token (PAT) used by Octokit client (optional for public repos)
- `RunMigrationsOnStartup` – Boolean flag to gate dev-only auto-migrations (default: `false`)

Example (PowerShell):

```powershell
$env:ConnectionStrings__CICDMonitorDb = "Host=localhost;Database=coding_agent_cicd;Username=postgres;Password=postgres"
$env:GitHub__Token = "<your-token>"
$env:RunMigrationsOnStartup = "true"  # Only for local development scenarios
```

Notes:
- Auto-migrations run only when `ASPNETCORE_ENVIRONMENT=Development` AND `RunMigrationsOnStartup=true`.
- In all other environments, apply migrations explicitly via EF tools or CI/CD.

## Design-time DbContext (EF Tools)

The design-time factory loads configuration from:
- `appsettings.json`
- `appsettings.{Environment}.json`
- Environment variables

If `ConnectionStrings:CICDMonitorDb` is missing, EF tools will throw a descriptive error. Provide the value via environment variable:

```powershell
$env:ConnectionStrings__CICDMonitorDb = "Host=localhost;Database=coding_agent_cicd;Username=postgres;Password=postgres"
```

## Observability

- OpenTelemetry tracing and metrics are enabled. Configure `OpenTelemetry:Endpoint` to export traces (OTLP). Prometheus scraping is mapped at `/metrics`.

## Health

- `/health` – standard ASP.NET Core health endpoint
- `/ping` – lightweight info endpoint
