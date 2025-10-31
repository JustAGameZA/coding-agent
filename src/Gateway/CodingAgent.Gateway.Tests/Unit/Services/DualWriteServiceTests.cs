using CodingAgent.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodingAgent.Gateway.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class DualWriteServiceTests
{
    private readonly Mock<ILogger<DualWriteService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public DualWriteServiceTests()
    {
        _loggerMock = new Mock<ILogger<DualWriteService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
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
        var serviceWithConfig = new DualWriteService(testConfig, _loggerMock.Object, _httpClientFactoryMock.Object);

        // Act
        var result = serviceWithConfig.IsDualWriteEnabled("Chat");

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
        var serviceWithConfig = new DualWriteService(testConfig, _loggerMock.Object, _httpClientFactoryMock.Object);

        // Act
        var result = serviceWithConfig.IsDualWriteEnabled("Chat");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDualWriteEnabled_WhenNotConfigured_ShouldReturnFalse()
    {
        // Arrange - Empty config should default to false
        var configBuilder = new ConfigurationBuilder();
        var testConfig = configBuilder.Build();
        var serviceWithConfig = new DualWriteService(testConfig, _loggerMock.Object, _httpClientFactoryMock.Object);

        // Act
        var result = serviceWithConfig.IsDualWriteEnabled("unknown");

        // Assert
        Assert.False(result);
    }
}
