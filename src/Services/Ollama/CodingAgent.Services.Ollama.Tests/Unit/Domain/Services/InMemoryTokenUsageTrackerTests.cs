using CodingAgent.Services.Ollama.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodingAgent.Services.Ollama.Tests.Unit.Domain.Services;

[Trait("Category", "Unit")]
public class InMemoryTokenUsageTrackerTests
{
    private readonly Mock<ILogger<InMemoryTokenUsageTracker>> _loggerMock;
    private readonly InMemoryTokenUsageTracker _sut;

    public InMemoryTokenUsageTrackerTests()
    {
        _loggerMock = new Mock<ILogger<InMemoryTokenUsageTracker>>();
        _sut = new InMemoryTokenUsageTracker(_loggerMock.Object);
    }

    [Fact]
    public async Task GetMonthlyUsageAsync_WhenNoUsage_ShouldReturnZero()
    {
        // Act
        var result = await _sut.GetMonthlyUsageAsync("openai");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task RecordUsageAsync_ShouldIncrementUsage()
    {
        // Arrange
        const string provider = "openai";
        const int tokens = 1000;

        // Act
        await _sut.RecordUsageAsync(tokens, provider);
        var result = await _sut.GetMonthlyUsageAsync(provider);

        // Assert
        result.Should().Be(tokens);
    }

    [Fact]
    public async Task RecordUsageAsync_MultipleRecords_ShouldAccumulate()
    {
        // Arrange
        const string provider = "openai";

        // Act
        await _sut.RecordUsageAsync(1000, provider);
        await _sut.RecordUsageAsync(500, provider);
        await _sut.RecordUsageAsync(250, provider);
        
        var result = await _sut.GetMonthlyUsageAsync(provider);

        // Assert
        result.Should().Be(1750);
    }

    [Fact]
    public async Task GetMonthlyUsageAsync_DifferentProviders_ShouldBeIsolated()
    {
        // Arrange
        await _sut.RecordUsageAsync(1000, "openai");
        await _sut.RecordUsageAsync(2000, "anthropic");

        // Act
        var openaiUsage = await _sut.GetMonthlyUsageAsync("openai");
        var anthropicUsage = await _sut.GetMonthlyUsageAsync("anthropic");

        // Assert
        openaiUsage.Should().Be(1000);
        anthropicUsage.Should().Be(2000);
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenUnderLimit_ShouldReturnTrue()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;
        
        await _sut.RecordUsageAsync(5000, provider);

        // Act
        var result = await _sut.IsWithinLimitAsync(3000, provider, monthlyLimit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenAtLimit_ShouldReturnTrue()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;
        
        await _sut.RecordUsageAsync(7000, provider);

        // Act
        var result = await _sut.IsWithinLimitAsync(3000, provider, monthlyLimit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenOverLimit_ShouldReturnFalse()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;
        
        await _sut.RecordUsageAsync(7000, provider);

        // Act
        var result = await _sut.IsWithinLimitAsync(4000, provider, monthlyLimit);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenNoUsage_ShouldCheckAgainstZero()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;

        // Act
        var result = await _sut.IsWithinLimitAsync(5000, provider, monthlyLimit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenExactlyAtLimit_ShouldReturnTrue()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;

        // Act
        var result = await _sut.IsWithinLimitAsync(10000, provider, monthlyLimit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinLimitAsync_WhenOneTokenOver_ShouldReturnFalse()
    {
        // Arrange
        const string provider = "openai";
        const int monthlyLimit = 10000;

        // Act
        var result = await _sut.IsWithinLimitAsync(10001, provider, monthlyLimit);

        // Assert
        result.Should().BeFalse();
    }
}
