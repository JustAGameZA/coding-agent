using CodingAgent.Services.Ollama.Domain.Services;
using CodingAgent.Services.Ollama.Infrastructure.Http;
using CodingAgent.Services.Ollama.Infrastructure.CloudApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTP client for Ollama backend
builder.Services.AddHttpClient<IOllamaHttpClient, OllamaHttpClient>(client =>
{
    var ollamaUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(ollamaUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // Allow long-running inference
});

// Cloud API Configuration
builder.Services.Configure<CloudApiOptions>(
    builder.Configuration.GetSection(CloudApiOptions.SectionName));

// Domain Services
builder.Services.AddScoped<IHardwareDetector, HardwareDetector>();
builder.Services.AddSingleton<ITokenUsageTracker, InMemoryTokenUsageTracker>();
builder.Services.AddSingleton<ICloudApiClient, MockCloudApiClient>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("ollama-service"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Validate Cloud API configuration on startup
using (var scope = app.Services.CreateScope())
{
    var cloudApiClient = scope.ServiceProvider.GetRequiredService<ICloudApiClient>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Startup");

    if (cloudApiClient.IsConfigured())
    {
        logger.LogInformation("Cloud API is configured and will be used as fallback");
    }
    else
    {
        logger.LogInformation("Cloud API not configured - only Ollama backend will be used");
    }
}

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Map health endpoints
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

// Map basic endpoints
app.MapGet("/", () => new
{
    Service = "Ollama Service",
    Version = "1.0.0",
    Status = "Running"
});

// Hardware detection endpoint
app.MapGet("/api/hardware", async (IHardwareDetector hardwareDetector) =>
{
    var profile = await hardwareDetector.DetectHardwareAsync();
    return Results.Ok(profile);
})
.WithName("DetectHardware")
.WithOpenApi();

// Recommended models endpoint
app.MapPost("/api/hardware/models", async (IHardwareDetector hardwareDetector) =>
{
    var profile = await hardwareDetector.DetectHardwareAsync();
    var models = await hardwareDetector.DetermineInitialModelsAsync(profile);
    return Results.Ok(new
    {
        Hardware = profile,
        RecommendedModels = models
    });
})
.WithName("GetRecommendedModels")
.WithOpenApi();

app.Run();

// Make Program partial for testing
public partial class Program { }
