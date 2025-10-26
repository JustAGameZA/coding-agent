using CodingAgent.Services.CICDMonitor.Api.Endpoints;
using CodingAgent.Services.CICDMonitor.Application.Services;
using CodingAgent.Services.CICDMonitor.Domain.Repositories;
using CodingAgent.Services.CICDMonitor.Infrastructure.GitHub;
using CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;
using CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;
using CodingAgent.SharedKernel.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Octokit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add ActivitySource for tracing
var activitySource = new ActivitySource("CodingAgent.Services.CICDMonitor");
builder.Services.AddSingleton(activitySource);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "CodingAgent.Services.CICDMonitor",
            serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("CodingAgent.Services.CICDMonitor")
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

// Database - PostgreSQL
builder.Services.AddDbContext<CICDMonitorDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("CICDMonitorDb");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
});

// GitHub Client
builder.Services.AddSingleton<IGitHubClient>(sp =>
{
    var githubToken = builder.Configuration["GitHub:Token"];
    var client = new GitHubClient(new ProductHeaderValue("CodingAgent-CICDMonitor"));
    
    if (!string.IsNullOrEmpty(githubToken))
    {
        client.Credentials = new Credentials(githubToken);
    }
    
    return client;
});

// Repositories
builder.Services.AddScoped<IBuildRepository, BuildRepository>();

// Services
builder.Services.AddSingleton<IGitHubActionsClient, GitHubActionsClient>();

// Background Services
builder.Services.AddHostedService<BuildMonitor>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCompletedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
        cfg.ConfigureEndpoints(context);
    });
});

// Add health checks
var healthChecksBuilder = builder.Services.AddHealthChecks();

// Database health check
var connectionString = builder.Configuration.GetConnectionString("CICDMonitorDb");
if (!string.IsNullOrEmpty(connectionString))
{
    healthChecksBuilder.AddNpgSql(connectionString, name: "postgresql");
}

// RabbitMQ health check if configured (only in Production to avoid dev package mismatches)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

var app = builder.Build();

// Optionally run migrations in development when explicitly enabled
var runMigrations = app.Configuration.GetValue<bool?>("RunMigrationsOnStartup") ?? false;
if (app.Environment.IsDevelopment() && runMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CICDMonitorDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Map API endpoints
app.MapBuildEndpoints();

// Map health endpoint
app.MapHealthChecks("/health");

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
