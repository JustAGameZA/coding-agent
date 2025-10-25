using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

/// <summary>
/// Enhanced test fixture with RabbitMQ support for event publishing verification.
/// Used for E2E tests that verify the full execution flow with messaging enabled.
/// </summary>
public sealed class OrchestrationWithRabbitMQFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RabbitMqContainer? _rabbitmq;
    
    public HttpClient Client { get; private set; } = default!;
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

    public OrchestrationWithRabbitMQFixture() { }

    public async Task InitializeAsync()
    {
        string? postgresConnectionString = null;
        string? rabbitMqConnectionString = null;

        // Try to start Testcontainers; fall back gracefully if Docker is unavailable
        try
        {
            // Start PostgreSQL
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("orchestration_events_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
            postgresConnectionString = _postgres.GetConnectionString();

            // Start RabbitMQ
            _rabbitmq = new RabbitMqBuilder()
                .WithImage("rabbitmq:3-management-alpine")
                .WithUsername("guest")
                .WithPassword("guest")
                .Build();

            await _rabbitmq.StartAsync();
            rabbitMqConnectionString = _rabbitmq.GetConnectionString();
        }
        catch (ArgumentException)
        {
            // Docker endpoint not detected; tests will use in-memory DB and NoOpEventPublisher
            postgresConnectionString = null;
            rabbitMqConnectionString = null;
        }

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["Logging:LogLevel:Default"] = "Warning",
                        ["Logging:LogLevel:Microsoft"] = "Warning",
                        ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
                        
                        // Enable messaging if RabbitMQ is available
                        ["Messaging:Enabled"] = (!string.IsNullOrEmpty(rabbitMqConnectionString)).ToString()
                    };

                    if (!string.IsNullOrEmpty(postgresConnectionString))
                    {
                        settings["ConnectionStrings:OrchestrationDb"] = postgresConnectionString;
                    }

                    if (!string.IsNullOrEmpty(rabbitMqConnectionString))
                    {
                        settings["RabbitMQ:Host"] = rabbitMqConnectionString;
                    }

                    config.AddInMemoryCollection(settings!);
                });
            });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Returns true if RabbitMQ container is running and messaging is enabled.
    /// </summary>
    public bool IsRabbitMQAvailable => _rabbitmq != null;

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        
        if (_postgres is not null)
        {
            try { await _postgres.StopAsync(); } catch { /* ignore */ }
            try { await _postgres.DisposeAsync(); } catch { /* ignore */ }
        }
        
        if (_rabbitmq is not null)
        {
            try { await _rabbitmq.StopAsync(); } catch { /* ignore */ }
            try { await _rabbitmq.DisposeAsync(); } catch { /* ignore */ }
        }
    }
}

[CollectionDefinition("OrchestrationWithRabbitMQCollection")]
public sealed class OrchestrationWithRabbitMQCollection : ICollectionFixture<OrchestrationWithRabbitMQFixture>
{
}
