using CodingAgent.Services.Dashboard.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace CodingAgent.Services.Dashboard.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class DashboardCacheServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<ILogger<DashboardCacheService>> _mockLogger;
    private readonly DashboardCacheService _service;

    public DashboardCacheServiceTests()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<DashboardCacheService>>();
        _service = new DashboardCacheService(_mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WhenCacheHit_ShouldReturnDeserializedValue()
    {
        // Arrange
        var key = "test-key";
        var cachedJson = """{"Value": "test"}""";
        var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);
        
        _mockCache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedBytes);

        // Act
        var result = await _service.GetAsync<TestModel>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("test");
    }

    [Fact]
    public async Task GetAsync_WhenCacheMiss_ShouldReturnNull()
    {
        // Arrange
        var key = "test-key";
        _mockCache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetAsync<TestModel>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeAndCache()
    {
        // Arrange
        var key = "test-key";
        var value = new TestModel { Value = "test" };

        // Act
        await _service.SetAsync(key, value);

        // Assert
        _mockCache.Verify(x => x.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromCache()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _service.RemoveAsync(key);

        // Assert
        _mockCache.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenExceptionThrown_ShouldReturnNull()
    {
        // Arrange
        var key = "test-key";
        _mockCache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        var result = await _service.GetAsync<TestModel>(key);

        // Assert
        result.Should().BeNull();
    }

    private class TestModel
    {
        public string Value { get; set; } = string.Empty;
    }
}
