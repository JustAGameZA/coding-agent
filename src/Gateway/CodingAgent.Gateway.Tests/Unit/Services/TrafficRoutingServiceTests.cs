using CodingAgent.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Gateway.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class TrafficRoutingServiceTests
{
    private readonly Mock<ILogger<TrafficRoutingService>> _loggerMock;

    public TrafficRoutingServiceTests()
    {
        _loggerMock = new Mock<ILogger<TrafficRoutingService>>();
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
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act
        var result = serviceWithConfig.ShouldRouteToNewService("chat");

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
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act
        var result = serviceWithConfig.ShouldRouteToNewService("chat");

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
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act
        var result = serviceWithConfig.ShouldRouteToNewService("chat", "test-correlation-id");

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
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act - Same correlation ID should route to same destination
        var correlationId = "test-123";
        var result1 = serviceWithConfig.ShouldRouteToNewService("chat", correlationId);
        var result2 = serviceWithConfig.ShouldRouteToNewService("chat", correlationId);

        // Assert - Deterministic routing
        Assert.Equal(result1, result2);
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
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act
        var result = serviceWithConfig.GetTrafficPercentage("Chat");

        // Assert
        Assert.Equal(75, result);
    }

    [Fact]
    public void GetTrafficPercentage_WhenNotConfigured_ShouldReturn100()
    {
        // Arrange - Empty config should default to 100
        var configBuilder = new ConfigurationBuilder();
        var testConfig = configBuilder.Build();
        var serviceWithConfig = new TrafficRoutingService(testConfig, _loggerMock.Object);

        // Act
        var result = serviceWithConfig.GetTrafficPercentage("unknown");

        // Assert
        Assert.Equal(100, result);
    }
}
