using CodingAgent.Services.Browser.Api.Endpoints;
using CodingAgent.Services.Browser.Api.Validators;
using CodingAgent.Services.Browser.Domain.Configuration;
using CodingAgent.Services.Browser.Domain.Services;
using CodingAgent.Services.Browser.Infrastructure.Browser;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<BrowserOptions>(
    builder.Configuration.GetSection(BrowserOptions.SectionName));

// Browser services
builder.Services.AddSingleton<IBrowserPool, BrowserPool>();
builder.Services.AddScoped<IBrowserService, PlaywrightBrowserService>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<BrowseRequestValidator>();

// Health checks
builder.Services.AddHealthChecks();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("BrowserService", serviceVersion: "2.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("BrowserService")
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

// Health endpoint
app.MapHealthChecks("/health");

// Ping endpoint
app.MapGet("/ping", () => Results.Ok(new
{
    service = "BrowserService",
    status = "healthy",
    version = "2.0.0",
    timestamp = DateTime.UtcNow
}))
.WithName("Ping")
.WithTags("Health")
.Produces(StatusCodes.Status200OK);

// Browser endpoints
app.MapBrowserEndpoints();

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.Run();

// Make Program accessible to test projects
public partial class Program { }
