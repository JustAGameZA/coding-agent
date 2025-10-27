using CodingAgent.Services.Chat.Api.Endpoints;
using CodingAgent.Services.Chat.Api.Hubs;
using CodingAgent.Services.Chat.Domain.Repositories;
using CodingAgent.Services.Chat.Domain.Services;
using CodingAgent.Services.Chat.Infrastructure.Caching;
using CodingAgent.Services.Chat.Infrastructure.Persistence;
using CodingAgent.Services.Chat.Infrastructure.Presence;
using CodingAgent.Services.Chat.Infrastructure.Storage;
using CodingAgent.SharedKernel.Infrastructure;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Diagnostics.Metrics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ChatDb");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        // Use PostgreSQL when a connection string is explicitly provided
        options.UseNpgsql(connectionString);
    }
    else if (builder.Environment.IsProduction())
    {
        // In Production, require explicit configuration
        throw new InvalidOperationException("ChatDb connection string is required in Production");
    }
    else
    {
        // Default to in-memory for development/testing when not configured
        options.UseInMemoryDatabase("ChatDb");
    }
});

// Repository registration
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

// Redis Cache (optional - skip if not configured)
var redisConnection = builder.Configuration["Redis:Connection"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });

    // Register Redis connection multiplexer for message caching
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnection));
}

// Register message cache service (accepts null connection when Redis is not configured)
builder.Services.AddScoped<IMessageCacheService>(sp =>
{
    var redis = sp.GetService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<MessageCacheService>>();
    var meterFactory = sp.GetRequiredService<IMeterFactory>();
    return new MessageCacheService(redis, logger, meterFactory);
});

// Register presence service (accepts null connection when Redis is not configured)
builder.Services.AddScoped<IPresenceService>(sp =>
{
    var redis = sp.GetService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<PresenceService>>();
    var meterFactory = sp.GetRequiredService<IMeterFactory>();
    return new PresenceService(redis, logger, meterFactory);
});

// File storage service
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodingAgent";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CodingAgent.API";

if (!string.IsNullOrEmpty(jwtSecret))
{
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Configure SignalR to accept JWT token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // If the request is for SignalR hub and token is in query string
                if (!string.IsNullOrEmpty(accessToken) && 
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
}
else
{
    // No JWT configured - allow anonymous access (development only)
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("JWT:Secret is required in Production environment");
    }
}

// SignalR
builder.Services.AddSignalR();

// FluentValidation - Register all validators from the Assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
        cfg.ConfigureEndpoints(context);
    });
});

// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<ChatDbContext>();

// Add Redis health check if configured
if (!string.IsNullOrEmpty(redisConnection))
{
    healthChecksBuilder.AddRedis(redisConnection, name: "redis");
}

// RabbitMQ health check if configured (only in Production to avoid dev package mismatches)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

// OpenTelemetry - Tracing and Metrics
var serviceName = "chat-service";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource(serviceName)
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

// CORS (for development)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseCors();

// Enable authentication and authorization
if (!string.IsNullOrEmpty(jwtSecret))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Map endpoints
app.MapGet("/ping", () => Results.Ok(new { status = "healthy", service = "chat-service", timestamp = DateTime.UtcNow }))
    .WithName("Ping")
    .WithTags("Health")
    .Produces(StatusCodes.Status200OK);

// Map endpoints
app.MapConversationEndpoints();
app.MapAttachmentEndpoints();
app.MapFileEndpoints();
app.MapPresenceEndpoints();
app.MapEventTestEndpoints();

// SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Apply EF Core migrations on startup when using a relational provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await db.MigrateDatabaseIfRelationalAsync(
        app.Logger,
        isProduction: app.Environment.IsProduction());
}

await app.RunAsync();

// Expose Program class for WebApplicationFactory in tests
public partial class Program
{
    // Prevent instantiation
    protected Program() { }
}
