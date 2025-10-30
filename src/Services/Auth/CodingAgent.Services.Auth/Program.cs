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
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.IdentityModel.Tokens.Jwt;

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
        options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "auth"))
               .UseSnakeCaseNamingConvention();
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

// JWT Authentication (support both Jwt:* and Authentication:Jwt:*)
// Prefer Authentication:Jwt:* when present (matches gateway/docker-compose), otherwise use Jwt:*
var authJwt = builder.Configuration.GetSection("Authentication:Jwt");
if (!string.IsNullOrWhiteSpace(authJwt["SecretKey"]))
{
    var jwtSecret = authJwt["SecretKey"];
    var jwtIssuer = !string.IsNullOrWhiteSpace(authJwt["Issuer"]) ? authJwt["Issuer"] : "CodingAgent";
    var jwtAudience = !string.IsNullOrWhiteSpace(authJwt["Audience"]) ? authJwt["Audience"] : "CodingAgent.API";

    Log.Information("[Auth] Configuring JWT (Authentication:Jwt). Issuer={Issuer}, Audience={Audience}, SecretSet={SecretSet}", jwtIssuer, jwtAudience, !string.IsNullOrWhiteSpace(jwtSecret));

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret!)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Temporary diagnostic logging for token validation issues
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(context.Exception, "JWT authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                logger.LogInformation("JWT token validated for sub {Sub}", sub);
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                // Log presence of Authorization header (without token content)
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var hasAuth = context.Request.Headers.ContainsKey("Authorization");
                logger.LogDebug("Authorization header present: {HasAuth}", hasAuth);
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
}
else
{
    string? jwtSecret = builder.Configuration["Jwt:Secret"];
    string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
    if (string.IsNullOrWhiteSpace(jwtIssuer))
    {
        jwtIssuer = "CodingAgent";
    }
    string? jwtAudience = builder.Configuration["Jwt:Audience"];
    if (string.IsNullOrWhiteSpace(jwtAudience))
    {
        jwtAudience = "CodingAgent.API";
    }

    if (!string.IsNullOrWhiteSpace(jwtSecret))
    {
        Log.Information("[Auth] Configuring JWT (Jwt:* fallback). Issuer={Issuer}, Audience={Audience}, SecretSet={SecretSet}", jwtIssuer, jwtAudience, !string.IsNullOrWhiteSpace(jwtSecret));
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret!)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(context.Exception, "JWT authentication failed (fallback config): {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    logger.LogInformation("JWT token validated (fallback config) for sub {Sub}", sub);
                    return Task.CompletedTask;
                }
            };
        });

        builder.Services.AddAuthorization();
    }
    else if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("JWT:Secret is required in Production environment");
    }
}

// (JWT configuration done above)

// MassTransit Transport
// Use RabbitMQ only when fully configured; otherwise fall back to in-memory transport (ideal for tests & local dev without a broker)
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(Program).Assembly);

    // Enable RabbitMQ only when explicitly enabled or in Production
    var rabbitEnabled = builder.Configuration.GetValue<bool?>("RabbitMQ:Enabled")
        ?? builder.Environment.IsProduction();
    var rabbitConfigured = builder.Configuration.IsRabbitMQConfigured();

    if (rabbitEnabled && rabbitConfigured)
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.ConfigureRabbitMQHost(builder.Configuration, builder.Environment);
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
    }
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
// Enable authentication/authorization if configured above
app.UseAuthentication();
app.UseAuthorization();

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

// (debug endpoint removed after troubleshooting)

// Auth endpoints
app.MapAuthEndpoints();

// Admin endpoints
app.MapAdminEndpoints();

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
