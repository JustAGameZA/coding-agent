# Service Catalog - Detailed Specifications

**Version**: 2.0.0
**Last Updated**: October 24, 2025

---

## Service Overview Matrix

| Service | Lines of Code (Est.) | Team Size | Complexity | Dependencies |
|---------|---------------------|-----------|------------|--------------|
| API Gateway | 2,000 | 0.5 | Low | None (entry point) |
| Chat Service | 8,000 | 1.0 | Medium | Auth, ML Classifier |
| Orchestration Service | 12,000 | 1.5 | High | All services |
| ML Classifier | 5,000 | 1.0 | High | Training data from Orchestration |
| GitHub Service | 6,000 | 1.0 | Medium | Auth, Orchestration |
| Browser Service | 4,000 | 0.5 | Medium | Orchestration |
| CI/CD Monitor | 5,000 | 0.5 | Medium | GitHub, Orchestration |
| Dashboard Service | 3,000 | 0.5 | Low | All services (BFF) |
| Ollama Service | 4,000 | 0.5 | Medium | Orchestration (LLM provider) |

**Total Estimated LOC**: ~49,000 (down from 60,000 in monolith due to reduced coupling)

---

## 1. API Gateway Service

### Responsibility
Single entry point for all external requests. Handles cross-cutting concerns before routing to backend services.

### Technology Stack
- **Framework**: YARP (Yet Another Reverse Proxy) on .NET 9
- **Auth**: ASP.NET Core Identity + JWT Bearer
- **Rate Limiting**: AspNetCoreRateLimit
- **Circuit Breaker**: Polly

### API Endpoints

```yaml
Routes:
  /api/auth/**        → Auth Service (future extraction)
  /api/chat/**        → Chat Service
  /api/orchestration/** → Orchestration Service
  /api/ml/**          → ML Classifier Service
  /api/github/**      → GitHub Service
  /api/browser/**     → Browser Service
  /api/cicd/**        → CI/CD Monitor
  /api/dashboard/**   → Dashboard Service
  /api/ollama/**      → Ollama Service (LLM provider)

External Integration APIs (for VS Code, Continue, AI Toolkit):
  /api/generate       → Ollama-compatible text completion (streaming)
  /api/chat           → Ollama-compatible chat completion (streaming)
  /api/tags           → List available models (Ollama format)
  /api/show           → Model information (Ollama format)
  /v1/chat/completions → OpenAI-compatible chat endpoint (streaming)

Special:
  /health            → Gateway health check
  /metrics           → Prometheus metrics
  /swagger           → Aggregated OpenAPI spec
```

### Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "chat-route": {
        "ClusterId": "chat-cluster",
        "Match": { "Path": "/api/chat/{**catch-all}" },
        "Transforms": [
          { "PathRemovePrefix": "/api/chat" }
        ]
      }
    },
    "Clusters": {
      "chat-cluster": {
        "Destinations": {
          "chat-1": { "Address": "http://chat-service:5001/" },
          "chat-2": { "Address": "http://chat-service:5002/" }
        },
        "LoadBalancingPolicy": "RoundRobin",
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Timeout": "00:00:05",
            "Path": "/health"
          }
        }
      }
    }
  },
  "RateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  },
  "Authentication": {
    "Jwt": {
      "Authority": "http://gateway:5000"
    }
  }
}
```

### Key Features

1. **JWT Validation**: Validates all incoming requests
2. **Rate Limiting**: Per-user (1000 req/hour), per-IP (100 req/min)
3. **Request Transformation**: Path rewriting, header injection
4. **Circuit Breaking**: Fail fast if downstream service unavailable
5. **Load Balancing**: Round-robin across multiple instances
6. **Health Checks**: Active probing of backend services
7. **Telemetry**: OpenTelemetry traces for all requests
8. **External API Compatibility**: Ollama and OpenAI-compatible endpoints for IDE integration

### Deployment

```yaml
# Kubernetes Deployment
replicas: 2 (HA)
resources:
  requests: { cpu: 100m, memory: 256Mi }
  limits: { cpu: 500m, memory: 512Mi }
```

---

## 2. Chat Service

### Responsibility
Real-time chat with AI agents. Manages conversations, message history, file uploads, and WebSocket connections.

### Technology Stack
- **Framework**: .NET 9 Minimal APIs + SignalR
- **Database**: PostgreSQL (`chat` schema)
- **Cache**: Redis (conversation context)
- **Message Bus**: RabbitMQ (publish `MessageSent` events)

### Domain Model

```csharp
// Entities
public class Conversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Message> Messages { get; set; }
}

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // null for AI messages
    public string Content { get; set; }
    public MessageRole Role { get; set; } // User, Assistant, System
    public DateTime SentAt { get; set; }
    public List<Attachment> Attachments { get; set; }
}

public class Attachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string BlobUrl { get; set; } // S3/Azure Blob
    public long SizeBytes { get; set; }
}

public enum MessageRole
{
    User,
    Assistant,
    System
}
```

### API Endpoints

```http
# REST API
POST   /conversations                        # Create new conversation
GET    /conversations                        # List user's conversations
GET    /conversations/{id}                   # Get conversation details
DELETE /conversations/{id}                   # Delete conversation
POST   /conversations/{id}/messages          # Send message
GET    /conversations/{id}/messages          # Get message history
POST   /conversations/{id}/attachments       # Upload file

# WebSocket (SignalR)
Hub: /hubs/chat
Methods:
  - JoinConversation(conversationId)
  - LeaveConversation(conversationId)
  - SendMessage(conversationId, content)
  - ReceiveMessage(message) [server→client]
  - TypingIndicator(conversationId, isTyping)
```

### Key Features

1. **Real-time Messaging**: SignalR WebSocket connections
2. **Conversation Context**: Redis cache for last 100 messages
3. **File Upload**: Multi-part upload to blob storage (max 100MB)
4. **Message Search**: Full-text search via PostgreSQL
5. **Typing Indicators**: Real-time updates via SignalR
6. **User Presence**: Track online/offline status
7. **Message Reactions**: Emoji reactions to messages

### Events Published

```csharp
public record MessageSentEvent(
    Guid ConversationId,
    Guid MessageId,
    Guid UserId,
    string Content,
    DateTime SentAt
);
```

### Configuration

```json
{
  "Chat": {
    "MaxConversationsPerUser": 100,
    "MaxMessagesPerConversation": 10000,
    "MessageRetentionDays": 90,
    "MaxAttachmentSizeMB": 100,
    "AllowedFileTypes": [".pdf", ".txt", ".md", ".json", ".yaml"],
    "Redis": {
      "CacheExpirationMinutes": 60
    }
  }
}
```

### Deployment

```yaml
replicas: 2
resources:
  requests: { cpu: 200m, memory: 512Mi }
  limits: { cpu: 1000m, memory: 1Gi }
```

---

## 3. Orchestration Service

### Responsibility
Core task execution engine. Classifies tasks, selects execution strategy, coordinates agents, manages workflows.

### Technology Stack
- **Framework**: .NET 9 Minimal APIs
- **Database**: PostgreSQL (`orchestration` schema)
- **Message Bus**: RabbitMQ (publish task events)
- **State Management**: Redis (workflow state)

### Domain Model

```csharp
public class CodingTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskType Type { get; set; } // from ML Classifier
    public TaskComplexity Complexity { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<TaskExecution> Executions { get; set; }
}

public class TaskExecution
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public ExecutionStrategy Strategy { get; set; }
    public string ModelUsed { get; set; }
    public int TokensUsed { get; set; }
    public decimal CostUSD { get; set; }
    public TimeSpan Duration { get; set; }
    public ExecutionResult Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TaskStatus
{
    Pending,
    Classifying,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum ExecutionStrategy
{
    SingleShot,
    Iterative,
    MultiAgent,
    HybridExecution
}
```

### API Endpoints

```http
POST   /tasks                    # Create new task
GET    /tasks                    # List tasks (paginated)
GET    /tasks/{id}               # Get task details
POST   /tasks/{id}/execute       # Execute task (optional overrides supported)
PUT    /tasks/{id}/cancel        # Cancel running task
POST   /tasks/{id}/retry         # Retry failed task
GET    /tasks/{id}/executions    # Get execution history
GET    /tasks/{id}/logs          # Stream execution logs (SSE)
POST   /tasks/classify           # Classify task without executing
GET    /strategies               # List available execution strategies
```

### Workflow

```text
1. Task Created
   ↓
2. Call ML Classifier → Get task type + complexity
   ↓
3. Select Execution Strategy (based on complexity)
   ↓
4. Execute Task:
   - Simple: SingleShot (one LLM call)
   - Medium: Iterative (multi-turn with validation)
   - Complex: MultiAgent (parallel agents)
   ↓
5. Publish TaskCompletedEvent
   ↓
6. Update Training Data (for ML feedback loop)
```

### Key Features

1. **Smart Classification**: Delegates to ML Classifier service
2. **Multiple Strategies**: SingleShot, Iterative, MultiAgent
3. **Retry Logic**: Exponential backoff with Polly
4. **Execution Telemetry**: Track tokens, cost, duration
5. **Streaming Logs**: Server-Sent Events for real-time logs
6. **Rollback Support**: Undo changes on failure
7. **SAGA Pattern**: Distributed transactions across services

### Execute API – overrides (forceStrategy, maxParallelSubagents)

Clients can optionally override strategy selection and limit the degree of parallelism for MultiAgent runs.

Request body (optional fields):

```json
{
  "forceStrategy": "Iterative",            
  "maxParallelSubagents": 3                  
}
```

- forceStrategy (optional): one of ["SingleShot", "Iterative", "MultiAgent"].
  - When provided, the orchestrator uses this strategy instead of auto-selection by complexity/classifier.
  - If the strategy is incompatible with the task (e.g., SingleShot for an Epic task), the request is rejected with 400 and a validation message.
- maxParallelSubagents (optional, integer 1–10): upper bound of concurrently running subagents for MultiAgent execution.
  - Ignored for SingleShot/Iterative.
  - If omitted, defaults to the service configuration value Orchestration.MaxParallelSubagents.

Responses:
- 202 Accepted with an execution resource reference when execution is queued/started.
- 400 Bad Request on validation failure (invalid strategy value, out-of-range parallelism, task not eligible).
- 409 Conflict if the task is already running.
- 404 Not Found if the task id does not exist or is not visible to the caller.

Notes:
- SSE logs support an optional `executionId` query parameter to stream a specific execution; otherwise the latest execution is streamed.

### Events Published

```csharp
public record TaskCreatedEvent(Guid TaskId, string Description);
public record TaskCompletedEvent(Guid TaskId, ExecutionResult Result);
public record TaskFailedEvent(Guid TaskId, string Error);
```

### Configuration

```json
{
  "Orchestration": {
    "MaxConcurrentTasks": 10,
    "MaxParallelSubagents": 3,
    "DefaultStrategy": "Iterative",
    "MaxRetries": 3,
    "RetryDelaySeconds": [5, 15, 60],
    "Models": {
      "Simple": "gpt-4o-mini",
      "Medium": "gpt-4o",
      "Complex": "gpt-4o + claude-3.5-sonnet"
    },
    "CloudApis": {
      "OpenAI": {
        "Enabled": true,
        "ApiKey": "sk-...",
        "MaxTokensPerMonth": 1000000,
        "AlertThresholdPercent": 80
      },
      "Anthropic": {
        "Enabled": false,
        "ApiKey": "",
        "MaxTokensPerMonth": 500000,
        "AlertThresholdPercent": 80
      }
    },
    "Ollama": {
      "ServiceUrl": "http://ollama-service:5008",
      "FallbackToCloudOnFailure": true,
      "CircuitBreakerThreshold": 5,
      "CircuitBreakerDurationSeconds": 30
    }
  }
}
```

### Deployment

```yaml
replicas: 3
resources:
  requests: { cpu: 500m, memory: 1Gi }
  limits: { cpu: 2000m, memory: 2Gi }
```

---

## 4. ML Classifier Service

### Responsibility
Task classification using machine learning. Predicts task type, complexity, and optimal execution strategy.

### Technology Stack
- **Framework**: Python FastAPI + Uvicorn
- **ML Libraries**: scikit-learn, XGBoost, ONNX Runtime
- **Database**: PostgreSQL (`ml` schema)
- **Cache**: Redis (model cache)

### Domain Model

```python
from pydantic import BaseModel
from enum import Enum

class TaskType(str, Enum):
    BUG_FIX = "bug_fix"
    FEATURE = "feature"
    REFACTOR = "refactor"
    DOCUMENTATION = "documentation"
    TEST = "test"
    DEPLOYMENT = "deployment"

class TaskComplexity(str, Enum):
    SIMPLE = "simple"         # < 50 LOC
    MEDIUM = "medium"         # 50-200 LOC
    COMPLEX = "complex"       # > 200 LOC

class ClassificationRequest(BaseModel):
    task_description: str
    context: dict[str, str] | None = None
    files_changed: list[str] | None = None

class ClassificationResult(BaseModel):
    task_type: TaskType
    complexity: TaskComplexity
    confidence: float  # 0.0 - 1.0
    reasoning: str
    suggested_strategy: str
    estimated_tokens: int
```

### API Endpoints

```http
POST   /classify                    # Classify single task
POST   /classify/batch               # Classify multiple tasks
POST   /train                        # Trigger model retraining
GET    /models                       # List available models
GET    /models/{id}/metrics          # Get model performance metrics
POST   /feedback                     # Submit classification feedback
GET    /health                       # Service health check
```

### Classification Pipeline

```python
# Heuristic Classifier (Fast, 90% accuracy)
def heuristic_classify(description: str) -> ClassificationResult:
    keywords = {
        "BUG_FIX": ["bug", "error", "fix", "crash", "issue"],
        "FEATURE": ["add", "implement", "create", "new"],
        "REFACTOR": ["refactor", "clean", "optimize", "improve"],
    }
    # ... keyword matching logic

# ML Classifier (Slow, 95%+ accuracy)
def ml_classify(description: str) -> ClassificationResult:
    # XGBoost model trained on historical data
    features = extract_features(description)
    prediction = xgb_model.predict(features)
    return ClassificationResult(...)

# Hybrid Approach (Best of both)
def classify(request: ClassificationRequest) -> ClassificationResult:
    heuristic_result = heuristic_classify(request.task_description)

    if heuristic_result.confidence > 0.85:
        return heuristic_result  # Fast path
    else:
        return ml_classify(request.task_description)  # Accurate path
```

### Key Features

1. **Hybrid Classification**: Heuristic + ML fallback
2. **Continuous Learning**: Feedback loop for model improvement
3. **A/B Testing**: Compare model versions in production
4. **Feature Extraction**: TF-IDF, embeddings, code metrics
5. **Model Versioning**: Track model versions and rollback
6. **Performance Tracking**: Accuracy, precision, recall metrics

### Training Data Collection

```python
# Event Listener
@consumer("TaskCompletedEvent")
async def collect_training_sample(event: TaskCompletedEvent):
    sample = TrainingSample(
        description=event.task_description,
        actual_type=event.task_type,  # ground truth
        actual_complexity=event.complexity,
        tokens_used=event.tokens_used,
        duration=event.duration
    )
    await training_repo.save(sample)

    # Retrain every 1000 samples
    if await training_repo.count() % 1000 == 0:
        await trigger_retraining()
```

### Configuration

```python
# config.py
ML_CONFIG = {
    "model_path": "/models/xgboost_classifier_v2.pkl",
    "confidence_threshold": 0.85,
    "retraining_sample_size": 1000,
    "feature_extractors": ["tfidf", "code_metrics", "embeddings"],
    "max_request_size_kb": 100,
}
```

### Deployment

```yaml
replicas: 2
resources:
  requests: { cpu: 1000m, memory: 2Gi }
  limits: { cpu: 2000m, memory: 4Gi }
```

---

## 5. GitHub Service

### Responsibility
GitHub integration: repository operations, PR creation, issue management, webhook handling.

### Technology Stack
- **Framework**: .NET 9 Minimal APIs
- **GitHub Client**: Octokit.NET
- **Database**: PostgreSQL (`github` schema)
- **Message Bus**: RabbitMQ

### Domain Model

```csharp
public class Repository
{
    public Guid Id { get; set; }
    public string Owner { get; set; }
    public string Name { get; set; }
    public string CloneUrl { get; set; }
    public string DefaultBranch { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

public class PullRequest
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceBranch { get; set; }
    public string TargetBranch { get; set; }
    public PRStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Issue
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public int Number { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public List<string> Labels { get; set; }
    public IssueStatus Status { get; set; }
}
```

### API Endpoints

```http
# Repositories
GET    /repositories                      # List connected repos
POST   /repositories                      # Connect new repo
DELETE /repositories/{id}                 # Disconnect repo
POST   /repositories/{id}/sync            # Sync repo metadata

# Pull Requests
POST   /repositories/{id}/pulls           # Create PR
GET    /repositories/{id}/pulls           # List PRs
GET    /pulls/{id}                        # Get PR details
POST   /pulls/{id}/merge                  # Merge PR
POST   /pulls/{id}/close                  # Close PR

# Issues
GET    /repositories/{id}/issues          # List issues
GET    /issues/{id}                       # Get issue details
POST   /issues/{id}/comment               # Add comment
POST   /issues/{id}/close                 # Close issue

# Webhooks
POST   /webhooks/github                   # GitHub webhook endpoint
```

### Key Features

1. **OAuth Integration**: GitHub App authentication
2. **Webhook Handling**: Process push, PR, issue events
3. **Branch Management**: Create, delete, merge branches
4. **PR Templates**: Customizable PR descriptions
5. **Code Review**: Automated code review comments
6. **Status Checks**: Update PR status checks
7. **Release Management**: Create releases and tags

### Events Published

```csharp
public record PullRequestCreatedEvent(Guid PullRequestId, string Url);
public record IssueClosedEvent(Guid IssueId, string Resolution);
```

### Configuration

```json
{
  "GitHub": {
    "AppId": 123456,
    "PrivateKeyPath": "/secrets/github-app.pem",
    "WebhookSecret": "***",
    "DefaultLabels": ["automated", "coding-agent"],
    "PRTemplate": "templates/pr_template.md"
  }
}
```

### Deployment

```yaml
replicas: 2
resources:
  requests: { cpu: 200m, memory: 512Mi }
  limits: { cpu: 1000m, memory: 1Gi }
```

---

## 6. Browser Service

### Responsibility
Browser automation using Playwright: web scraping, screenshot capture, form filling.

### Technology Stack
- **Framework**: .NET 9 Minimal APIs
- **Browser Automation**: Microsoft Playwright
- **Database**: None (stateless)
- **Cache**: Redis (page cache)

### API Endpoints

```http
POST   /browse                     # Navigate to URL
POST   /screenshot                 # Capture screenshot
POST   /extract                    # Extract page content
POST   /interact                   # Click, type, select
POST   /pdf                        # Generate PDF
GET    /health                     # Service health
```

### Key Features

1. **Headless Browsing**: Chromium, Firefox, WebKit
2. **Screenshot Capture**: Full page or element-specific
3. **Content Extraction**: Text, links, images, forms
4. **Form Interaction**: Fill, submit, upload files
5. **PDF Generation**: Convert pages to PDF
6. **Session Management**: Cookie persistence

### Configuration

```json
{
  "Browser": {
    "Headless": true,
    "Timeout": 30000,
    "UserAgent": "CodingAgent/2.0",
    "BlockImages": false,
    "BlockCSS": false
  }
}
```

### Deployment

```yaml
replicas: 1
resources:
  requests: { cpu: 500m, memory: 1Gi }
  limits: { cpu: 2000m, memory: 4Gi }
```

---

## 7. CI/CD Monitor Service

### Responsibility
Monitor GitHub Actions builds, detect failures, generate automated fixes.

### API Endpoints

```http
GET    /builds                     # List recent builds
GET    /builds/{id}                # Get build details
POST   /builds/{id}/analyze        # Analyze build failure
POST   /builds/{id}/fix            # Generate automated fix
GET    /health                     # Service health
```

### Key Features

1. **Build Monitoring**: Poll GitHub Actions API
2. **Failure Detection**: Parse build logs
3. **Error Extraction**: Identify root causes
4. **Automated Fixes**: Generate PR with fixes
5. **Success Tracking**: Track fix success rate

### Deployment

```yaml
replicas: 1
resources:
  requests: { cpu: 100m, memory: 256Mi }
  limits: { cpu: 500m, memory: 512Mi }
```

---

## 8. Dashboard Service (BFF)

### Responsibility
Backend-for-Frontend: Aggregate data from multiple services for Angular dashboard.

### API Endpoints

```http
GET    /dashboard/stats            # Aggregate statistics
GET    /dashboard/tasks            # Enriched task list
GET    /dashboard/activity         # Recent activity feed
GET    /dashboard/user/profile     # User profile + preferences
```

### Key Features

1. **Data Aggregation**: Call multiple services in parallel
2. **Response Shaping**: Transform data for frontend
3. **Caching**: Aggressive caching (5 min TTL)
4. **GraphQL Support**: Optional GraphQL endpoint

### Deployment

```yaml
replicas: 2
resources:
  requests: { cpu: 100m, memory: 256Mi }
  limits: { cpu: 500m, memory: 512Mi }
```

---

## 9. Ollama Service

### Responsibility
Managed local LLM provider using Ollama. Provides cost-effective, on-premise inference for code generation, reducing reliance on external APIs (OpenAI, Anthropic). Acts as an intelligent router between hosted Ollama models and the orchestration service.

### Technology Stack
- **Framework**: .NET 9 Minimal APIs
- **LLM Backend**: Ollama (Docker container)
- **Database**: PostgreSQL (`ollama` schema) for model metadata, usage tracking
- **Cache**: Redis (prompt cache, response cache for deterministic queries)
- **Message Bus**: RabbitMQ (publish model usage events)

### Domain Model

```csharp
public class OllamaModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }              // e.g., "codellama:13b"
    public string DisplayName { get; set; }       // e.g., "CodeLlama 13B"
    public string Version { get; set; }           // Model version
    public long SizeBytes { get; set; }
    public ModelCapability Capability { get; set; } // CodeGen, Chat, Analysis
    public ModelStatus Status { get; set; }       // Available, Downloading, Error
    public DateTime PulledAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public ModelMetrics Metrics { get; set; }
}

public class ModelMetrics
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double AverageLatencyMs { get; set; }
    public long TotalTokensGenerated { get; set; }
    public double AverageQualityScore { get; set; } // User feedback
}

public class OllamaRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public string ModelName { get; set; }
    public string Prompt { get; set; }
    public OllamaOptions Options { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public OllamaResponse? Response { get; set; }
}

public class OllamaOptions
{
    public double Temperature { get; set; } = 0.7;
    public int NumPredict { get; set; } = 2000;
    public int TopK { get; set; } = 40;
    public double TopP { get; set; } = 0.9;
    public int NumCtx { get; set; } = 4096;      // Context window
    public string Stop { get; set; }             // Stop sequences
}

public class OllamaResponse
{
    public string ModelName { get; set; }
    public string Response { get; set; }
    public int PromptEvalCount { get; set; }     // Prompt tokens
    public int EvalCount { get; set; }           // Generated tokens
    public double EvalDuration { get; set; }     // Generation time (ns)
    public bool Done { get; set; }
    public string DoneReason { get; set; }       // "stop", "length", "error"
}

public enum ModelCapability
{
    CodeGeneration,
    ChatCompletion,
    CodeAnalysis,
    CodeReview,
    Documentation,
    Testing
}

public enum ModelStatus
{
    Available,
    Downloading,
    Loading,
    Error,
    Deprecated
}
```

### API Endpoints

```http
# Model Management
GET    /models                          # List available models
POST   /models/pull                     # Download new model from Ollama registry
DELETE /models/{name}                   # Remove model
GET    /models/{name}                   # Get model details
GET    /models/{name}/metrics           # Get usage metrics

# Generation (Ollama-compatible)
POST   /generate                        # Text completion (streaming)
POST   /chat                            # Chat completion (streaming)
POST   /embeddings                      # Generate embeddings

# Orchestration Integration
POST   /inference                       # High-level inference (auto-selects model)
POST   /code-generation                 # Specialized code generation endpoint
POST   /code-review                     # Code review endpoint
POST   /explain                         # Code explanation endpoint

# Cache Management
GET    /cache/stats                     # Cache hit rate, size
DELETE /cache                           # Flush cache
POST   /cache/warm                      # Pre-warm cache with common prompts

# Health & Admin
GET    /health                          # Service health + Ollama backend status
GET    /admin/usage                     # Usage statistics
POST   /admin/reload-models             # Reload model list from Ollama
```

### Ollama Backend Integration

**Architecture**:

```
┌────────────────────────────────────────────────┐
│          Ollama Service (.NET 9)               │
│  ┌──────────────────────────────────────────┐ │
│  │   API Layer (Minimal APIs)               │ │
│  └────────────┬─────────────────────────────┘ │
│               │                                │
│  ┌────────────▼─────────────────────────────┐ │
│  │   Domain Services                        │ │
│  │   • ModelManager                         │ │
│  │   • InferenceRouter                      │ │
│  │   • PromptOptimizer                      │ │
│  └────────────┬─────────────────────────────┘ │
│               │                                │
│  ┌────────────▼─────────────────────────────┐ │
│  │   Ollama HTTP Client                     │ │
│  │   (HttpClient → http://ollama:11434)     │ │
│  └────────────┬─────────────────────────────┘ │
└───────────────┼────────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│         Ollama Container                      │
│         (ollama/ollama:latest)                │
│                                               │
│   Models:                                     │
│   • codellama:13b    (Code generation)        │
│   • deepseek-coder:6.7b (Fast inference)      │
│   • starcoder2:7b    (Multi-language)         │
│   • mistral:7b       (General purpose)        │
│                                               │
│   Storage: /root/.ollama (volume mount)       │
└───────────────────────────────────────────────┘
```

### Key Features

#### 1. Intelligent Model Selection

**Auto-routing based on task characteristics**:

```csharp
public class InferenceRouter
{
    public string SelectOptimalModel(CodeGenerationRequest request)
    {
        // Fast model for simple tasks
        if (request.ComplexityScore < 0.3)
            return "deepseek-coder:6.7b";  // 2s inference

        // Specialized model for specific languages
        if (request.Language == "python")
            return "codellama:13b-python"; // Python-optimized

        // General-purpose model for complex tasks
        return "codellama:13b";            // 5s inference
    }
}
```

#### 2. Prompt Optimization & Caching

**Deterministic prompt caching**:

```csharp
public async Task<string> GenerateWithCacheAsync(string prompt, string model)
{
    // Generate cache key
    var cacheKey = $"ollama:{model}:{ComputeSHA256(prompt)}";

    // Check Redis cache
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        _logger.LogInformation("Cache hit for prompt hash {Hash}", cacheKey);
        return cached;
    }

    // Generate via Ollama
    var response = await _ollamaClient.GenerateAsync(model, prompt);

    // Cache for 24 hours (deterministic prompts only)
    if (IsDeterministic(prompt))
    {
        await _cache.SetStringAsync(cacheKey, response,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });
    }

    return response;
}
```

#### 3. Streaming Support

**Server-Sent Events (SSE)**:

```csharp
[HttpPost("generate")]
public async Task GenerateStreaming([FromBody] OllamaGenerateRequest request)
{
    Response.ContentType = "text/event-stream";
    Response.Headers.Add("Cache-Control", "no-cache");

    await foreach (var chunk in _ollamaClient.GenerateStreamAsync(request.Model, request.Prompt))
    {
        var json = JsonSerializer.Serialize(new { response = chunk, done = false });
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { done = true })}\n\n");
}
```

#### 4. Cost Tracking

**Track usage for budgeting**:

```csharp
public async Task RecordUsageAsync(OllamaRequest request, OllamaResponse response)
{
    var usage = new ModelUsage
    {
        UserId = request.UserId,
        TaskId = request.TaskId,
        ModelName = request.ModelName,
        PromptTokens = response.PromptEvalCount,
        CompletionTokens = response.EvalCount,
        TotalTokens = response.PromptEvalCount + response.EvalCount,
        LatencyMs = response.EvalDuration / 1_000_000, // Convert ns to ms
        Cost = 0.0m,  // Ollama is free, but track "opportunity cost"
        Timestamp = DateTime.UtcNow
    };

    await _usageRepo.CreateAsync(usage);

    // Publish event for analytics
    await _eventBus.PublishAsync(new ModelUsageEvent
    {
        ModelName = usage.ModelName,
        TokensUsed = usage.TotalTokens,
        LatencyMs = usage.LatencyMs
    });
}
```

#### 5. Model Health Monitoring

**Periodic health checks**:

```csharp
public class OllamaHealthCheck : IHealthCheck
{
    private readonly IOllamaClient _ollamaClient;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Check if Ollama is responsive
            var models = await _ollamaClient.ListModelsAsync();

            if (models.Count == 0)
                return HealthCheckResult.Degraded("No models available");

            // Check if primary model is loaded
            var primaryModel = models.FirstOrDefault(m => m.Name == "codellama:13b");
            if (primaryModel == null)
                return HealthCheckResult.Degraded("Primary model not available");

            return HealthCheckResult.Healthy($"{models.Count} models available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ollama backend unreachable", ex);
        }
    }
}
```

### Integration with Orchestration Service

**LLM Client Interface**:

```csharp
// In Orchestration Service
public interface ILlmClient
{
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateStreamAsync(LlmRequest request, CancellationToken ct = default);
}

// Ollama implementation
public class OllamaLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;

    public OllamaLlmClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("Ollama");
        _httpClient.BaseAddress = new Uri("http://ollama-service:5008");
    }

    public async Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct)
    {
        var ollamaRequest = new
        {
            model = MapModelName(request.Model),
            prompt = request.Prompt,
            options = new
            {
                temperature = request.Temperature,
                num_predict = request.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/generate", ollamaRequest, ct);
        response.EnsureSuccessStatusCode();

        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);

        return new LlmResponse
        {
            Content = ollamaResponse.Response,
            Model = ollamaResponse.ModelName,
            TokensUsed = ollamaResponse.PromptEvalCount + ollamaResponse.EvalCount,
            FinishReason = ollamaResponse.DoneReason
        };
    }

    private string MapModelName(string requestedModel)
    {
        // Map generic model names to Ollama-specific models
        return requestedModel switch
        {
            "code-generation" => "codellama:13b",
            "fast-code" => "deepseek-coder:6.7b",
            "chat" => "mistral:7b",
            _ => requestedModel
        };
    }
}
```

### Model Recommendation Strategy

**Decision Matrix**:

| Task Type | Complexity | Model | Context Size | Avg Latency |
|-----------|-----------|-------|--------------|-------------|
| Bug Fix | Simple | deepseek-coder:6.7b | 4K | 2s |
| Feature | Medium | codellama:13b | 8K | 5s |
| Refactor | Complex | codellama:34b | 16K | 15s |
| Documentation | Simple | mistral:7b | 8K | 3s |
| Code Review | Medium | codellama:13b | 8K | 5s |
| Testing | Simple | deepseek-coder:6.7b | 4K | 2s |

### Events Published

```csharp
public record ModelUsageEvent(
    string ModelName,
    long TokensUsed,
    double LatencyMs,
    DateTime Timestamp
);

public record ModelDownloadedEvent(
    string ModelName,
    long SizeBytes,
    DateTime DownloadedAt
);

public record InferenceFailedEvent(
    string ModelName,
    string ErrorMessage,
    Guid RequestId,
    DateTime FailedAt
);
```

### Configuration

```json
{
  "Ollama": {
    "BaseUrl": "http://ollama:11434",
    "Timeout": 120000,
    "DefaultModel": "codellama:13b",
    "Models": {
      "CodeGeneration": "codellama:13b",
      "FastInference": "deepseek-coder:6.7b",
      "Chat": "mistral:7b",
      "Python": "codellama:13b-python"
    },
    "CacheEnabled": true,
    "CacheTTLHours": 24,
    "MaxConcurrentRequests": 5,
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffSeconds": [2, 5, 10]
    }
  },
  "ModelManagement": {
    "AutoPullModels": ["codellama:13b", "deepseek-coder:6.7b"],
    "PullOnStartup": true,
    "CheckForUpdatesDaily": true
  }
}
```

### Deployment

```yaml
# Ollama Service (.NET)
replicas: 2
resources:
  requests: { cpu: 200m, memory: 512Mi }
  limits: { cpu: 1000m, memory: 1Gi }

# Ollama Backend (GPU-accelerated)
replicas: 1  # Single instance (GPU constraint)
resources:
  requests: { cpu: 4000m, memory: 8Gi }
  limits: { cpu: 8000m, memory: 16Gi, nvidia.com/gpu: 1 }

volumes:
  - name: ollama-models
    persistentVolumeClaim:
      claimName: ollama-models-pvc  # 100GB for model storage
```

### Docker Compose Example

```yaml
services:
  ollama-service:
    build: ./src/Services/Ollama
    ports: ["5008:5008"]
    environment:
      - Ollama__BaseUrl=http://ollama:11434
      - ConnectionStrings__OllamaDb=Host=postgres;Database=coding_agent;Username=dev
      - Redis__Connection=redis:6379
    depends_on:
      - ollama
      - postgres
      - redis

  ollama:
    image: ollama/ollama:latest
    ports: ["11434:11434"]
    volumes:
      - ollama-models:/root/.ollama
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    environment:
      - OLLAMA_HOST=0.0.0.0

volumes:
  ollama-models:
```

### Advantages of Ollama Service

1. **Cost Reduction**: $0 inference cost vs $0.50-$2.00 per 1M tokens for cloud APIs
2. **Data Privacy**: All inference happens on-premise, no data leaves infrastructure
3. **Low Latency**: Direct network connection, no internet round-trip (50ms → 5s vs 200ms → 2s)
4. **Offline Support**: Works without internet connectivity
5. **Customizable Models**: Fine-tune models on proprietary codebases
6. **Compliance**: Meets strict data residency requirements (GDPR, HIPAA)

### When to Use Ollama vs Cloud LLMs

| Criterion | Use Ollama | Use Cloud (OpenAI/Anthropic) |
|-----------|------------|------------------------------|
| **Task Complexity** | Simple, medium | Complex, critical |
| **Budget** | Cost-sensitive | High-value tasks |
| **Data Sensitivity** | Proprietary code | Public algorithms |
| **Latency Tolerance** | Real-time (5s) | Can wait (10-30s) |
| **Quality Requirements** | 85-90% accuracy | 95%+ accuracy |

---

## Service Communication Matrix

| From ↓ To → | Gateway | Chat | Orch | ML | GitHub | Browser | CI/CD | Dashboard | Ollama |
|-------------|---------|------|------|----|----|---------|-------|-----------|--------|
| Gateway | - | REST | REST | REST | REST | REST | REST | REST | REST |
| Chat | - | - | Events | REST | Events | - | - | Events | - |
| Orchestration | - | Events | - | REST | REST | REST | REST | Events | REST |
| ML | - | - | Events | - | - | - | - | - | - |
| GitHub | - | Events | Events | - | - | - | Events | - | - |
| Browser | - | - | Events | - | - | - | - | - | - |
| CI/CD | - | - | Events | - | REST | - | - | - | - |
| Dashboard | - | REST | REST | REST | REST | REST | REST | - | REST |
| Ollama | - | - | Events | - | - | - | - | - | - |

**Legend**: REST = Synchronous HTTP, Events = Asynchronous RabbitMQ

**Key Integrations**:
- **Orchestration → Ollama**: Primary LLM inference calls (code generation, chat, review)
- **Dashboard → Ollama**: Model status queries, usage statistics
- **Ollama → Events**: Publishes `ModelUsageEvent`, `InferenceFailedEvent` for analytics

---

## Next Steps

- [02-API-CONTRACTS.md](./02-API-CONTRACTS.md) - Detailed OpenAPI specifications
- [03-DATA-MODELS.md](./03-DATA-MODELS.md) - Complete database schemas
