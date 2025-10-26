using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CodingAgent.Services.Ollama.Tests.Integration;

/// <summary>
/// Test fixture for integration tests with Ollama Testcontainer
/// </summary>
public sealed class OllamaServiceFixture : IAsyncLifetime
{
    private IContainer? _ollamaContainer;
    public HttpClient Client { get; private set; } = default!;
    public WebApplicationFactory<Program> Factory { get; private set; } = default!;
    public string? OllamaBaseUrl { get; private set; }

    public OllamaServiceFixture() { }

    public async Task InitializeAsync()
    {
        // Try to start Ollama Testcontainer; if Docker is unavailable, skip container startup
        try
        {
            _ollamaContainer = new ContainerBuilder()
                .WithImage("ollama/ollama:latest")
                .WithPortBinding(11434, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(11434))
                .Build();

            await _ollamaContainer.StartAsync();
            var port = _ollamaContainer.GetMappedPublicPort(11434);
            OllamaBaseUrl = $"http://localhost:{port}";
        }
        catch (ArgumentException)
        {
            // Docker endpoint not detected; tests will run without Ollama container
            OllamaBaseUrl = null;
        }

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    if (!string.IsNullOrEmpty(OllamaBaseUrl))
                    {
                        var dict = new Dictionary<string, string?>
                        {
                            ["Ollama:BaseUrl"] = OllamaBaseUrl
                        };
                        config.AddInMemoryCollection(dict!);
                    }
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (Client is not null)
        {
            Client.Dispose();
        }
        
        if (Factory is not null)
        {
            Factory.Dispose();
        }
        
        if (_ollamaContainer is not null)
        {
            try
            { 
                await _ollamaContainer.StopAsync(); 
            }
            catch 
            { 
                /* ignore */ 
            }
            
            try
            { 
                await _ollamaContainer.DisposeAsync(); 
            }
            catch 
            { 
                /* ignore */ 
            }
        }
    }
}

[CollectionDefinition("OllamaServiceCollection")]
public sealed class OllamaServiceCollection : ICollectionFixture<OllamaServiceFixture>
{
}
