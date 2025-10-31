using CodingAgent.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodingAgent.Gateway.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class TrafficRoutingServiceTests
{
    private readonly ILogger<TrafficRoutingService> _logger;

    public TrafficRoutingServiceTests()
    {
        _logger = NullLogger<TrafficRoutingService>.Instance;
    }

    [Fact]
    public void ShouldRouteToNewService_WhenFeatureFlagIsFalse_ShouldReturnTrue()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Features:UseLegacyChat"] = "false"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.ShouldRouteToNewService("chat");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRouteToNewService_WhenFeatureFlagIsTrue_ShouldReturnFalse()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Features:UseLegacyChat"] = "true"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.ShouldRouteToNewService("chat");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRouteToNewService_WhenPercentageIs100_ShouldReturnTrue()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Features:UseLegacyChat"] = "false",
            ["TrafficRouting:chat:Percentage"] = "100"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.ShouldRouteToNewService("chat", "test-correlation-id");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRouteToNewService_WhenPercentageIs50_ShouldRouteDeterministically()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Features:UseLegacyChat"] = "false",
            ["TrafficRouting:chat:Percentage"] = "50"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act - Same correlation ID should route to same destination
        var correlationId = "test-123";
        var result1 = service.ShouldRouteToNewService("chat", correlationId);
        var result2 = service.ShouldRouteToNewService("chat", correlationId);

        // Assert - Deterministic routing
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void ShouldRouteToNewService_WhenPercentageIs0_ShouldReturnFalse()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Features:UseLegacyChat"] = "false",
            ["TrafficRouting:chat:Percentage"] = "0"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.ShouldRouteToNewService("chat", "test-correlation-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRouteToNewService_WhenUnknownService_ShouldReturnTrue()
    {
        // Arrange - Unknown service should default to new system
        var configBuilder = new ConfigurationBuilder();
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.ShouldRouteToNewService("unknown-service");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetTrafficPercentage_ShouldReturnConfiguredValue()
    {
        // Arrange - Use actual configuration instance
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["TrafficRouting:Chat:Percentage"] = "75"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.GetTrafficPercentage("Chat");

        // Assert
        Assert.Equal(75, result);
    }

    [Fact]
    public void GetTrafficPercentage_WhenNotConfigured_ShouldReturn100()
    {
        // Arrange - Empty config should default to 100
        var configBuilder = new ConfigurationBuilder();
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.GetTrafficPercentage("unknown");

        // Assert
        Assert.Equal(100, result);
    }

    [Fact]
    public void GetTrafficPercentage_WhenInvalidValue_ShouldReturn100()
    {
        // Arrange - Invalid value should default to 100
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["TrafficRouting:Chat:Percentage"] = "invalid"
        };
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(inMemoryConfig);
        var testConfig = configBuilder.Build();
        var service = new TrafficRoutingService(testConfig, _logger);

        // Act
        var result = service.GetTrafficPercentage("Chat");

        // Assert
        Assert.Equal(100, result);
    }
}
