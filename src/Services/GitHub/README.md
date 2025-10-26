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

### Webhook Operations
- **GitHub Webhook Handler**: Receive and process GitHub webhooks
- **Signature Validation**: HMAC-SHA256 validation for webhook security
- **Event Publishing**: Publish domain events to RabbitMQ for downstream services
- **Supported Events**: Push, Pull Request, Issues, Issue Comments

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
    "Token": "your-github-personal-access-token",
    "WebhookSecret": "your-webhook-secret-from-github"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
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
GitHub__WebhookSecret=your-webhook-secret
RabbitMQ__Host=localhost
RabbitMQ__Username=guest
RabbitMQ__Password=guest
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

### Webhook Endpoints

#### GitHub Webhook Handler
```http
POST /webhooks/github
Content-Type: application/json
X-Hub-Signature-256: sha256=<signature>
X-GitHub-Event: <event-type>
X-GitHub-Delivery: <delivery-id>

{
  "action": "opened",
  "repository": { ... },
  "pull_request": { ... }
}
```

**Supported Events:**
- `push` - New commits pushed to a branch
- `pull_request` - PR opened, closed, merged, etc.
- `issues` - Issue opened, closed, etc.
- `issue_comment` - Comment added to an issue

**Security:**
- Validates HMAC-SHA256 signature using `X-Hub-Signature-256` header
- Rejects requests with invalid or missing signatures
- Uses webhook secret configured in GitHub settings

**Domain Events Published:**
- `GitHubPushEvent` - When push webhook received
- `GitHubPullRequestEvent` - When PR webhook received
- `GitHubIssueEvent` - When issue/comment webhook received

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

### Webhook Setup

1. Go to your GitHub repository settings
2. Navigate to Webhooks → Add webhook
3. Set Payload URL to `https://your-domain.com/webhooks/github`
4. Set Content type to `application/json`
5. Set Secret to match your `GitHub:WebhookSecret` configuration
6. Select individual events or choose "Send me everything"
7. Ensure webhook is Active

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
│   ├── Services/           # IGitHubService, IWebhookService interfaces
│   └── Webhooks/           # Webhook payload models
├── Infrastructure/
│   ├── GitHubService.cs    # Octokit implementation
│   ├── WebhookService.cs   # Webhook processing
│   └── WebhookValidator.cs # HMAC signature validation
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
- **MassTransit**: 8.3.4 - Message bus for event publishing
- **MassTransit.RabbitMQ**: 8.3.4 - RabbitMQ transport
- **OpenTelemetry**: 1.10.0 - Observability
- **xUnit**: 2.9.2 - Testing framework
- **FluentAssertions**: 7.0.0 - Test assertions
- **Moq**: 4.20.72 - Mocking framework

## Test Coverage

The service includes comprehensive test coverage:
- **Unit Tests**: 28 tests covering domain logic and infrastructure
  - WebhookValidator (7 tests)
  - WebhookService (8 tests)
  - Repository operations (7 tests)
  - Branch operations (6 tests)
- **Integration Tests**: 10 tests covering HTTP endpoints
  - Webhook endpoints (7 tests)
  - Health/Ping endpoints (3 tests)
- **Total**: 38 tests with 85%+ code coverage

## Limitations

- No pagination support (returns all results)
- No pull request operations
- No issue management
- Basic error messages (no retry logic)

## Future Enhancements

1. Add pagination support for list operations
2. Add pull request creation/management
3. Add issue management operations
4. Implement retry logic with exponential backoff
5. Add rate limit monitoring and handling
6. Support for GitHub Apps authentication
7. Caching layer for frequently accessed data
8. Webhook event filtering and routing

## Contributing

Follow the existing patterns:
- Use minimal APIs for endpoints
- Mock Octokit clients in unit tests
- Use `[Trait("Category", "Unit")]` for unit tests
- Use `[Trait("Category", "Integration")]` for integration tests
- Maintain 85%+ code coverage

## License

MIT License
