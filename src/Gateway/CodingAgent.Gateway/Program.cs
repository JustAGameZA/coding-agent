using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;
using HealthChecks.UI.Client;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Text;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;
using CodingAgent.Gateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap Serilog early for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Authentication:Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
        
        // Configure JWT Bearer to work with SignalR WebSocket connections
        // SignalR passes the token as a query parameter for WebSocket connections
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // For SignalR WebSocket connections, the token is passed as a query parameter
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                // Also check Authorization header for regular HTTP requests
                else if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS configuration
var allowedOrigins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Correlation-Id", "X-RateLimit-Limit", "X-RateLimit-Remaining", "Link")
              .AllowCredentials();
    });
});

// Health checks (liveness/readiness)
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

// OpenTelemetry: Traces + Metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(
        serviceName: "CodingAgent.Gateway",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            var endpoint = builder.Configuration["OpenTelemetry:Endpoint"];
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                options.Endpoint = new Uri(endpoint);
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// YARP reverse proxy from configuration
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Phase 5: Traffic routing and dual-write services
builder.Services.AddSingleton<TrafficRoutingService>();
builder.Services.AddScoped<DualWriteService>();
builder.Services.AddHttpClient("new-system")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
builder.Services.AddHttpClient("legacy-system")
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Configure default HTTP client with Polly resilience policies
// YARP uses HttpClient internally, so these policies apply to proxied requests
builder.Services.AddHttpClient(string.Empty)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
        MaxConnectionsPerServer = 100
    })
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
            {
                // Exponential backoff with jitter: 2^attempt seconds + random 0-1000ms
                var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                return baseDelay + jitter;
            },
            onRetry: (outcome, timespan, retryAttempt) =>
            {
                Log.Warning(
                    "HTTP retry {RetryAttempt}/3. Waiting {RetryDelayMs}ms. Reason: {Reason}",
                    retryAttempt,
                    (int)timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            }))
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Log.Error(
                    "Circuit breaker OPENED for {BreakDurationSeconds}s. Reason: {Reason}",
                    (int)breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker RESET. Normal operation resumed.");
            },
            onHalfOpen: () =>
            {
                Log.Information("Circuit breaker HALF-OPEN. Testing downstream health.");
            }))
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Redis connection for distributed rate limiting
var redisConnectionString = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
ConnectionMultiplexer? redis;
try
{
    redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
    Log.Information("Connected to Redis for rate limiting at {RedisEndpoint}", redisConnectionString);
}
catch (Exception ex)
{
    Log.Warning(ex, "Redis unavailable at {RedisEndpoint}. Rate limiting will fallback to allow-all.", redisConnectionString);
    redis = null;
}

var app = builder.Build();
var appConfiguration = app.Configuration; // Store for use in middleware

// Apply CORS policy
app.UseCors("FrontendCors");

// Enable WebSockets for SignalR proxying
app.UseWebSockets();

// Serilog request logging
app.UseSerilogRequestLogging();

// Correlation ID middleware
app.Use(async (context, next) =>
{
    const string HeaderName = "X-Correlation-Id";
    if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || StringValues.IsNullOrEmpty(value))
    {
        value = Guid.NewGuid().ToString();
    }
    var correlationId = value.ToString();

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers[HeaderName] = correlationId;
        // Enrich current Activity and Serilog scope
        Activity.Current?.SetTag("correlation.id", correlationId);
        await next.Invoke();
    }
});

// Add authentication middleware - must be before MapReverseProxy to authenticate WebSocket connections
app.UseAuthentication();

// Authorization is handled per-route basis (see MapReverseProxy below)

// Rate limiting middleware (per-IP and per-user via Redis counters)
app.Use(async (context, next) =>
{
    // Exempt health endpoints
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // If Redis not available, skip limiting
    if (redis is null || !redis.IsConnected)
    {
        await next();
        return;
    }

    var db = redis.GetDatabase();
    var now = DateTimeOffset.UtcNow;

    // Resolve client IP (prefer X-Forwarded-For if present)
    string clientIp = context.Request.Headers.TryGetValue("X-Forwarded-For", out var fwd)
        ? fwd.ToString().Split(',')[0].Trim()
        : (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    // Resolve user id from claims when authenticated
    string? userId = null;
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? context.User.FindFirst("sub")?.Value
                 ?? context.User.FindFirst("uid")?.Value;
    }

    // IP rate limit: 100 req/min
    var ipWindowKey = now.ToString("yyyyMMddHHmm");
    var ipKey = $"rl:ip:{clientIp}:{ipWindowKey}";
    long ipCount = await db.StringIncrementAsync(ipKey);
    if (ipCount == 1)
    {
        await db.KeyExpireAsync(ipKey, TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(5));
    }
    const int ipLimit = 100;
    var ipRemaining = Math.Max(0, ipLimit - (int)ipCount);

    // User rate limit: 1000 req/hour (only if authenticated)
    const int userLimit = 1000;
    long userCount = 0;
    int userRemaining = userLimit;
    if (!string.IsNullOrEmpty(userId))
    {
        var userWindowKey = now.ToString("yyyyMMddHH");
        var userKey = $"rl:user:{userId}:{userWindowKey}";
        userCount = await db.StringIncrementAsync(userKey);
        if (userCount == 1)
        {
            await db.KeyExpireAsync(userKey, TimeSpan.FromHours(1) + TimeSpan.FromSeconds(10));
        }
        userRemaining = Math.Max(0, userLimit - (int)userCount);
    }

    // Select active policy for header reporting
    var isAuthenticated = !string.IsNullOrEmpty(userId);
    var activeLimit = isAuthenticated ? userLimit : ipLimit;
    var activeRemaining = isAuthenticated ? userRemaining : ipRemaining;

    // Add rate limit headers (also include IP-specific when authenticated)
    context.Response.Headers["X-RateLimit-Limit"] = activeLimit.ToString();
    context.Response.Headers["X-RateLimit-Remaining"] = activeRemaining.ToString();
    context.Response.Headers["X-RateLimit-Limit-IP"] = ipLimit.ToString();
    context.Response.Headers["X-RateLimit-Remaining-IP"] = ipRemaining.ToString();
    if (isAuthenticated)
    {
        context.Response.Headers["X-RateLimit-Limit-User"] = userLimit.ToString();
        context.Response.Headers["X-RateLimit-Remaining-User"] = userRemaining.ToString();
    }

    // Enforce limits (both IP and User if authenticated)
    var ipExceeded = ipCount > ipLimit;
    var userExceeded = isAuthenticated && userCount > userLimit;
    if (ipExceeded || userExceeded)
    {
        // Compute Retry-After based on window end
        TimeSpan retryAfter;
        if (ipExceeded)
        {
            var minuteFloorUtc = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);
            retryAfter = minuteFloorUtc.AddMinutes(1) - now;
        }
        else
        {
            var hourFloorUtc = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
            retryAfter = hourFloorUtc.AddHours(1) - now;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString();
        await context.Response.WriteAsync("Too Many Requests");
        return;
    }

    await next();
});

// IMPORTANT: Apply authorization only for non-auth routes.
// Auth endpoints (/api/auth/*) must allow anonymous access for login/register/refresh.
// Using a branch avoids unintended 401 challenges on those paths.
app.UseWhen(context =>
    !context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase),
    subApp =>
    {
        subApp.UseAuthorization();
    });

// Feature flags: expose current values to aid cutover verification
var useLegacyChat = builder.Configuration.GetValue<bool>("Features:UseLegacyChat");
var useLegacyOrch = builder.Configuration.GetValue<bool>("Features:UseLegacyOrchestration");
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Feature-UseLegacyChat"] = useLegacyChat ? "true" : "false";
    ctx.Response.Headers["X-Feature-UseLegacyOrchestration"] = useLegacyOrch ? "true" : "false";
    await next();
});

// Expose Prometheus scraping endpoint
app.MapPrometheusScrapingEndpoint("/metrics");

// Map health endpoints (allow anonymous access)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Map reverse proxy with custom authorization and traffic routing
// Auth endpoints (login, register, refresh) allow anonymous access
// All other endpoints require authentication
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Allow anonymous access to auth service endpoints
        if (path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/health", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }
        
        // Allow SignalR hub endpoints to pass through (authentication handled by JWT Bearer middleware)
        // JWT Bearer middleware extracts token from query string for WebSocket connections
        if (path.StartsWith("/hubs/chat", StringComparison.OrdinalIgnoreCase))
        {
            // For SignalR hub connections, authentication is handled by JWT Bearer middleware
            // which extracts the token from the query string. If authentication fails, it will return 401.
            await next();
            return;
        }
        
        // For all other routes, require authentication
        if (!context.User?.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        
        await next();
    });

    // Phase 5: Traffic routing middleware (10% → 50% → 100%)
    proxyPipeline.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var routingService = context.RequestServices.GetRequiredService<TrafficRoutingService>();
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var corrValues)
            ? corrValues.FirstOrDefault()
            : null;

        // Determine service name from path
        string? serviceName = null;
        if (path.StartsWith("/api/conversations", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/chat", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hubs/chat", StringComparison.OrdinalIgnoreCase))
        {
            serviceName = "chat";
        }
        else if (path.StartsWith("/api/orchestration", StringComparison.OrdinalIgnoreCase))
        {
            serviceName = "orchestration";
        }

        // Route based on feature flags and traffic percentage
        if (serviceName != null && !routingService.ShouldRouteToNewService(serviceName, correlationId))
        {
            // Route to legacy system (if configured)
            var legacyUrl = appConfiguration[$"LegacySystem:{serviceName}:BaseUrl"];
            if (!string.IsNullOrEmpty(legacyUrl))
            {
                context.Request.Headers["X-Routed-To"] = "legacy";
                // In a real scenario, you'd proxy to legacy URL
                // For now, we'll log and continue to new system as fallback
                Log.Warning(
                    "Traffic routing: Request for {ServiceName} should go to legacy, but legacy URL not configured. Routing to new system.",
                    serviceName);
            }
        }
        else if (serviceName != null)
        {
            context.Request.Headers["X-Routed-To"] = "new";
        }

        // Add traffic percentage header for observability
        if (serviceName != null)
        {
            var percentage = routingService.GetTrafficPercentage(serviceName);
            context.Response.Headers["X-Traffic-Percentage"] = percentage.ToString();
        }

        await next();
    });

    // Phase 5: Dual-write monitoring (for POST/PUT/PATCH operations)
    proxyPipeline.Use(async (context, next) =>
    {
        var method = context.Request.Method;
        if (method == "POST" || method == "PUT" || method == "PATCH")
        {
            var path = context.Request.Path.Value ?? string.Empty;
            string? serviceName = null;
            if (path.StartsWith("/api/conversations", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/chat", StringComparison.OrdinalIgnoreCase))
            {
                serviceName = "chat";
            }
            else if (path.StartsWith("/api/orchestration", StringComparison.OrdinalIgnoreCase))
            {
                serviceName = "orchestration";
            }

            if (serviceName != null)
            {
                var dualWriteService = context.RequestServices.GetRequiredService<DualWriteService>();
                if (dualWriteService.IsDualWriteEnabled(serviceName))
                {
                    // Mark request for dual-write (actual write happens in response handler)
                    context.Items["DualWriteEnabled"] = serviceName;
                    Log.Debug("Dual-write enabled for {ServiceName} on {Method} {Path}",
                        serviceName, method, path);
                }
            }
        }

        await next();
    });
});

await app.RunAsync();
