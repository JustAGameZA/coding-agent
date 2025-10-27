using System.Diagnostics;
using CodingAgent.Services.Dashboard.Api.Endpoints;
using CodingAgent.Services.Dashboard.Application.Services;
using CodingAgent.Services.Dashboard.Domain.Services;
using CodingAgent.Services.Dashboard.Infrastructure.Caching;
using CodingAgent.Services.Dashboard.Infrastructure.ExternalServices;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ActivitySource for custom tracing
var activitySource = new ActivitySource("CodingAgent.Services.Dashboard");
builder.Services.AddSingleton(activitySource);

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
});

// Caching service
builder.Services.AddSingleton<IDashboardCacheService, DashboardCacheService>();

// HTTP clients with Polly retry policies
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<ChatServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Chat:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<OrchestrationServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Orchestration:BaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddPolicyHandler(retryPolicy);

// Domain services
builder.Services.AddScoped<IDashboardAggregationService, DashboardAggregationService>();

// CORS (allow requests from Angular frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Correlation-Id", "X-RateLimit-Limit", "X-RateLimit-Remaining")
            .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration["Redis:Connection"] ?? "localhost:6379");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("CodingAgent.Services.Dashboard", serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("CodingAgent.Services.Dashboard")
        .AddOtlpExporter(options =>
        {
            var endpoint = builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://jaeger:4317";
            options.Endpoint = new Uri(endpoint);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// Apply CORS policy
app.UseCors();

// Warm cache on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var aggregationService = scope.ServiceProvider.GetRequiredService<IDashboardAggregationService>();
    
    logger.LogInformation("Warming dashboard cache on startup...");
    try
    {
        _ = await aggregationService.GetStatsAsync();
        logger.LogInformation("Dashboard cache warmed successfully");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to warm dashboard cache, will populate on first request");
    }
}

// Map health endpoint
app.MapHealthChecks("/health");

// Map metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Map dashboard endpoints
app.MapDashboardEndpoints();

// Ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }))
    .WithName("Ping")
    .WithOpenApi();

app.Run();
