# GitHub Service - Octokit Integration

## Overview

The GitHub Service provides a REST API for interacting with GitHub repositories and branches using the Octokit.NET library. It supports OAuth authentication and includes comprehensive error handling and observability.

## Features

### Repository Operations
- **Create Repository**: Create new GitHub repositories
- **List Repositories**: Get all repositories for authenticated user
- **Get Repository**: Retrieve details of a specific repository
- **Update Repository**: Modify repository settings (e.g., description)
- **Delete Repository**: Remove a repository

### Branch Operations
- **Create Branch**: Create a new branch from an existing branch
- **List Branches**: Get all branches in a repository
- **Get Branch**: Retrieve details of a specific branch
- **Delete Branch**: Remove a branch

### Additional Features
- OAuth token authentication
- Rate limiting error handling
- OpenTelemetry instrumentation
- Health checks
- Prometheus metrics

## Configuration

### appsettings.json

```json
{
  "GitHub": {
    "Token": "your-github-personal-access-token"
  },
  "OpenTelemetry": {
    "ServiceName": "CodingAgent.Services.GitHub",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

### Environment Variables

```bash
GitHub__Token=your-github-personal-access-token
```

## API Endpoints

### Repository Endpoints

#### Create Repository
```http
POST /repositories
Content-Type: application/json

{
  "name": "my-repo",
  "description": "My repository description",
  "isPrivate": false
}
```

#### List Repositories
```http
GET /repositories
```

#### Get Repository
```http
GET /repositories/{owner}/{name}
```

#### Update Repository
```http
PUT /repositories/{owner}/{name}
Content-Type: application/json

{
  "description": "Updated description"
}
```

#### Delete Repository
```http
DELETE /repositories/{owner}/{name}
```

### Branch Endpoints

#### Create Branch
```http
POST /repositories/{owner}/{repo}/branches
Content-Type: application/json

{
  "branchName": "feature-branch",
  "sourceBranch": "main"
}
```

#### List Branches
```http
GET /repositories/{owner}/{repo}/branches
```

#### Get Branch
```http
GET /repositories/{owner}/{repo}/branches/{branchName}
```

#### Delete Branch
```http
DELETE /repositories/{owner}/{repo}/branches/{branchName}
```

### Health & Monitoring

#### Health Check
```http
GET /health
```

#### Ping
```http
GET /ping
```

#### Metrics
```http
GET /metrics
```

## Authentication

The service uses GitHub Personal Access Tokens (PAT) for authentication. Configure the token in `appsettings.json` or via environment variables.

### Required Scopes

For full functionality, your token should have these scopes:
- `repo` - Full control of private repositories
- `delete_repo` - Delete repositories (for delete operations)

## Running the Service

### Local Development

```bash
cd src/Services/GitHub/CodingAgent.Services.GitHub
dotnet run
```

The service will start on `http://localhost:5000`

### Docker

```bash
docker build -t github-service -f src/Services/GitHub/CodingAgent.Services.GitHub/Dockerfile .
docker run -p 5000:8080 -e GitHub__Token=your-token github-service
```

## Testing

### Run All Tests

```bash
cd src/Services/GitHub
dotnet test
```

### Run Unit Tests Only

```bash
dotnet test --filter "Category=Unit"
```

### Run Integration Tests Only

```bash
dotnet test --filter "Category=Integration"
```

### Run with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture

The service follows Clean Architecture principles:

```
CodingAgent.Services.GitHub/
├── Domain/
│   ├── Entities/           # Repository, Branch entities
│   └── Services/           # IGitHubService interface
├── Infrastructure/
│   └── GitHubService.cs    # Octokit implementation
└── Api/
    └── Endpoints/          # Minimal API endpoints
```

## Error Handling

All Octokit exceptions are wrapped in `InvalidOperationException` with descriptive messages:

- `Repository not found: owner/repo` - 404 errors
- `Failed to create repository: <details>` - API errors
- `Failed to delete branch: <details>` - Operation errors

## Observability

### Logging

The service uses structured logging with Microsoft.Extensions.Logging:

```csharp
_logger.LogInformation("Creating repository: {Name}", name);
_logger.LogError(ex, "Failed to create repository: {Name}", name);
```

### Metrics

Metrics are exposed via Prometheus at `/metrics`:
- HTTP request duration
- HTTP request count
- API call success/failure rates

### Tracing

Distributed tracing via OpenTelemetry with OTLP exporter to Jaeger/Zipkin.

## Dependencies

- **Octokit**: 13.0.1 - GitHub API client
- **OpenTelemetry**: 1.10.0 - Observability
- **xUnit**: 2.9.2 - Testing framework
- **FluentAssertions**: 7.0.0 - Test assertions
- **Moq**: 4.20.72 - Mocking framework

## Limitations

- No pagination support (returns all results)
- No webhook handling
- No pull request operations
- No issue management
- Basic error messages (no retry logic)

## Future Enhancements

1. Add pagination support for list operations
2. Implement webhook handling for GitHub events
3. Add pull request creation/management
4. Add issue management operations
5. Implement retry logic with exponential backoff
6. Add rate limit monitoring and handling
7. Support for GitHub Apps authentication
8. Caching layer for frequently accessed data

## Contributing

Follow the existing patterns:
- Use minimal APIs for endpoints
- Mock Octokit clients in unit tests
- Use `[Trait("Category", "Unit")]` for unit tests
- Use `[Trait("Category", "Integration")]` for integration tests
- Maintain 85%+ code coverage

## License

MIT License
