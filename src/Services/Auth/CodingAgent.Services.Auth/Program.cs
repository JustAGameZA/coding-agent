using CodingAgent.Services.Auth.Api.Endpoints;
using CodingAgent.Services.Auth.Application.Services;
using CodingAgent.Services.Auth.Domain.Repositories;
using CodingAgent.Services.Auth.Infrastructure.Persistence;
using CodingAgent.Services.Auth.Infrastructure.Security;
using CodingAgent.SharedKernel.Infrastructure;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "auth-service")
    .CreateLogger();

builder.Host.UseSerilog();

// Database configuration
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AuthDb");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("AuthDb connection string is required in Production");
    }
    else
    {
        options.UseInMemoryDatabase("AuthDb");
    }
});

// Repository registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();

// Security services
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();

// FluentValidation - Register all validators from the Assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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
    });

    builder.Services.AddAuthorization();
}
else if (builder.Environment.IsProduction())
{
    throw new InvalidOperationException("JWT:Secret is required in Production environment");
}

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
    .AddDbContextCheck<AuthDbContext>();

// RabbitMQ health check if configured (only in Production)
if (builder.Environment.IsProduction())
{
    healthChecksBuilder.AddRabbitMQHealthCheckIfConfigured(builder.Configuration);
}

// OpenTelemetry - Tracing and Metrics
var serviceName = "auth-service";
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
app.UseSerilogRequestLogging();

// Enable authentication and authorization
if (!string.IsNullOrEmpty(jwtSecret))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Map endpoints
app.MapGet("/ping", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "auth-service", 
    timestamp = DateTime.UtcNow 
}))
    .WithName("Ping")
    .WithTags("Health")
    .Produces(StatusCodes.Status200OK)
    .AllowAnonymous();

// Auth endpoints
app.MapAuthEndpoints();

// Health checks
app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Apply EF Core migrations on startup when using a relational provider
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.MigrateDatabaseIfRelationalAsync(
        app.Logger,
        isProduction: app.Environment.IsProduction());
}

await app.RunAsync();

// Expose Program class for WebApplicationFactory in tests
public partial class Program
{
    protected Program() { }
}
