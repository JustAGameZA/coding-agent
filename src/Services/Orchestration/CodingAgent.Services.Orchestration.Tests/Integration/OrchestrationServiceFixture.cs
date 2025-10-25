using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CodingAgent.Services.Orchestration.Tests.Integration;

public sealed class OrchestrationServiceFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    public HttpClient Client { get; private set; } = default!;
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;

    public OrchestrationServiceFixture() { }

    public async Task InitializeAsync()
    {
        string? connectionString = null;

        // Try to start PostgreSQL Testcontainer; fall back to in-memory if Docker is unavailable
        try
        {
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("orchestration_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgres.StartAsync();
            connectionString = _postgres.GetConnectionString();
        }
        catch (ArgumentException)
        {
            // Docker endpoint not detected; continue with in-memory database
            connectionString = null;
        }

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Ensure non-production environment for tests so the app uses in-memory DB
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    // Reduce test server logging to minimize console I/O and keep VS Code responsive
                    var settings = new Dictionary<string, string?>
                    {
                        ["Logging:LogLevel:Default"] = "Warning",
                        ["Logging:LogLevel:Microsoft"] = "Warning",
                        ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning"
                    };

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        settings["ConnectionStrings:OrchestrationDb"] = connectionString;
                    }

                    config.AddInMemoryCollection(settings!);
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        if (_postgres is not null)
        {
            try
            { await _postgres.StopAsync(); }
            catch { /* ignore */ }
            try
            { await _postgres.DisposeAsync(); }
            catch { /* ignore */ }
        }
    }
}

[CollectionDefinition("OrchestrationServiceCollection")]
public sealed class OrchestrationServiceCollection : ICollectionFixture<OrchestrationServiceFixture>
{
}
