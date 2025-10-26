using CodingAgent.Services.CICDMonitor.Api.Endpoints;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.Services.CICDMonitor.Domain.Services.Implementation;
using CodingAgent.Services.CICDMonitor.Infrastructure.ExternalServices;
using CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;
using CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;
using CodingAgent.SharedKernel.Abstractions;
using CodingAgent.SharedKernel.Infrastructure;
using CodingAgent.SharedKernel.Infrastructure.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add Database
builder.Services.AddDbContext<CICDMonitorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CICDMonitorDb")));

// Add repositories
builder.Services.AddScoped<IBuildFailureRepository, BuildFailureRepository>();
builder.Services.AddScoped<IFixAttemptRepository, FixAttemptRepository>();

// Add domain services
builder.Services.AddScoped<IAutomatedFixService, AutomatedFixService>();

// Add HTTP clients for external services
builder.Services.AddHttpClient<IOrchestrationClient, OrchestrationClient>(client =>
{
    var orchestrationUrl = builder.Configuration["ExternalServices:Orchestration:BaseUrl"] 
        ?? "http://orchestration-service:5003";
    client.BaseAddress = new Uri(orchestrationUrl);
});

builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
{
    var githubUrl = builder.Configuration["ExternalServices:GitHub:BaseUrl"] 
        ?? "http://github-service:5004";
    client.BaseAddress = new Uri(githubUrl);
});

// Add event publisher
if (builder.Environment.IsProduction() || builder.Environment.EnvironmentName == "Docker")
{
    builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
}
else
{
    builder.Services.AddScoped<IEventPublisher, NoOpEventPublisher>();
}

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "CodingAgent.Services.CICDMonitor",
            serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<TaskCompletedEventConsumer>();
    x.AddConsumer<BuildFailedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();

// PostgreSQL health check
var connectionString = builder.Configuration.GetConnectionString("CICDMonitorDb");
if (!string.IsNullOrEmpty(connectionString))
{
    healthChecksBuilder.AddNpgSql(connectionString, name: "cicd_monitor_db");
}

// RabbitMQ health check if configured (only in Production to avoid dev package mismatches)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

var app = builder.Build();

// Run migrations automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CICDMonitorDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Map health endpoint
app.MapHealthChecks("/health");

// Map API endpoints
app.MapFixStatisticsEndpoints();

// Map ping endpoint
app.MapGet("/ping", () => Results.Ok(new
{
    service = "CodingAgent.Services.CICDMonitor",
    version = "2.0.0",
    status = "healthy",
    timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("Health");

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

await app.RunAsync();
