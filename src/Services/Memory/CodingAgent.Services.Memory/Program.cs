using CodingAgent.Services.Memory.Api.Endpoints;
using CodingAgent.Services.Memory.Application.Services;
using CodingAgent.Services.Memory.Domain.Repositories;
using CodingAgent.Services.Memory.Domain.Services;
using CodingAgent.Services.Memory.Infrastructure.Persistence;
using CodingAgent.Services.Memory.Infrastructure.Services;
using CodingAgent.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<MemoryDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MemoryDb");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("MemoryDb connection string is required in Production");
    }
    else
    {
        options.UseInMemoryDatabase("MemoryDb");
    }
});

// Repository registration
builder.Services.AddScoped<IEpisodeRepository, EpisodeRepository>();
builder.Services.AddScoped<ISemanticMemoryRepository, SemanticMemoryRepository>();
builder.Services.AddScoped<IProcedureRepository, ProcedureRepository>();

// Service registration
builder.Services.AddScoped<IMemoryService, MemoryService>();
builder.Services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();

// HTTP client for Ollama
builder.Services.AddHttpClient("ollama", client =>
{
    var ollamaUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://ollama-service:5008";
    client.BaseAddress = new Uri(ollamaUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is required");
var jwtKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodingAgent",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CodingAgent.API",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// OpenTelemetry
var serviceName = "CodingAgent.Memory";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource(serviceName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MemoryDbContext>();

var app = builder.Build();

// Apply migrations in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MemoryDbContext>();
    
    if (dbContext.Database.IsRelational())
    {
        try
        {
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to apply migrations. Database may not exist yet.");
        }
    }
}

// Middleware
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapMemoryEndpoints();
app.MapHealthChecks("/health");

app.Run();

