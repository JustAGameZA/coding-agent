using CodingAgent.Services.Orchestration.Api.Endpoints;
using CodingAgent.Services.Orchestration.Domain.Repositories;
using CodingAgent.Services.Orchestration.Domain.Services;
using CodingAgent.Services.Orchestration.Domain.Strategies;
using CodingAgent.Services.Orchestration.Infrastructure.ExternalServices;
using CodingAgent.Services.Orchestration.Infrastructure.LLM;
using CodingAgent.Services.Orchestration.Infrastructure.Logging;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence;
using CodingAgent.Services.Orchestration.Infrastructure.Persistence.Repositories;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Infrastructure;
using CodingAgent.SharedKernel.Infrastructure.Messaging;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;

var builder = WebApplication.CreateBuilder(args);

// Database configuration with in-memory fallback for dev/test
var connectionString = builder.Configuration.GetConnectionString("OrchestrationDb");

builder.Services.AddDbContext<OrchestrationDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("OrchestrationDb");
    }
});

// Register repositories
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IExecutionRepository, ExecutionRepository>();

// Register domain services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IExecutionCoordinator, ExecutionCoordinator>();
builder.Services.AddSingleton<IExecutionLogService, ExecutionLogService>();

// Register agentic AI services (optional - can be null if not configured)
builder.Services.AddScoped<IReflectionService, ReflectionService>();
builder.Services.AddScoped<IPlanningService, PlanningService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

// Register validators
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();
builder.Services.AddScoped<IValidator<ExecuteTaskRequest>, ExecuteTaskRequestValidator>();

// Configure rate limiting - 10 executions per hour per user
builder.Services.AddRateLimiter(options =>
{
    // Default user ID constant - TODO: Replace with authenticated user from JWT
    const string DefaultUserId = "00000000-0000-0000-0000-000000000001";

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Get user ID from context (for now using a default, will be replaced with actual auth)
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault()
            ?? DefaultUserId;

        // Only apply rate limiting to execution endpoints
        if (context.Request.Path.StartsWithSegments("/tasks") &&
            context.Request.Path.Value?.Contains("/execute") == true)
        {
            return RateLimitPartition.GetFixedWindowLimiter(userId, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromHours(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0 // No queueing
                });
        }

        // No rate limit for other endpoints
        return RateLimitPartition.GetNoLimiter<string>(string.Empty);
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Maximum 10 task executions per hour allowed.",
            cancellationToken: token);
    };
});

// Messaging configuration: enable MassTransit only when explicitly enabled and configured
var messagingEnabled = builder.Configuration.GetValue<bool?>("Messaging:Enabled") ?? false;
var rabbitConfigured = builder.Configuration.IsRabbitMQConfigured();

if (messagingEnabled && rabbitConfigured)
{
    builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
}
else
{
    // Fallback to no-op publisher for local/dev/test where RabbitMQ isn't available
    builder.Services.AddScoped<IEventPublisher, NoOpEventPublisher>();
}

// Service metadata for telemetry
var serviceName = "CodingAgent.Services.Orchestration";
var serviceVersion = "2.0.0";

// Register execution strategies and dependencies
// TODO: Replace mock implementations with real LLM and validator implementations in future phases
builder.Services.AddScoped<ILlmClient, MockLlmClient>();
builder.Services.AddScoped<ICodeValidator, MockCodeValidator>();

// Register ActivitySource for distributed tracing
builder.Services.AddSingleton(_ => new System.Diagnostics.ActivitySource(serviceName));

// Register agents for MultiAgent strategy
builder.Services.AddScoped<IPlannerAgent, PlannerAgent>();
builder.Services.AddScoped<ICoderAgent, CoderAgent>();
builder.Services.AddScoped<IReviewerAgent, ReviewerAgent>();
builder.Services.AddScoped<ITesterAgent, TesterAgent>();

// Register execution strategies
builder.Services.AddScoped<IExecutionStrategy, SingleShotStrategy>();
builder.Services.AddScoped<IExecutionStrategy, IterativeStrategy>();
builder.Services.AddScoped<IExecutionStrategy, MultiAgentStrategy>();

// Register ML Classifier HTTP client with retry policy
builder.Services.AddHttpClient<IMLClassifierClient, MLClassifierClient>(client =>
{
    var mlClassifierBaseUrl = builder.Configuration["MLClassifier:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(mlClassifierBaseUrl);
    var timeoutMs = builder.Configuration.GetValue<int?>("MLClassifier:TimeoutMs")
        ?? (builder.Environment.IsProduction() ? 300 : 1000);
    client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Register GitHub service HTTP client with retry policy
builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
{
    var githubServiceUrl = builder.Configuration["GitHub:ServiceUrl"] ?? "http://localhost:5004";
    client.BaseAddress = new Uri(githubServiceUrl);
    var timeoutMs = builder.Configuration.GetValue<int?>("GitHub:TimeoutMs")
        ?? (builder.Environment.IsProduction() ? 5000 : 10000);
    client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Register Strategy Selector
builder.Services.AddScoped<IStrategySelector, StrategySelector>();

// Bind GitHub repository configuration (used by TaskService for PR creation)
builder.Services.Configure<GitHubRepositoryOptions>(builder.Configuration.GetSection("GitHub:Repository"));

// Health checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrchestrationDbContext>();

if (!string.IsNullOrWhiteSpace(connectionString))
{
    healthChecksBuilder.AddNpgSql(
        connectionString,
        name: "postgresql",
        tags: new[] { "db", "ready" });
}

// RabbitMQ health check if configured (only in Production to avoid dev package mismatches)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

// OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            // Default OTLP endpoint (will be configured via appsettings for production)
            options.Endpoint = new Uri(
                builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// MassTransit + RabbitMQ with event publishing configuration (enabled only when configured)
if (messagingEnabled && rabbitConfigured)
{
    builder.Services.AddMassTransit(x =>
    {
        x.SetKebabCaseEndpointNameFormatter();
        x.AddConsumers(typeof(Program).Assembly);

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.ConfigureEventPublishingForRabbitMq(builder.Configuration, builder.Environment);
            cfg.ConfigureEndpoints(context);
        });
    });
}

// API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "orchestration";
    config.Title = "Orchestration Service API";
    config.Version = "v2.0";
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "Orchestration Service API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
    });
}

// Enable rate limiting
app.UseRateLimiter();

// Map endpoints
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapTaskEndpoints();
app.MapEventTestEndpoints();
app.MapFeedbackEndpoints();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Orchestration Service",
    version = "2.0.0",
    documentation = "/swagger"
}))
.WithName("Root")
.ExcludeFromDescription();

// Apply EF Core migrations on startup when using a relational provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
    await db.MigrateDatabaseIfRelationalAsync(
        app.Logger,
        isProduction: app.Environment.IsProduction());
}

await app.RunAsync();

// Make Program accessible to tests and host Polly policy helpers
public partial class Program
{
    // Prevent instantiation
    protected Program() { }

    // Polly policies for ML Classifier HTTP client
    internal static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(50));
    }

    internal static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
