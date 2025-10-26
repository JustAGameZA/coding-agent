# Solution Structure - Monorepo Organization

**Version**: 2.0.0
**Last Updated**: October 24, 2025
**Repository**: Monorepo with service boundaries

---

## Directory Structure

```
coding-agent-v2/                          # Root monorepo
├── .github/
│   └── workflows/
│       ├── gateway.yml                   # CI/CD for Gateway
│       ├── chat-service.yml              # CI/CD for Chat Service
│       ├── orchestration-service.yml     # CI/CD for Orchestration
│       ├── ml-classifier.yml             # CI/CD for ML Classifier
│       ├── github-service.yml            # CI/CD for GitHub Service
│       ├── browser-service.yml           # CI/CD for Browser Service
│       ├── cicd-monitor.yml              # CI/CD for CI/CD Monitor
│       ├── dashboard-service.yml         # CI/CD for Dashboard Service
│       ├── frontend.yml                  # CI/CD for Angular Frontend
│       └── e2e-tests.yml                 # E2E test suite
│
├── src/
│   ├── SharedKernel/                     # Shared contracts (NuGet package)
│   │   ├── CodingAgent.SharedKernel/
│   │   │   ├── Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   └── BaseEntity.cs    # Base entity with Id, timestamps
│   │   │   │   ├── ValueObjects/
│   │   │   │   │   ├── TaskType.cs      # Shared enum
│   │   │   │   │   ├── TaskComplexity.cs
│   │   │   │   │   └── ExecutionStrategy.cs
│   │   │   │   └── Events/
│   │   │   │       ├── IDomainEvent.cs  # Event interface
│   │   │   │       ├── TaskCreatedEvent.cs
│   │   │   │       ├── TaskCompletedEvent.cs
│   │   │   │       └── MessageSentEvent.cs
│   │   │   ├── Contracts/
│   │   │   │   ├── Requests/
│   │   │   │   │   ├── CreateTaskRequest.cs
│   │   │   │   │   └── SendMessageRequest.cs
│   │   │   │   ├── Responses/
│   │   │   │   │   ├── TaskResponse.cs
│   │   │   │   │   └── MessageResponse.cs
│   │   │   │   └── DTOs/
│   │   │   │       ├── TaskDto.cs
│   │   │   │       └── ConversationDto.cs
│   │   │   ├── Abstractions/
│   │   │   │   ├── IRepository.cs       # Generic repository
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   └── IEventPublisher.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── DomainException.cs
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   └── ValidationException.cs
│   │   │   └── CodingAgent.SharedKernel.csproj
│   │   └── CodingAgent.SharedKernel.Tests/
│   │       └── ValueObjects/
│   │           └── TaskTypeTests.cs
│   │
│   ├── Gateway/                          # API Gateway (YARP)
│   │   ├── CodingAgent.Gateway/
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json
│   │   │   ├── Middleware/
│   │   │   │   ├── AuthenticationMiddleware.cs
│   │   │   │   ├── RateLimitingMiddleware.cs
│   │   │   │   └── CorrelationIdMiddleware.cs
│   │   │   ├── Configuration/
│   │   │   │   ├── YarpConfiguration.cs
│   │   │   │   └── AuthConfiguration.cs
│   │   │   ├── Transforms/
│   │   │   │   └── JwtPropagationTransform.cs
│   │   │   └── Dockerfile
│   │   ├── CodingAgent.Gateway.Tests/
│   │   │   ├── Integration/
│   │   │   │   ├── GatewayTests.cs
│   │   │   │   └── AuthenticationTests.cs
│   │   │   └── Unit/
│   │   │       └── RateLimitingTests.cs
│   │   └── CodingAgent.Gateway.sln
│   │
│   ├── Services/
│   │   ├── Chat/
│   │   │   ├── CodingAgent.Services.Chat/
│   │   │   │   ├── Program.cs
│   │   │   │   ├── appsettings.json
│   │   │   │   ├── Domain/
│   │   │   │   │   ├── Entities/
│   │   │   │   │   │   ├── Conversation.cs
│   │   │   │   │   │   ├── Message.cs
│   │   │   │   │   │   └── Attachment.cs
│   │   │   │   │   ├── Events/
│   │   │   │   │   │   └── MessageSentEvent.cs
│   │   │   │   │   ├── Repositories/
│   │   │   │   │   │   ├── IConversationRepository.cs
│   │   │   │   │   │   └── IMessageRepository.cs
│   │   │   │   │   └── Services/
│   │   │   │   │       ├── IConversationService.cs
│   │   │   │   │       └── ConversationService.cs
│   │   │   │   ├── Application/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── CreateConversationCommand.cs
│   │   │   │   │   │   └── SendMessageCommand.cs
│   │   │   │   │   ├── Queries/
│   │   │   │   │   │   ├── GetConversationQuery.cs
│   │   │   │   │   │   └── ListConversationsQuery.cs
│   │   │   │   │   └── Validators/
│   │   │   │   │       └── SendMessageValidator.cs
│   │   │   │   ├── Infrastructure/
│   │   │   │   │   ├── Persistence/
│   │   │   │   │   │   ├── ChatDbContext.cs
│   │   │   │   │   │   ├── ConversationRepository.cs
│   │   │   │   │   │   └── MessageRepository.cs
│   │   │   │   │   ├── Caching/
│   │   │   │   │   │   └── RedisCacheService.cs
│   │   │   │   │   └── Messaging/
│   │   │   │   │       └── EventPublisher.cs
│   │   │   │   ├── Api/
│   │   │   │   │   ├── Endpoints/
│   │   │   │   │   │   ├── ConversationEndpoints.cs
│   │   │   │   │   │   └── MessageEndpoints.cs
│   │   │   │   │   └── Hubs/
│   │   │   │   │       └── ChatHub.cs  # SignalR
│   │   │   │   └── Dockerfile
│   │   │   ├── CodingAgent.Services.Chat.Tests/
│   │   │   │   ├── Unit/
│   │   │   │   │   ├── Domain/
│   │   │   │   │   │   └── ConversationTests.cs
│   │   │   │   │   └── Application/
│   │   │   │   │       └── SendMessageCommandTests.cs
│   │   │   │   ├── Integration/
│   │   │   │   │   ├── ConversationEndpointsTests.cs
│   │   │   │   │   └── ChatHubTests.cs
│   │   │   │   └── TestFixtures/
│   │   │   │       └── ChatServiceFixture.cs
│   │   │   └── CodingAgent.Services.Chat.sln
│   │   │
│   │   ├── Orchestration/
│   │   │   ├── CodingAgent.Services.Orchestration/
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Domain/
│   │   │   │   │   ├── Entities/
│   │   │   │   │   │   ├── CodingTask.cs
│   │   │   │   │   │   ├── TaskExecution.cs
│   │   │   │   │   │   └── ExecutionResult.cs
│   │   │   │   │   ├── Strategies/
│   │   │   │   │   │   ├── IExecutionStrategy.cs
│   │   │   │   │   │   ├── SingleShotStrategy.cs
│   │   │   │   │   │   ├── IterativeStrategy.cs
│   │   │   │   │   │   └── MultiAgentStrategy.cs
│   │   │   │   │   └── Services/
│   │   │   │   │       ├── ITaskOrchestrator.cs
│   │   │   │   │       └── TaskOrchestrator.cs
│   │   │   │   ├── Application/
│   │   │   │   │   ├── Commands/
│   │   │   │   │   ├── Queries/
│   │   │   │   │   └── EventHandlers/
│   │   │   │   │       └── TaskCompletedEventHandler.cs
│   │   │   │   ├── Infrastructure/
│   │   │   │   │   ├── Persistence/
│   │   │   │   │   ├── ExternalServices/
│   │   │   │   │   │   ├── MLClassifierClient.cs
│   │   │   │   │   │   └── GitHubServiceClient.cs
│   │   │   │   │   └── Messaging/
│   │   │   │   └── Api/
│   │   │   │       └── Endpoints/
│   │   │   ├── CodingAgent.Services.Orchestration.Tests/
│   │   │   └── CodingAgent.Services.Orchestration.sln
│   │   │
│   │   ├── MLClassifier/                 # Python service
│   │   │   ├── ml_classifier_service/
│   │   │   │   ├── main.py              # FastAPI app
│   │   │   │   ├── api/
│   │   │   │   │   ├── routes/
│   │   │   │   │   │   ├── classification.py
│   │   │   │   │   │   ├── training.py
│   │   │   │   │   │   └── health.py
│   │   │   │   │   └── schemas/
│   │   │   │   │       ├── classification.py
│   │   │   │   │       └── training.py
│   │   │   │   ├── domain/
│   │   │   │   │   ├── classifiers/
│   │   │   │   │   │   ├── heuristic.py
│   │   │   │   │   │   ├── ml_classifier.py
│   │   │   │   │   │   └── hybrid.py
│   │   │   │   │   └── models/
│   │   │   │   │       ├── task_type.py
│   │   │   │   │       └── complexity.py
│   │   │   │   ├── infrastructure/
│   │   │   │   │   ├── database/
│   │   │   │   │   │   └── repository.py
│   │   │   │   │   ├── ml/
│   │   │   │   │   │   ├── feature_extractor.py
│   │   │   │   │   │   ├── model_trainer.py
│   │   │   │   │   │   └── model_loader.py
│   │   │   │   │   └── messaging/
│   │   │   │   │       └── event_consumer.py
│   │   │   │   ├── config.py
│   │   │   │   └── requirements.txt
│   │   │   ├── tests/
│   │   │   │   ├── unit/
│   │   │   │   │   ├── test_heuristic.py
│   │   │   │   │   └── test_ml_classifier.py
│   │   │   │   └── integration/
│   │   │   │       └── test_api.py
│   │   │   ├── Dockerfile
│   │   │   └── pyproject.toml
│   │   │
│   │   ├── GitHub/
│   │   │   ├── CodingAgent.Services.GitHub/
│   │   │   │   ├── Domain/
│   │   │   │   │   ├── Entities/
│   │   │   │   │   │   ├── Repository.cs
│   │   │   │   │   │   ├── PullRequest.cs
│   │   │   │   │   │   └── Issue.cs
│   │   │   │   │   └── Services/
│   │   │   │   │       ├── IGitHubService.cs
│   │   │   │   │       └── GitHubService.cs
│   │   │   │   ├── Application/
│   │   │   │   ├── Infrastructure/
│   │   │   │   │   ├── OctokitClient.cs
│   │   │   │   │   └── WebhookValidator.cs
│   │   │   │   └── Api/
│   │   │   ├── CodingAgent.Services.GitHub.Tests/
│   │   │   └── CodingAgent.Services.GitHub.sln
│   │   │
│   │   ├── Browser/
│   │   │   ├── CodingAgent.Services.Browser/
│   │   │   │   ├── Domain/
│   │   │   │   │   ├── Services/
│   │   │   │   │   │   ├── IBrowserService.cs
│   │   │   │   │   │   └── BrowserService.cs
│   │   │   │   │   └── Models/
│   │   │   │   │       ├── BrowserRequest.cs
│   │   │   │   │       └── BrowserResult.cs
│   │   │   │   ├── Infrastructure/
│   │   │   │   │   └── Playwright/
│   │   │   │   │       └── PlaywrightManager.cs
│   │   │   │   └── Api/
│   │   │   ├── CodingAgent.Services.Browser.Tests/
│   │   │   └── CodingAgent.Services.Browser.sln
│   │   │
│   │   ├── CICDMonitor/
│   │   │   ├── CodingAgent.Services.CICDMonitor/
│   │   │   │   ├── Domain/
│   │   │   │   ├── Application/
│   │   │   │   ├── Infrastructure/
│   │   │   │   └── Api/
│   │   │   ├── CodingAgent.Services.CICDMonitor.Tests/
│   │   │   └── CodingAgent.Services.CICDMonitor.sln
│   │   │
│   │   ├── Dashboard/
│   │       ├── CodingAgent.Services.Dashboard/
│   │       │   ├── Domain/
│   │       │   ├── Application/
│   │       │   │   └── Aggregators/
│   │       │   │       ├── StatsAggregator.cs
│   │       │   │       └── ActivityAggregator.cs
│   │       │   ├── Infrastructure/
│   │       │   │   └── ServiceClients/
│   │       │   │       ├── ChatServiceClient.cs
│   │       │   │       ├── OrchestrationServiceClient.cs
│   │       │   │       └── GitHubServiceClient.cs
│   │       │   └── Api/
│   │       ├── CodingAgent.Services.Dashboard.Tests/
│   │       └── CodingAgent.Services.Dashboard.sln
│   │   │
│   │   └── Ollama/
│   │       ├── CodingAgent.Services.Ollama/
│   │       │   ├── Program.cs
│   │       │   ├── appsettings.json
│   │       │   ├── Domain/
│   │       │   │   ├── Entities/
│   │       │   │   │   ├── OllamaModel.cs
│   │       │   │   │   ├── ModelMetrics.cs
│   │       │   │   │   ├── OllamaRequest.cs
│   │       │   │   │   └── OllamaResponse.cs
│   │       │   │   ├── Services/
│   │       │   │   │   ├── IModelManager.cs
│   │       │   │   │   ├── ModelManager.cs
│   │       │   │   │   ├── IInferenceRouter.cs
│   │       │   │   │   ├── InferenceRouter.cs
│   │       │   │   │   └── PromptOptimizer.cs
│   │       │   │   └── Repositories/
│   │       │   │       ├── IModelRepository.cs
│   │       │   │       └── IUsageRepository.cs
│   │       │   ├── Application/
│   │       │   │   ├── Commands/
│   │       │   │   │   ├── PullModelCommand.cs
│   │       │   │   │   └── DeleteModelCommand.cs
│   │       │   │   ├── Queries/
│   │       │   │   │   ├── ListModelsQuery.cs
│   │       │   │   │   └── GetModelMetricsQuery.cs
│   │       │   │   └── EventHandlers/
│   │       │   │       └── ModelUsageEventHandler.cs
│   │       │   ├── Infrastructure/
│   │       │   │   ├── Persistence/
│   │       │   │   │   ├── OllamaDbContext.cs
│   │       │   │   │   ├── ModelRepository.cs
│   │       │   │   │   └── UsageRepository.cs
│   │       │   │   ├── Caching/
│   │       │   │   │   └── PromptCacheService.cs
│   │       │   │   ├── Messaging/
│   │       │   │   │   └── EventPublisher.cs
│   │       │   │   └── ExternalServices/
│   │       │   │       └── OllamaHttpClient.cs
│   │       │   ├── Api/
│   │       │   │   ├── Endpoints/
│   │       │   │   │   ├── ModelEndpoints.cs
│   │       │   │   │   ├── GenerationEndpoints.cs
│   │       │   │   │   ├── InferenceEndpoints.cs
│   │       │   │   │   └── AdminEndpoints.cs
│   │       │   │   └── HealthChecks/
│   │       │   │       └── OllamaHealthCheck.cs
│   │       │   └── Dockerfile
│   │       ├── CodingAgent.Services.Ollama.Tests/
│   │       │   ├── Unit/
│   │       │   │   ├── Domain/
│   │       │   │   │   ├── InferenceRouterTests.cs
│   │       │   │   │   └── ModelManagerTests.cs
│   │       │   │   └── Application/
│   │       │   │       └── PullModelCommandTests.cs
│   │       │   ├── Integration/
│   │       │   │   ├── GenerationEndpointsTests.cs
│   │       │   │   └── ModelEndpointsTests.cs
│   │       │   └── TestFixtures/
│   │       │       └── OllamaServiceFixture.cs
│   │       └── CodingAgent.Services.Ollama.sln
│   │
│   └── Frontend/
│       ├── coding-agent-dashboard/       # Angular 20.3
│       │   ├── src/
│       │   │   ├── app/
│       │   │   │   ├── core/
│       │   │   │   │   ├── services/
│       │   │   │   │   │   ├── api.service.ts
│       │   │   │   │   │   ├── auth.service.ts
│       │   │   │   │   │   └── signalr.service.ts
│       │   │   │   │   └── guards/
│       │   │   │   │       └── auth.guard.ts
│       │   │   │   ├── features/
│       │   │   │   │   ├── chat/
│       │   │   │   │   │   ├── chat.component.ts
│       │   │   │   │   │   ├── chat.component.html
│       │   │   │   │   │   └── chat.store.ts
│       │   │   │   │   ├── tasks/
│       │   │   │   │   │   ├── task-list.component.ts
│       │   │   │   │   │   └── task-detail.component.ts
│       │   │   │   │   └── dashboard/
│       │   │   │   │       └── dashboard.component.ts
│       │   │   │   ├── shared/
│       │   │   │   │   ├── components/
│       │   │   │   │   ├── pipes/
│       │   │   │   │   └── directives/
│       │   │   │   └── app.component.ts
│       │   │   ├── environments/
│       │   │   │   ├── environment.ts
│       │   │   │   └── environment.prod.ts
│       │   │   └── main.ts
│       │   ├── angular.json
│       │   ├── package.json
│       │   └── tsconfig.json
│       └── e2e/                          # Cypress E2E tests
│           ├── cypress/
│           │   ├── e2e/
│           │   │   ├── chat.cy.ts
│           │   │   ├── tasks.cy.ts
│           │   │   └── authentication.cy.ts
│           │   └── support/
│           └── cypress.config.ts
│
├── deployment/
│   ├── docker-compose/
│   │   ├── docker-compose.dev.yml
│   │   ├── docker-compose.prod.yml
│   │   └── docker-compose.override.yml
│   ├── kubernetes/
│   │   ├── base/
│   │   │   ├── namespace.yaml
│   │   │   ├── configmap.yaml
│   │   │   └── secrets.yaml
│   │   ├── services/
│   │   │   ├── gateway/
│   │   │   │   ├── deployment.yaml
│   │   │   │   ├── service.yaml
│   │   │   │   └── hpa.yaml
│   │   │   ├── chat/
│   │   │   ├── orchestration/
│   │   │   └── ... (one per service)
│   │   └── infrastructure/
│   │       ├── postgresql.yaml
│   │       ├── redis.yaml
│   │       └── rabbitmq.yaml
│   └── helm/
│       └── coding-agent/
│           ├── Chart.yaml
│           ├── values.yaml
│           └── templates/
│
├── docs/
│   ├── architecture/
│   │   ├── microservices-rewrite/
│   │   │   ├── 00-OVERVIEW.md
│   │   │   ├── 01-SERVICE-CATALOG.md
│   │   │   ├── 02-IMPLEMENTATION-ROADMAP.md
│   │   │   ├── 03-SOLUTION-STRUCTURE.md
│   │   │   └── ADRs/
│   │   │       ├── 001-microservices-architecture.md
│   │   │       ├── 002-api-gateway-yarp.md
│   │   │       ├── 003-event-driven-messaging.md
│   │   │       ├── 004-postgresql-schemas.md
│   │   │       └── 005-observability-stack.md
│   │   └── diagrams/
│   │       ├── system-context.puml
│   │       ├── service-map.puml
│   │       └── deployment.puml
│   ├── api/
│   │   ├── gateway-openapi.yaml
│   │   ├── chat-service-openapi.yaml
│   │   ├── orchestration-service-openapi.yaml
│   │   └── ... (one per service)
│   ├── runbooks/
│   │   ├── incident-response.md
│   │   ├── deployment.md
│   │   ├── database-migration.md
│   │   └── rollback-procedure.md
│   └── guides/
│       ├── developer-onboarding.md
│       ├── local-development.md
│       └── production-deployment.md
│
├── scripts/
│   ├── setup-dev-environment.ps1
│   ├── build-all-services.ps1
│   ├── run-integration-tests.ps1
│   └── migrate-database.ps1
│
├── tests/
│   ├── E2E/
│   │   ├── CodingAgent.E2E.Tests/
│   │   │   ├── Scenarios/
│   │   │   │   ├── ChatScenarioTests.cs
│   │   │   │   ├── TaskExecutionScenarioTests.cs
│   │   │   │   └── GitHubIntegrationScenarioTests.cs
│   │   │   ├── Fixtures/
│   │   │   │   └── DockerComposeFixture.cs
│   │   │   └── CodingAgent.E2E.Tests.csproj
│   │   └── run-e2e-tests.sh
│   ├── Load/
│   │   ├── k6/
│   │   │   ├── chat-load-test.js
│   │   │   ├── orchestration-load-test.js
│   │   │   └── gateway-load-test.js
│   │   └── run-load-tests.sh
│   └── Contract/
│       ├── pacts/
│       │   ├── orchestration-ml-classifier.json
│       │   └── orchestration-github.json
│       └── verify-contracts.sh
│
├── .editorconfig
├── .gitignore
├── Directory.Build.props              # Common .NET properties
├── global.json                         # .NET SDK version
├── nuget.config
├── README.md
└── LICENSE
```

---

## Service Solutions

Each service has its own `.sln` file for independent building:

```bash
# Build specific service
dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln

# Build all .NET services
dotnet build src/Gateway/CodingAgent.Gateway.sln
dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln
dotnet build src/Services/Orchestration/CodingAgent.Services.Orchestration.sln
# ... etc.
```

---

## SharedKernel as NuGet Package

The `SharedKernel` is published as a **private NuGet package** consumed by all services:

```xml
<!-- In each service's .csproj -->
<ItemGroup>
  <PackageReference Include="CodingAgent.SharedKernel" Version="2.0.0" />
</ItemGroup>
```

**Version Management**:
- Semantic versioning (2.0.0, 2.1.0, 3.0.0)
- Breaking changes = major bump
- New features = minor bump
- Bug fixes = patch bump

**Publishing**:
```bash
# Pack SharedKernel
cd src/SharedKernel/CodingAgent.SharedKernel
dotnet pack -c Release -o nupkgs/

# Push to private NuGet feed (GitHub Packages or Azure Artifacts)
dotnet nuget push nupkgs/CodingAgent.SharedKernel.2.0.0.nupkg \
  --source "https://nuget.pkg.github.com/JustAGameZA/index.json" \
  --api-key $GITHUB_TOKEN
```

---

## CI/CD Pipeline Structure

Each service has an **independent GitHub Actions workflow**:

```yaml
# .github/workflows/chat-service.yml
name: Chat Service CI/CD

on:
  push:
    paths:
      - 'src/Services/Chat/**'
      - 'src/SharedKernel/**'
      - '.github/workflows/chat-service.yml'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore
        run: dotnet restore src/Services/Chat/CodingAgent.Services.Chat.sln

      - name: Build
        run: dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln --no-restore

      - name: Test
        run: dotnet test src/Services/Chat/CodingAgent.Services.Chat.sln --no-build --verbosity quiet --nologo --collect:"XPlat Code Coverage"

      - name: Check Coverage
        run: |
          coverage=$(grep -oP 'Line coverage: \K[\d.]+' coverage.txt)
          if (( $(echo "$coverage < 85" | bc -l) )); then
            echo "Coverage $coverage% is below 85%"
            exit 1
          fi

      - name: Docker Build
        run: docker build -f src/Services/Chat/CodingAgent.Services.Chat/Dockerfile -t chat-service:${{ github.sha }} .

      - name: Docker Push
        if: github.ref == 'refs/heads/main'
        run: |
          echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
          docker tag chat-service:${{ github.sha }} yourdockerhub/chat-service:latest
          docker push yourdockerhub/chat-service:latest

      - name: Deploy to Staging
        if: github.ref == 'refs/heads/main'
        run: |
          helm upgrade --install chat-service deployment/helm/chat-service \
            --set image.tag=${{ github.sha }} \
            --namespace staging
```

**Parallel Builds**: Since services are independent, CI/CD pipelines run in parallel, enabling faster feedback.

---

## Development Workflow

### 1. Start Infrastructure

```powershell
# Start shared infrastructure (PostgreSQL, Redis, RabbitMQ, Observability)
docker compose -f deployment/docker-compose/docker-compose.dev.yml up -d postgres redis rabbitmq prometheus grafana jaeger
```

### 2. Run Services Locally (Hot Reload)

```powershell
# Terminal 1: Gateway
cd src/Gateway/CodingAgent.Gateway
dotnet watch run

# Terminal 2: Chat Service
cd src/Services/Chat/CodingAgent.Services.Chat
dotnet watch run

# Terminal 3: Orchestration Service
cd src/Services/Orchestration/CodingAgent.Services.Orchestration
dotnet watch run

# Terminal 4: ML Classifier (Python)
cd src/Services/MLClassifier/ml_classifier_service
uvicorn main:app --reload

# Terminal 5: Angular Dashboard
cd src/Frontend/coding-agent-dashboard
npm start
```

### 3. Access Services

| Service | URL | Description |
|---------|-----|-------------|
| Gateway | http://localhost:5000 | Single entry point |
| Chat API | http://localhost:5001 | (via Gateway: /api/chat) |
| Orchestration | http://localhost:5002 | (via Gateway: /api/orchestration) |
| ML Classifier | http://localhost:5003 | (via Gateway: /api/ml) |
| Swagger | http://localhost:5000/swagger | Aggregated API docs |
| Angular Dashboard | http://localhost:4200 | Frontend |
| Grafana | http://localhost:3000 | Metrics dashboard |
| Jaeger | http://localhost:16686 | Distributed tracing |
| RabbitMQ Mgmt | http://localhost:15672 | Message queue admin |

---

## Testing Strategy

### Unit Tests

**Per Service**: Located in `*.Tests` project next to service project.

```bash
# Run unit tests for specific service
dotnet test src/Services/Chat/CodingAgent.Services.Chat.Tests

# Run all unit tests
dotnet test --filter "Category=Unit"
```

### Integration Tests

**Per Service**: Uses Testcontainers to spin up PostgreSQL, Redis, RabbitMQ.

```csharp
// Example: ChatServiceFixture.cs
public class ChatServiceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Run EF migrations
        var dbContext = new ChatDbContext(_postgres.GetConnectionString());
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
```

### Contract Tests (Pact)

Verify service-to-service contracts:

```bash
# Provider (e.g., ML Classifier) publishes contract
dotnet test src/Services/MLClassifier/CodingAgent.Services.MLClassifier.Tests --filter "Category=ContractProvider"

# Consumer (e.g., Orchestration) verifies contract
dotnet test src/Services/Orchestration/CodingAgent.Services.Orchestration.Tests --filter "Category=ContractConsumer"
```

### E2E Tests

Tests full user journeys across all services:

```bash
cd tests/E2E
docker compose -f docker-compose.test.yml up -d
dotnet test CodingAgent.E2E.Tests
```

### Load Tests (k6)

```bash
cd tests/Load
k6 run --vus 100 --duration 5m gateway-load-test.js
```

---

## Deployment Strategies

### Local Development

```bash
docker compose -f deployment/docker-compose/docker-compose.dev.yml up -d
```

### Staging (Docker Compose)

```bash
docker compose -f deployment/docker-compose/docker-compose.prod.yml up -d
```

### Production (Kubernetes)

```bash
# Deploy via Helm
helm install coding-agent deployment/helm/coding-agent \
  --namespace production \
  --values deployment/helm/coding-agent/values.prod.yaml

# Deploy individual service
helm upgrade chat-service deployment/helm/chat-service \
  --namespace production \
  --set image.tag=v2.1.0
```

---

## Observability Setup

### OpenTelemetry Configuration

**All .NET services**:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
```

### Grafana Dashboards

Pre-configured dashboards in `deployment/observability/grafana/dashboards/`:
- `system-overview.json`
- `api-performance.json`
- `service-health.json`
- `database-metrics.json`
- `cache-metrics.json`

---

## Next Steps

1. **Clone Repository**: `git clone https://github.com/JustAGameZA/coding-agent-v2`
2. **Setup Development Environment**: Run `scripts/setup-dev-environment.ps1`
3. **Start Phase 0**: Review architecture docs and start POC
4. **Join Weekly Standups**: Monday 9am (progress tracking)

---

**Document Owner**: Architecture Team
**Review Cycle**: Bi-weekly
**Approval Required**: Technical Lead
