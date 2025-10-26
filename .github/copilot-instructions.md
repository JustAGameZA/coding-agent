<todos title="Todos" rule="Review steps frequently throughout the conversation and DO NOT stop between steps unless they explicitly require it.">
- No current todos
</todos>

<!-- Todos: Review steps frequently throughout the conversation and DO NOT stop between steps unless they explicitly require it. -->

# Copilot Instructions - Coding Agent v2.0 Microservices

## Project Status & Context

**Current Phase**: Architecture Complete → **Implementation Starting** (Phase 0, Week 2)
**Architecture**: 8-service microservices platform with AI-powered task orchestration
**No implementation exists yet** - scaffolding from scratch following `docs/` specifications

### Key Architecture Facts
- **8 Microservices**: Gateway (YARP), Chat (SignalR), Orchestration, ML Classifier (Python), GitHub, Browser (Playwright), CI/CD Monitor, Dashboard (BFF)
- **Tech Stack**: .NET 9 Minimal APIs, Python FastAPI, Angular 20.3, PostgreSQL (schemas per service), Redis, RabbitMQ + MassTransit
- **Deployment**: Docker Compose (dev), Kubernetes (prod-ready Helm charts)
- **Observability**: OpenTelemetry → Prometheus + Grafana + Jaeger, structured logs to Seq

## Essential Documentation (Read First)

Before suggesting code, **always consult** these architecture docs:
1. **`docs/00-OVERVIEW.md`** - System architecture, service boundaries, data flows
2. **`docs/01-SERVICE-CATALOG.md`** - Detailed API specs, domain models per service
3. **`docs/03-SOLUTION-STRUCTURE.md`** - Monorepo layout, file placement conventions
4. **`docs/04-ML-AND-ORCHESTRATION-ADR.md`** - ML classification strategy, execution patterns
5. **`docs/02-IMPLEMENTATION-ROADMAP.md`** - Week-by-week plan (currently Week 2)

When implementing, **reference specific docs sections** to justify design choices.

## Code Generation Rules

### File Placement (Critical)
**Monorepo Structure** (currently empty `src/` directory):
```
src/
├── SharedKernel/CodingAgent.SharedKernel/     # Common contracts (NuGet package)
├── Gateway/CodingAgent.Gateway/                # YARP entry point
├── Services/
│   ├── Chat/CodingAgent.Services.Chat/         # SignalR + conversation management
│   ├── Orchestration/CodingAgent.Services.Orchestration/ # Task execution engine
│   ├── MLClassifier/ml_classifier_service/     # Python FastAPI ML service
│   ├── GitHub/CodingAgent.Services.GitHub/     # Octokit wrapper
│   ├── Browser/CodingAgent.Services.Browser/   # Playwright automation
│   ├── CICDMonitor/CodingAgent.Services.CICDMonitor/ # Build monitoring
│   └── Dashboard/CodingAgent.Services.Dashboard/   # BFF for Angular
└── Frontend/coding-agent-dashboard/            # Angular 20.3 app
```

**Never** create files outside this structure. Use exact project names from `03-SOLUTION-STRUCTURE.md`.

### Service Architecture Pattern (Per Service)
```
CodingAgent.Services.<ServiceName>/
├── Program.cs                          # Minimal API setup
├── Domain/                             # Entities, value objects, domain logic
│   ├── Entities/                       # Rich domain models
│   ├── ValueObjects/                   # Immutable types (TaskType, Complexity)
│   ├── Events/                         # Domain events (for RabbitMQ)
│   ├── Repositories/                   # Repository interfaces
│   └── Services/                       # Domain service interfaces
├── Application/                        # Use cases, orchestration
│   ├── Commands/                       # CQRS commands
│   ├── Queries/                        # CQRS queries
│   ├── Validators/                     # FluentValidation rules
│   └── EventHandlers/                  # RabbitMQ consumer handlers
├── Infrastructure/                     # External dependencies
│   ├── Persistence/                    # EF Core DbContext, repositories
│   ├── Caching/                        # Redis abstractions
│   ├── Messaging/                      # MassTransit event bus
│   └── ExternalServices/               # HTTP clients to other services
└── Api/                                # HTTP endpoints
    ├── Endpoints/                      # Minimal API endpoint definitions
    └── Hubs/                           # SignalR hubs (Chat service only)
```

### .NET Code Style
```csharp
// Minimal API registration (Program.cs)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChatDb")));
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration["Redis:Connection"]);
builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((ctx, cfg) => cfg.Host(builder.Configuration["RabbitMQ:Host"]));
});
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddOtlpExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddPrometheusExporter());

// Endpoint definition (Api/Endpoints/ConversationEndpoints.cs)
public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations").WithTags("Conversations");

        group.MapPost("", async (CreateConversationRequest req, IConversationService svc) =>
        {
            var conversation = await svc.CreateAsync(req);
            return Results.Created($"/conversations/{conversation.Id}", conversation);
        }).RequireAuthorization();
    }
}
```

### Python Code Style (ML Classifier)
```python
# FastAPI route (api/routes/classification.py)
from fastapi import APIRouter, Depends
from pydantic import BaseModel

router = APIRouter(prefix="/classify", tags=["Classification"])

@router.post("/", response_model=ClassificationResult)
async def classify_task(
    request: ClassificationRequest,
    classifier: Classifier = Depends(get_classifier)
) -> ClassificationResult:
    """Hybrid classification: heuristic → ML → LLM fallback"""
    return await classifier.classify(request)

# Hybrid classifier (domain/classifiers/hybrid.py)
async def classify(self, request: ClassificationRequest) -> ClassificationResult:
    # Phase 1: Fast heuristic (90% accuracy, 5ms)
    heuristic_result = self.heuristic.classify(request.task_description)
    if heuristic_result.confidence > 0.85:
        return heuristic_result
    
    # Phase 2: ML (95% accuracy, 50ms)
    ml_result = await self.ml_classifier.classify(request)
    if ml_result.confidence > 0.70:
        return ml_result
    
    # Phase 3: LLM fallback (98% accuracy, 800ms)
    return await self.llm_classifier.classify(request)
```

### Angular Code Style
```typescript
// Standalone component (features/chat/chat.component.ts)
import { Component, inject, signal } from '@angular/core';
import { ChatService } from '@core/services/chat.service';
import { SignalRService } from '@core/services/signalr.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './chat.component.html'
})
export class ChatComponent implements OnInit {
  private chatService = inject(ChatService);
  private signalR = inject(SignalRService);
  
  messages = signal<Message[]>([]);
  
  async ngOnInit() {
    await this.signalR.connect();
    this.signalR.on<Message>('ReceiveMessage', msg => {
      this.messages.update(msgs => [...msgs, msg]);
    });
  }
}
```

## Domain-Specific Patterns

### 1. ML Classification Strategy (Critical for Orchestration)
**Hybrid Approach** (see `04-ML-AND-ORCHESTRATION-ADR.md`):
- **Heuristic classifier** (keyword matching): 90% accuracy, 5ms latency → handles 85% of traffic
- **ML classifier** (XGBoost): 95% accuracy, 50ms latency → handles 14% of traffic
- **LLM fallback** (GPT-4): 98% accuracy, 800ms latency → handles 1% of edge cases

```python
# Confidence thresholds
HEURISTIC_THRESHOLD = 0.85  # Use ML if below this
ML_THRESHOLD = 0.70          # Use LLM if below this
```

### 2. Execution Strategies (Orchestration Service)
| Complexity | Strategy | Models | Use Case |
|------------|----------|--------|----------|
| Simple (<50 LOC) | `SingleShot` | gpt-4o-mini | Bug fixes, small features |
| Medium (50-200) | `Iterative` | gpt-4o | Multi-turn with validation |
| Complex (200-1000) | `MultiAgent` | gpt-4o + claude-3.5 | Parallel specialized agents |
| Epic (>1000) | `HybridExecution` | Ensemble (3 models) | Critical tasks |

### 3. Event-Driven Communication
**Always publish domain events** after state changes:
```csharp
// After task completion
await _eventBus.Publish(new TaskCompletedEvent {
    TaskId = task.Id,
    TaskType = task.Type,
    Success = result.IsSuccess,
    TokensUsed = result.TokensUsed
});
```

**ML Classifier consumes events** for self-learning:
```python
@consumer("TaskCompletedEvent")
async def collect_training_sample(event: TaskCompletedEvent):
    # Store as training data for model retraining
    await training_repo.save(TrainingSample.from_event(event))
```

### 4. SAGA Pattern for Distributed Transactions
For workflows spanning multiple services (GitHub + Browser + CI/CD):
```csharp
var saga = new Saga();
try {
    var branch = await saga.Execute(
        forward: () => _github.CreateBranch(taskId),
        compensate: (b) => _github.DeleteBranch(b.Name)
    );
    var pr = await saga.Execute(
        forward: () => _github.CreatePR(branch, changes),
        compensate: (p) => _github.ClosePR(p.Number)
    );
    // If any step fails, compensating transactions auto-rollback
} catch {
    await saga.Compensate();
}
```

### 5. Observability (Non-Negotiable)
**Every service must**:
```csharp
// Correlation ID propagation
app.Use(async (context, next) => {
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    Activity.Current?.SetTag("correlation_id", correlationId);
    await next();
});

// Structured logging
_logger.LogInformation("Task {TaskId} completed with status {Status}",
    task.Id, result.Status);

// Custom spans for critical operations
using var span = _tracer.StartActiveSpan("ClassifyTask");
span.SetAttribute("task.type", taskType);
```

## Testing Requirements (85%+ Coverage)

**CRITICAL: All test classes MUST have [Trait] attributes for proper test filtering and CI performance**

### Test Trait Requirements
- **Unit tests** must have `[Trait("Category", "Unit")]` - fast tests with no external dependencies
- **Integration tests** must have `[Trait("Category", "Integration")]` - tests using Testcontainers, databases, or external services
- This enables:
    - Fast local development: `dotnet test --filter "Category=Unit" --verbosity quiet --nologo` (< 1 second)
    - Separate CI stages: Unit tests run first, integration tests in parallel jobs
    - Avoiding VS Code "not responding" dialogs during slow integration test runs

### Test Execution Commands
Always use `.runsettings` for optimal parallel test execution:

```bash
# All tests with parallel execution
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo

# Unit tests only (fast, < 1 second)
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Unit"

# Integration tests only (Testcontainers)
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Integration"
```

> **Crash Prevention**: Always run `dotnet test` with `--verbosity quiet --nologo`. Higher verbosity produces enough console output to freeze VS Code and Copilot. Use `--filter` to target a single test when you need more detail.

### Unit Tests (Domain + Application layers)
```csharp
// CodingAgent.Services.Chat.Tests/Unit/Domain/ConversationTests.cs
[Trait("Category", "Unit")]
public class ConversationTests
{
    [Fact]
    public void AddMessage_WhenValid_ShouldSucceed()
    {
        // Arrange
        var conversation = new Conversation(userId: Guid.NewGuid());
        var message = new Message("Hello", MessageRole.User);
        
        // Act
        conversation.AddMessage(message);
        
        // Assert
        conversation.Messages.Should().ContainSingle();
    }
}
```

### Integration Tests (with Testcontainers)
```csharp
// CodingAgent.Services.Chat.Tests/Integration/ConversationEndpointsTests.cs
[Collection("ChatServiceCollection")]
[Trait("Category", "Integration")]
public class ConversationEndpointsTests : IClassFixture<ChatServiceFixture>
{
    [Fact]
    public async Task CreateConversation_ShouldPersistToDb()
    {
        // Arrange: Testcontainers spins up PostgreSQL
        var request = new CreateConversationRequest("Test Title");
        
        // Act
        var response = await _client.PostAsJsonAsync("/conversations", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDto>();
        conversation.Should().NotBeNull();
    }
}
```

## Complete Service Scaffolding Examples

### Example 1: Chat Service (Full Stack)

**Program.cs** - Service entry point:
```csharp
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Api.Hubs;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChatDb")));

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Redis:Connection"]);

// SignalR
builder.Services.AddSignalR();

// MassTransit (RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });
    });
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"])))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// Domain Services
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IConversationService, ConversationService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ChatDb"))
    .AddRedis(builder.Configuration["Redis:Connection"]);

var app = builder.Build();

// Middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map Endpoints
app.MapConversationEndpoints();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

app.Run();
```

**Domain Entity** - `Domain/Entities/Conversation.cs`:
```csharp
namespace CodingAgent.Services.Chat.Domain.Entities;

public class Conversation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    // EF Core constructor
    private Conversation() { }

    public Conversation(Guid userId, string title)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMessage(Message message)
    {
        if (message.ConversationId != Id)
            throw new DomainException("Message does not belong to this conversation");
        
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new DomainException("Title cannot be empty");
        
        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Repository** - `Infrastructure/Persistence/ConversationRepository.cs`:
```csharp
namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(
        ChatDbContext context,
        IDistributedCache cache,
        ILogger<ConversationRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Try cache first
        var cacheKey = $"conversation:{id}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for conversation {ConversationId}", id);
            return JsonSerializer.Deserialize<Conversation>(cached);
        }

        // Query database
        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(100))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conversation != null)
        {
            // Cache for 1 hour
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(conversation),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
                ct);
        }

        return conversation;
    }

    public async Task<Conversation> CreateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Created conversation {ConversationId} for user {UserId}",
            conversation.Id, conversation.UserId);
        
        return conversation;
    }
}
```

**API Endpoint** - `Api/Endpoints/ConversationEndpoints.cs`:
```csharp
namespace CodingAgent.Services.Chat.Api.Endpoints;

public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .WithTags("Conversations")
            .WithOpenApi();

        group.MapPost("", CreateConversation)
            .RequireAuthorization()
            .WithName("CreateConversation")
            .Produces<ConversationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("{id:guid}", GetConversation)
            .RequireAuthorization()
            .WithName("GetConversation")
            .Produces<ConversationDto>()
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateConversation(
        CreateConversationRequest request,
        IConversationService service,
        IValidator<CreateConversationRequest> validator,
        ClaimsPrincipal user,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("CreateConversation");
        
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var userId = user.GetUserId();
        activity?.SetTag("user.id", userId);

        var conversation = await service.CreateAsync(userId, request.Title, ct);
        
        logger.LogInformation("User {UserId} created conversation {ConversationId}",
            userId, conversation.Id);

        return Results.Created($"/conversations/{conversation.Id}", conversation);
    }

    private static async Task<IResult> GetConversation(
        Guid id,
        IConversationService service,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        using var activity = Activity.Current?.Source.StartActivity("GetConversation");
        activity?.SetTag("conversation.id", id);

        var conversation = await service.GetByIdAsync(id, ct);
        
        if (conversation == null)
            return Results.NotFound();

        // Verify user owns this conversation
        if (conversation.UserId != user.GetUserId())
            return Results.Forbid();

        return Results.Ok(conversation);
    }
}
```

**SignalR Hub** - `Api/Hubs/ChatHub.cs`:
```csharp
namespace CodingAgent.Services.Chat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IConversationService _conversationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IConversationService conversationService,
        IPublishEndpoint publishEndpoint,
        ILogger<ChatHub> logger)
    {
        _conversationService = conversationService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}",
            Context.UserIdentifier, conversationId);
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        using var activity = Activity.Current?.Source.StartActivity("SendMessage");
        activity?.SetTag("conversation.id", conversationId);

        var userId = Context.User.GetUserId();
        var message = await _conversationService.AddMessageAsync(
            conversationId, userId, content, MessageRole.User);

        // Broadcast to conversation group
        await Clients.Group(conversationId.ToString())
            .SendAsync("ReceiveMessage", message);

        // Publish event for other services
        await _publishEndpoint.Publish(new MessageSentEvent
        {
            ConversationId = conversationId,
            MessageId = message.Id,
            UserId = userId,
            Content = content,
            SentAt = DateTime.UtcNow
        });

        _logger.LogInformation("Message {MessageId} sent to conversation {ConversationId}",
            message.Id, conversationId);
    }

    public async Task TypingIndicator(Guid conversationId, bool isTyping)
    {
        await Clients.OthersInGroup(conversationId.ToString())
            .SendAsync("UserTyping", Context.UserIdentifier, isTyping);
    }
}
```

### Example 2: Orchestration Service (Execution Strategy)

**Strategy Interface** - `Domain/Strategies/IExecutionStrategy.cs`:
```csharp
namespace CodingAgent.Services.Orchestration.Domain.Strategies;

public interface IExecutionStrategy
{
    string Name { get; }
    TaskComplexity SupportsComplexity { get; }
    
    Task<ExecutionResult> ExecuteAsync(
        CodingTask task,
        ExecutionContext context,
        CancellationToken ct = default);
}
```

**SingleShot Strategy** - `Domain/Strategies/SingleShotStrategy.cs`:
```csharp
namespace CodingAgent.Services.Orchestration.Domain.Strategies;

public class SingleShotStrategy : IExecutionStrategy
{
    public string Name => "SingleShot";
    public TaskComplexity SupportsComplexity => TaskComplexity.Simple;

    private readonly ILlmClient _llmClient;
    private readonly ICodeValidator _validator;
    private readonly ILogger<SingleShotStrategy> _logger;
    private readonly ActivitySource _activitySource;

    public SingleShotStrategy(
        ILlmClient llmClient,
        ICodeValidator validator,
        ILogger<SingleShotStrategy> logger,
        ActivitySource activitySource)
    {
        _llmClient = llmClient;
        _validator = validator;
        _logger = logger;
        _activitySource = activitySource;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        CodingTask task,
        ExecutionContext context,
        CancellationToken ct = default)
    {
        using var activity = _activitySource.StartActivity("ExecuteSingleShot");
        activity?.SetTag("task.id", task.Id);
        activity?.SetTag("task.type", task.Type);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Build context from task
            var prompt = await BuildPromptAsync(task, context, ct);
            activity?.SetTag("prompt.length", prompt.Length);

            // 2. Single LLM call
            _logger.LogInformation("Executing SingleShot strategy for task {TaskId}", task.Id);
            
            var response = await _llmClient.GenerateAsync(new LlmRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    new Message { Role = "system", Content = GetSystemPrompt() },
                    new Message { Role = "user", Content = prompt }
                },
                Temperature = 0.3,
                MaxTokens = 4000
            }, ct);

            activity?.SetTag("tokens.used", response.TokensUsed);
            activity?.SetTag("cost.usd", response.Cost);

            // 3. Parse code changes
            var changes = ParseCodeChanges(response.Content);
            _logger.LogInformation("Parsed {ChangeCount} code changes", changes.Count);

            // 4. Validate changes
            var validationResult = await _validator.ValidateAsync(changes, ct);
            
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning("Validation failed for task {TaskId}: {Errors}",
                    task.Id, string.Join(", ", validationResult.Errors));
                
                return ExecutionResult.Failed(
                    "Code validation failed",
                    validationResult.Errors,
                    response.TokensUsed,
                    response.Cost,
                    stopwatch.Elapsed);
            }

            // 5. Return success
            return ExecutionResult.Success(
                changes,
                response.TokensUsed,
                response.Cost,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SingleShot strategy failed for task {TaskId}", task.Id);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return ExecutionResult.Failed(
                ex.Message,
                new[] { ex.ToString() },
                0,
                0,
                stopwatch.Elapsed);
        }
    }

    private async Task<string> BuildPromptAsync(
        CodingTask task,
        ExecutionContext context,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Task: {task.Title}");
        sb.AppendLine($"Description: {task.Description}");
        sb.AppendLine($"Type: {task.Type}");
        sb.AppendLine();
        
        if (context.RelevantFiles.Any())
        {
            sb.AppendLine("Relevant Files:");
            foreach (var file in context.RelevantFiles)
            {
                sb.AppendLine($"## {file.Path}");
                sb.AppendLine("```");
                sb.AppendLine(file.Content);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }

    private string GetSystemPrompt() => @"
You are an expert coding assistant. Generate precise code changes to solve the given task.

Output format:
For each file change, use this structure:
FILE: path/to/file.cs
```csharp
// Full file content or diff
```

Be concise and only change what's necessary.
";

    private List<CodeChange> ParseCodeChanges(string content)
    {
        var changes = new List<CodeChange>();
        var filePattern = @"FILE:\s*(.+)";
        var codePattern = @"```(\w+)\n(.*?)\n```";
        
        var fileMatches = Regex.Matches(content, filePattern);
        var codeMatches = Regex.Matches(content, codePattern, RegexOptions.Singleline);
        
        for (int i = 0; i < Math.Min(fileMatches.Count, codeMatches.Count); i++)
        {
            changes.Add(new CodeChange
            {
                FilePath = fileMatches[i].Groups[1].Value.Trim(),
                Language = codeMatches[i].Groups[1].Value,
                Content = codeMatches[i].Groups[2].Value
            });
        }
        
        return changes;
    }
}
```

### Example 3: Python ML Classifier (Hybrid)

**FastAPI Main** - `main.py`:
```python
from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import logging
from .api.routes import classification, training, health
from .infrastructure.database import init_db, close_db
from .infrastructure.messaging import start_consumer, stop_consumer
from .domain.classifiers.hybrid import HybridClassifier
from .infrastructure.ml.model_loader import ModelLoader

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Startup and shutdown events"""
    # Startup
    logger.info("Starting ML Classifier Service...")
    await init_db()
    await start_consumer()
    
    # Load ML model
    model_loader = ModelLoader()
    app.state.model = await model_loader.load_latest_model()
    logger.info(f"Loaded model version: {app.state.model.version}")
    
    yield
    
    # Shutdown
    logger.info("Shutting down ML Classifier Service...")
    await stop_consumer()
    await close_db()

app = FastAPI(
    title="ML Classifier Service",
    version="2.0.0",
    lifespan=lifespan
)

# CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Routes
app.include_router(classification.router)
app.include_router(training.router)
app.include_router(health.router)

@app.get("/")
async def root():
    return {
        "service": "ML Classifier",
        "version": "2.0.0",
        "status": "running"
    }
```

**Hybrid Classifier** - `domain/classifiers/hybrid.py`:
```python
from typing import Optional
from ..models.task_type import TaskType, TaskComplexity
from .heuristic import HeuristicClassifier
from .ml_classifier import MLClassifier
from .llm_classifier import LLMClassifier
from ...api.schemas.classification import ClassificationRequest, ClassificationResult
import logging

logger = logging.getLogger(__name__)

class HybridClassifier:
    """
    Three-tier classification system:
    1. Heuristic (fast, 90% accuracy) - 85% of traffic
    2. ML (medium, 95% accuracy) - 14% of traffic  
    3. LLM (slow, 98% accuracy) - 1% of traffic
    """
    
    HEURISTIC_THRESHOLD = 0.85
    ML_THRESHOLD = 0.70
    
    def __init__(
        self,
        heuristic: HeuristicClassifier,
        ml_classifier: MLClassifier,
        llm_classifier: LLMClassifier
    ):
        self.heuristic = heuristic
        self.ml_classifier = ml_classifier
        self.llm_classifier = llm_classifier
        
    async def classify(self, request: ClassificationRequest) -> ClassificationResult:
        """Execute hybrid classification strategy"""
        
        # Phase 1: Try heuristic classification (5ms)
        logger.info(f"Classifying task with heuristic: {request.task_description[:50]}...")
        heuristic_result = self.heuristic.classify(request.task_description)
        
        if heuristic_result.confidence >= self.HEURISTIC_THRESHOLD:
            logger.info(f"Heuristic classification succeeded with confidence {heuristic_result.confidence:.2f}")
            heuristic_result.classifier_used = "heuristic"
            return heuristic_result
        
        # Phase 2: Try ML classification (50ms)
        logger.info(f"Heuristic confidence too low ({heuristic_result.confidence:.2f}), trying ML classifier...")
        ml_result = await self.ml_classifier.classify(request)
        
        if ml_result.confidence >= self.ML_THRESHOLD:
            logger.info(f"ML classification succeeded with confidence {ml_result.confidence:.2f}")
            ml_result.classifier_used = "ml"
            return ml_result
        
        # Phase 3: Fallback to LLM (800ms)
        logger.warning(f"ML confidence too low ({ml_result.confidence:.2f}), using LLM fallback...")
        llm_result = await self.llm_classifier.classify(request)
        llm_result.classifier_used = "llm"
        
        logger.info(f"LLM classification completed with confidence {llm_result.confidence:.2f}")
        return llm_result

    async def classify_with_feedback(
        self,
        request: ClassificationRequest,
        actual_result: Optional[ClassificationResult] = None
    ) -> ClassificationResult:
        """Classify and optionally store feedback for training"""
        
        result = await self.classify(request)
        
        if actual_result:
            # Store feedback for model retraining
            from ...infrastructure.database import get_training_repo
            repo = get_training_repo()
            
            await repo.store_feedback({
                "task_description": request.task_description,
                "predicted_type": result.task_type,
                "predicted_complexity": result.complexity,
                "actual_type": actual_result.task_type,
                "actual_complexity": actual_result.complexity,
                "confidence": result.confidence,
                "classifier_used": result.classifier_used,
                "was_correct": result.task_type == actual_result.task_type
            })
            
        return result
```

**Heuristic Classifier** - `domain/classifiers/heuristic.py`:
```python
from ..models.task_type import TaskType, TaskComplexity
from ...api.schemas.classification import ClassificationResult
import re
from typing import Dict, List

class HeuristicClassifier:
    """Fast keyword-based classification (90% accuracy, 5ms latency)"""
    
    # Keyword patterns for each task type
    KEYWORDS: Dict[TaskType, List[str]] = {
        TaskType.BUG_FIX: [
            r'\bbug\b', r'\berror\b', r'\bfix\b', r'\bcrash\b', 
            r'\bissue\b', r'\bfail(s|ing|ed)?\b', r'\bbroken\b'
        ],
        TaskType.FEATURE: [
            r'\badd\b', r'\bimplement\b', r'\bcreate\b', r'\bnew\b',
            r'\bfeature\b', r'\benhance\b', r'\bsupport\b'
        ],
        TaskType.REFACTOR: [
            r'\brefactor\b', r'\bclean\b', r'\boptimize\b', 
            r'\bimprove\b', r'\breorganize\b', r'\brestructure\b'
        ],
        TaskType.TEST: [
            r'\btest\b', r'\bunit test\b', r'\bintegration test\b',
            r'\bcoverage\b', r'\bspec\b'
        ],
        TaskType.DOCUMENTATION: [
            r'\bdoc(s|umentation)?\b', r'\breadme\b', r'\bcomment\b',
            r'\bexplain\b', r'\bdescribe\b'
        ],
        TaskType.DEPLOYMENT: [
            r'\bdeploy\b', r'\brelease\b', r'\bci/cd\b', r'\bpipeline\b',
            r'\bdocker\b', r'\bkubernetes\b'
        ]
    }
    
    # Complexity indicators
    COMPLEXITY_KEYWORDS = {
        'simple': [
            r'\bsmall\b', r'\bquick\b', r'\bminor\b', r'\btrivial\b',
            r'\btypo\b', r'\bone[ -]line\b'
        ],
        'complex': [
            r'\bcomplex\b', r'\bmajor\b', r'\barchitecture\b', 
            r'\brewrite\b', r'\bmigration\b', r'\brefactor all\b'
        ]
    }
    
    def __init__(self):
        # Compile regex patterns for performance
        self.compiled_keywords = {
            task_type: [re.compile(pattern, re.IGNORECASE) for pattern in patterns]
            for task_type, patterns in self.KEYWORDS.items()
        }
        self.compiled_complexity = {
            level: [re.compile(pattern, re.IGNORECASE) for pattern in patterns]
            for level, patterns in self.COMPLEXITY_KEYWORDS.items()
        }
    
    def classify(self, task_description: str) -> ClassificationResult:
        """Classify task using keyword matching"""
        
        # Count keyword matches per task type
        match_counts = {}
        for task_type, patterns in self.compiled_keywords.items():
            count = sum(1 for pattern in patterns if pattern.search(task_description))
            if count > 0:
                match_counts[task_type] = count
        
        # No matches - default to FEATURE with low confidence
        if not match_counts:
            return ClassificationResult(
                task_type=TaskType.FEATURE,
                complexity=self._classify_complexity(task_description),
                confidence=0.3,
                reasoning="No keyword matches found, defaulting to FEATURE",
                suggested_strategy="Iterative",
                estimated_tokens=2000
            )
        
        # Get task type with most matches
        predicted_type = max(match_counts, key=match_counts.get)
        max_matches = match_counts[predicted_type]
        
        # Calculate confidence based on match count and uniqueness
        total_matches = sum(match_counts.values())
        base_confidence = max_matches / total_matches if total_matches > 0 else 0
        
        # Boost confidence if matches are unique to one type
        if len(match_counts) == 1:
            confidence = min(0.95, base_confidence + 0.2)
        else:
            confidence = min(0.85, base_confidence)
        
        complexity = self._classify_complexity(task_description)
        
        return ClassificationResult(
            task_type=predicted_type,
            complexity=complexity,
            confidence=confidence,
            reasoning=f"Matched {max_matches} keywords for {predicted_type}",
            suggested_strategy=self._suggest_strategy(complexity),
            estimated_tokens=self._estimate_tokens(complexity)
        )
    
    def _classify_complexity(self, description: str) -> TaskComplexity:
        """Classify complexity based on indicators"""
        
        # Check for explicit complexity keywords
        simple_matches = sum(
            1 for pattern in self.compiled_complexity['simple']
            if pattern.search(description)
        )
        complex_matches = sum(
            1 for pattern in self.compiled_complexity['complex']
            if pattern.search(description)
        )
        
        if complex_matches > 0:
            return TaskComplexity.COMPLEX
        elif simple_matches > 0:
            return TaskComplexity.SIMPLE
        
        # Use length as heuristic
        word_count = len(description.split())
        if word_count < 20:
            return TaskComplexity.SIMPLE
        elif word_count > 100:
            return TaskComplexity.COMPLEX
        else:
            return TaskComplexity.MEDIUM
    
    def _suggest_strategy(self, complexity: TaskComplexity) -> str:
        """Suggest execution strategy based on complexity"""
        return {
            TaskComplexity.SIMPLE: "SingleShot",
            TaskComplexity.MEDIUM: "Iterative",
            TaskComplexity.COMPLEX: "MultiAgent"
        }[complexity]
    
    def _estimate_tokens(self, complexity: TaskComplexity) -> int:
        """Estimate token usage based on complexity"""
        return {
            TaskComplexity.SIMPLE: 2000,
            TaskComplexity.MEDIUM: 6000,
            TaskComplexity.COMPLEX: 20000
        }[complexity]
```

## Common Prompts

### Effective Prompts (Copy These)
- "Create the Chat Service following `docs/01-SERVICE-CATALOG.md` section 2. Include DbContext, repository, and SignalR hub."
- "Add a Minimal API endpoint for `/tasks` with FluentValidation and OpenTelemetry spans."
- "Generate Python FastAPI route for ML classification with heuristic → ML → LLM fallback from `04-ML-AND-ORCHESTRATION-ADR.md`."
- "Write integration tests using Testcontainers for the GitHub service PR creation flow."
- "Set up YARP gateway routing to Chat and Orchestration services per `03-SOLUTION-STRUCTURE.md`."

### Chat modes & short commands (LLM‑agnostic)

These concise prompts are designed to work with Copilot Chat (and other assistants) without a long preamble. They follow the guardrails in this file: minimal diffs, ≤5 files at once, tests first, and quiet test output.

#### General rules for the assistant (apply to all modes)
- Prefer repo facts from `docs/` and this file over assumptions.
- Never change unrelated files. Keep edits surgical and explain rationale in-line.
- For tests: use `dotnet test --settings .runsettings --no-build --verbosity quiet --nologo` and filter when possible.
- If a log/secret is required and not accessible, explicitly ask me to paste the last 100 lines with timestamps.

#### Mode: repo-help (Q&A about this monorepo)
- Prompt: "repo: help <topic>"
- Behavior: cite sections/anchors from `docs/*.md` and `.github/copilot-instructions.md`. Include the exact file paths for any referenced artifacts.
- Example: `repo: help solution structure`

#### Mode: build
- Prompt: `build: all` or `build: <service>`
- Behavior: show the exact commands; prefer solution builds for .NET and point to service sln where applicable. Do not run with high verbosity. If errors occur, show first/last 30 lines and a concise diagnosis.
- Examples:
    - `build: all` → `dotnet build CodingAgent.sln --no-restore`
    - `build: chat` → `dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln --no-restore`

#### Mode: test
- Prompts:
    - `test: unit` → run fast tests only
    - `test: integration` → Testcontainers flows
    - `test: service <name>` → run tests for a specific service
- Behavior: always use quiet/nologo, show a short summary (pass/fail counts), then list only failing tests with their error message and the top stack frame. Offer the single smallest fix and required file edits.

#### Mode: ci-fix (GitHub Actions)
- Prompt: `ci: fix pr <number>`
- Behavior: summarize failing jobs, identify the first actionable error, propose a minimal patch (docs, workflow YAML, or code) and the commit message. If logs can’t be fetched, request the failing step’s last 100 lines.
- Example: `ci: fix pr 167`

#### Mode: git-ops (safe, explicit)
- Prompts:
    - `git: sync master` → abort merge, fetch, hard-reset to `origin/master` (destructive; confirm first)
    - `git: discard local` → explain and propose `git restore .` or `git reset --hard` based on status
    - `git: commit "<msg>"` → craft Conventional Commit message and show staged file list expectation
- Behavior: show commands and a reversible alternative when destructive.

#### Mode: scaffold
- Prompt: `scaffold: service <Name>`
- Behavior: create the directories and starter files per `docs/03-SOLUTION-STRUCTURE.md` and the per‑service pattern (Program.cs, Domain, Application, Infrastructure, Api). Include unit test skeletons with `[Trait("Category", "Unit")]`.

#### Mode: endpoint
- Prompt: `endpoint: <service> <method> <route>`
- Behavior: add Minimal API endpoint with FluentValidation, OpenTelemetry spans, and tests. Example: `endpoint: chat POST /conversations`

#### Mode: docs
- Prompt: `docs: update <area>`
- Behavior: edit only the relevant doc(s), cross‑link to ADRs, and add a brief changelog note.

#### Mode: observability
- Prompt: `otel: wire <service>`
- Behavior: add tracing + metrics (AspNetCore, HttpClient, EF) and Prometheus exporter per examples in this file.

#### Mode: gemini (external assistant profile)
- Prompt: `gemini: <task>`
- Behavior: respond with a self‑contained prompt pack tailored for Gemini 2.5 Pro, referencing `.github/chatmodes/gemini-mode.md` as the system profile. Include only the minimal task-specific additions the user should paste under “User” while the system prompt comes from that file.

#### Mode: pr-review
- Prompt: `pr: review <number>`
- Behavior: perform a completeness review against repo rules: scope/surgical diffs, build/test status (quiet), validation/observability present, docs updated, Conventional Commits, and produce a concise review summary with 1–3 high‑impact line comments per file.

#### Mode: pr-address-comments
- Prompt: `pr: address-comments <number>`
- Behavior: classify comments (bug/test/security/perf/style/docs/arch), validate using repo rubric, then propose or produce minimal code/test/doc patches; provide fixup commit message(s) and rerun quiet tests.

---

### One‑liners you can paste in chat
- `repo: help roadmap`
- `build: all`
- `test: unit`
- `test: integration`
- `ci: fix pr 167`
- `git: sync master`
- `scaffold: service GitHub`
- `endpoint: chat POST /conversations`
- `docs: update testing verbosity guidance`
- `gemini: summarize failing CI for PR 167 and propose minimal patch`
- `pr: review 167`
- `pr: address-comments 167`

> Note: Model selection (e.g., Gemini vs GPT) is controlled by the provider. These prompts are model‑agnostic and tuned for this repository.

### Anti-Patterns (Avoid)
- ❌ "Build the entire system" → Too broad, split into services
- ❌ "Add error handling" → Generic, specify scenarios (network timeout, validation failure)
- ❌ "Make it production-ready" → Vague, use checklist (observability, tests, docs)

## Deployment & Operations

### Docker Compose (Development)
```yaml
# deployment/docker-compose/docker-compose.dev.yml
services:
  gateway:
    build: ../../src/Gateway
    ports: ["5000:5000"]
    depends_on: [postgres, redis, rabbitmq]
  
  chat-service:
    build: ../../src/Services/Chat
    environment:
      - ConnectionStrings__ChatDb=Host=postgres;Database=coding_agent;Username=dev
      - Redis__Connection=redis:6379
```

### CI/CD (Per-Service Pipelines)
```yaml
# .github/workflows/chat-service.yml
name: Chat Service CI
on:
  push:
    paths: ['src/Services/Chat/**', 'src/SharedKernel/**']
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln
      - run: dotnet test --filter Category=Unit
      - run: dotnet test --filter Category=Integration  # Testcontainers
```

## Key Decision Records (ADRs)

When making design choices, reference these ADRs:
- **ADR-001**: Microservices over monolith (scalability, deployment independence)
- **ADR-002**: YARP over Nginx (native .NET, simpler config)
- **ADR-003**: PostgreSQL schemas over separate DBs (Phase 1 simplicity, migrate Phase 2)
- **ADR-004**: Hybrid ML classification (cost/latency/accuracy tradeoff)
- **ADR-005**: MassTransit + RabbitMQ over Kafka (easier setup, good enough throughput)

## Security Checklist

- [ ] JWT authentication at Gateway (propagate to services via headers)
- [ ] Never log secrets (use `[Sensitive]` attribute or filter patterns)
- [ ] Validate all inputs with FluentValidation
- [ ] Use prepared statements for SQL (EF Core default)
- [ ] Enable CORS with explicit origins (no `*` in prod)
- [ ] Rate limit per user (1000 req/hour) and per IP (100 req/min)

## Final Checks Before Committing

1. **Lint passes**: `dotnet format`, `ruff check .`, `ng lint`
2. **Tests pass**: `dotnet test`, `pytest`, `npm test`
3. **Coverage ≥85%**: Check CI output
4. **Docs updated**: If API changed, update OpenAPI spec + relevant `docs/*.md`
5. **Conventional Commit**: `feat(chat): add SignalR typing indicators`

## When You're Stuck

1. **Re-read docs**: Check `docs/01-SERVICE-CATALOG.md` for the service you're building
2. **Review examples**: Look at similar .NET microservices (eShopOnContainers, DAPR samples)
3. **Ask specific questions**: "How do I implement retry policy with Polly for HTTP calls to ML service?"
4. **Check roadmap**: Ensure you're on the right phase (`docs/02-IMPLEMENTATION-ROADMAP.md`)

---

**Remember**: This is a greenfield project. No code exists yet. Always start by creating the directory structure from `03-SOLUTION-STRUCTURE.md`, then scaffold projects following the service architecture pattern above.
