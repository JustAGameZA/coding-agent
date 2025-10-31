using CodingAgent.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodingAgent.Gateway.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class DualWriteServiceTests
{
    private readonly ILogger<DualWriteService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DualWriteServiceTests()
    {
        _logger = NullLogger<DualWriteService>.Instance;
        var services = new ServiceCollection();
        services.AddHttpClient("new-system");
        services.AddHttpClient("legacy-system");
        var serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    }

    [Fact]
    public void IsDualWriteEnabled_WhenEnabled_ShouldReturnTrue()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["DualWrite:Chat:Enabled"] = "true"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new DualWriteService(testConfig, _logger, _httpClientFactory);

        // Act
        var result = service.IsDualWriteEnabled("Chat");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDualWriteEnabled_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["DualWrite:Chat:Enabled"] = "false"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new DualWriteService(testConfig, _logger, _httpClientFactory);

        // Act
        var result = service.IsDualWriteEnabled("Chat");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDualWriteEnabled_WhenNotConfigured_ShouldReturnFalse()
    {
        // Arrange - Empty config should default to false
        var configBuilder = new ConfigurationBuilder();
        var testConfig = configBuilder.Build();
        var service = new DualWriteService(testConfig, _logger, _httpClientFactory);

        // Act
        var result = service.IsDualWriteEnabled("unknown");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDualWriteEnabled_WhenInvalidValue_ShouldReturnFalse()
    {
        // Arrange - Invalid value should return false
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["DualWrite:Chat:Enabled"] = "invalid"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new DualWriteService(testConfig, _logger, _httpClientFactory);

        // Act
        var result = service.IsDualWriteEnabled("Chat");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteDualWriteAsync_WhenDisabled_ShouldOnlyWriteToNewSystem()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["DualWrite:Chat:Enabled"] = "false"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new DualWriteService(testConfig, _logger, _httpClientFactory);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://test.example.com/api/test");

        // Act
        var result = await service.ExecuteDualWriteAsync("Chat", request);

        // Assert
        Assert.True(result.NewSystemWritten);
        Assert.False(result.LegacySystemWritten);
        Assert.Null(result.NewSystemStatusCode);
        Assert.Null(result.LegacySystemStatusCode);
    }
}
