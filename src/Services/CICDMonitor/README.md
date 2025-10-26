# CI/CD Monitor Service

The CI/CD Monitor service is responsible for monitoring GitHub Actions workflows, detecting build failures, and publishing events for automated remediation.

## Features

- **Automatic Build Monitoring**: Polls GitHub Actions API every 60 seconds for configured repositories
- **Failure Detection**: Identifies failed, cancelled, and successful builds
- **Error Log Parsing**: Extracts error messages from workflow job steps
- **Event Publishing**: Publishes `BuildFailedEvent` to RabbitMQ for downstream processing
- **Build History**: Stores last 100 builds per repository in PostgreSQL
- **Rate Limiting**: Respects GitHub API limits with 1 request/second throttling
- **Observability**: Full OpenTelemetry instrumentation with traces and metrics

## Architecture

### Components

1. **BuildMonitor** (Background Service)
   - Polls configured repositories every minute
   - Detects new builds and status changes
   - Triggers failure handling on build failures

2. **GitHubActionsClient**
   - Interfaces with GitHub Actions API via Octokit
   - Fetches workflow runs and job logs
   - Parses error messages from failed steps

3. **BuildRepository**
   - PostgreSQL persistence layer
   - Manages build history with retention limits
   - Supports queries by repository and workflow run ID

4. **Build Entity**
   - Domain model representing a CI/CD build
   - Tracks status, metadata, and error messages

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "CICDMonitorDb": "Host=localhost;Database=coding_agent_cicd;Username=postgres;Password=postgres"
  },
  "GitHub": {
    "Token": "github_pat_YOUR_TOKEN_HERE"
  },
  "BuildMonitor": {
    "MonitoredRepositories": [
      {
        "Owner": "your-org",
        "Repository": "your-repo"
      },
      {
        "Owner": "your-org",
        "Repository": "another-repo"
      }
    ]
  },
  "OpenTelemetry": {
    "Endpoint": "http://jaeger:4317"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Environment Variables

- `ConnectionStrings__CICDMonitorDb`: PostgreSQL connection string
- `GitHub__Token`: GitHub personal access token with `repo` scope
- `RabbitMQ__Host`: RabbitMQ host
- `OpenTelemetry__Endpoint`: OTLP exporter endpoint

## API Endpoints

### GET /builds
Lists all recent builds across monitored repositories.

**Query Parameters:**
- `limit` (int, default: 100): Maximum number of builds to return

**Response:**
```json
[
  {
    "id": "guid",
    "workflowRunId": 12345,
    "owner": "test-owner",
    "repository": "test-repo",
    "branch": "main",
    "commitSha": "abc123",
    "workflowName": "CI",
    "status": "Failure",
    "conclusion": "failure",
    "workflowUrl": "https://github.com/test-owner/test-repo/actions/runs/12345",
    "errorMessages": [
      "Step 'Build' failed in job 'build-job'"
    ],
    "createdAt": "2025-01-01T00:00:00Z",
    "updatedAt": "2025-01-01T00:05:00Z",
    "startedAt": "2025-01-01T00:01:00Z",
    "completedAt": "2025-01-01T00:05:00Z"
  }
]
```

### GET /builds/{id}
Gets details for a specific build.

**Response:** Single build object (same structure as above)

### GET /health
Health check endpoint.

### GET /ping
Simple ping endpoint for service availability.

## Events

### BuildFailedEvent
Published to RabbitMQ when a build fails.

**Properties:**
- `BuildId`: Unique identifier for the build
- `Owner`: Repository owner
- `Repository`: Repository name
- `Branch`: Branch name
- `CommitSha`: Commit SHA
- `WorkflowRunId`: GitHub workflow run ID
- `WorkflowName`: Workflow name
- `ErrorMessages`: List of parsed error messages
- `Conclusion`: Build conclusion (e.g., "failure", "cancelled")
- `WorkflowUrl`: URL to the workflow run on GitHub
- `FailedAt`: Timestamp of failure

## Database Schema

### Builds Table

| Column | Type | Description |
|--------|------|-------------|
| Id | UUID | Primary key |
| WorkflowRunId | bigint | GitHub workflow run ID (unique) |
| Owner | varchar(100) | Repository owner |
| Repository | varchar(100) | Repository name |
| Branch | varchar(200) | Branch name |
| CommitSha | varchar(40) | Commit SHA |
| WorkflowName | varchar(200) | Workflow name |
| Status | int | Build status enum |
| Conclusion | varchar(50) | Build conclusion |
| WorkflowUrl | varchar(500) | GitHub URL |
| ErrorMessages | varchar(4000) | Delimited error messages |
| CreatedAt | timestamp | Creation timestamp |
| UpdatedAt | timestamp | Last update timestamp |
| StartedAt | timestamp | Start timestamp |
| CompletedAt | timestamp | Completion timestamp |

**Indexes:**
- Unique index on `WorkflowRunId`
- Composite index on `(Owner, Repository, CreatedAt)`

## Development

### Prerequisites
- .NET 9 SDK
- PostgreSQL 14+
- RabbitMQ (optional for local development)
- GitHub Personal Access Token

### Running Locally

1. Update `appsettings.json` with your configuration
2. Run migrations:
   ```bash
   dotnet ef database update
   ```
3. Start the service:
   ```bash
   dotnet run
   ```

### Running Tests

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# All tests
dotnet test
```

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.CICDMonitor.dll"]
```

### Kubernetes

See `deployment/kubernetes/cicd-monitor/` for Helm charts.

## Observability

The service exports:
- **Traces**: OTLP to Jaeger
- **Metrics**: Prometheus endpoint at `/metrics`
- **Logs**: Structured JSON logs

Key metrics:
- `cicd_monitor_polls_total`: Total number of polling operations
- `cicd_monitor_builds_detected_total`: Total builds detected
- `cicd_monitor_failures_detected_total`: Total failures detected

## Troubleshooting

### No builds being detected
- Verify GitHub token has `repo` scope
- Check that repositories are correctly configured
- Review logs for API errors

### Rate limiting errors
- Increase delay between polls (default: 60 seconds)
- Check GitHub rate limit status: `curl -H "Authorization: token YOUR_TOKEN" https://api.github.com/rate_limit`

### Database connection errors
- Verify PostgreSQL is running and accessible
- Check connection string in configuration
- Run migrations: `dotnet ef database update`

## License

See the LICENSE file in the repository root.
