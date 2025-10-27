using CodingAgent.Services.Chat.Infrastructure.Presence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Diagnostics.Metrics;
using Xunit;

namespace CodingAgent.Services.Chat.Tests.Unit.Infrastructure.Presence;

[Trait("Category", "Unit")]
public class PresenceServiceTests
{
    private readonly Mock<ILogger<PresenceService>> _loggerMock;
    private readonly Mock<IMeterFactory> _meterFactoryMock;
    private readonly Mock<Meter> _meterMock;
    private readonly PresenceService _service;

    public PresenceServiceTests()
    {
        _loggerMock = new Mock<ILogger<PresenceService>>();
        _meterFactoryMock = new Mock<IMeterFactory>();
        _meterMock = new Mock<Meter>("test-meter");

        // Setup meter factory to return mock meter
        _meterFactoryMock
            .Setup(f => f.Create(It.IsAny<MeterOptions>()))
            .Returns(_meterMock.Object);

        // Create service with null Redis connection
        _service = new PresenceService(null, _loggerMock.Object, _meterFactoryMock.Object);
    }

    [Fact]
    public async Task SetUserOnlineAsync_WithoutRedis_ShouldLogWarning()
    {
        // Arrange
        var userId = "test-user";
        var connectionId = "conn-123";

        // Act
        await _service.SetUserOnlineAsync(userId, connectionId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis not available")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetUserOfflineAsync_WithoutRedis_ShouldLogWarning()
    {
        // Arrange
        var userId = "test-user";
        var connectionId = "conn-123";

        // Act
        await _service.SetUserOfflineAsync(userId, connectionId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Redis not available")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsUserOnlineAsync_WithoutRedis_ShouldReturnFalse()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _service.IsUserOnlineAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetLastSeenAsync_WithoutRedis_ShouldReturnNull()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _service.GetLastSeenAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOnlineUsersAsync_WithoutRedis_ShouldReturnEmptyList()
    {
        // Arrange & Act
        var result = await _service.GetOnlineUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserConnectionCountAsync_WithoutRedis_ShouldReturnZero()
    {
        // Arrange
        var userId = "test-user";

        // Act
        var result = await _service.GetUserConnectionCountAsync(userId);

        // Assert
        result.Should().Be(0);
    }
}
