using CodingAgent.Services.Ollama.Domain.Services;
using CodingAgent.Services.Ollama.Infrastructure.CloudApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CodingAgent.Services.Ollama.Tests.Integration;

/// <summary>
/// Integration tests for cloud API fallback functionality
/// </summary>
[Collection("OllamaServiceCollection")]
[Trait("Category", "Integration")]
public class CloudApiFallbackIntegrationTests
{
    private readonly OllamaServiceFixture _fixture;

    public CloudApiFallbackIntegrationTests(OllamaServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CloudApiClient_ShouldBeRegistered()
    {
        // Act
        var cloudApiClient = _fixture.Factory.Services.GetService<ICloudApiClient>();

        // Assert
        cloudApiClient.Should().NotBeNull();
        cloudApiClient.Should().BeOfType<MockCloudApiClient>();
    }

    [Fact]
    public void CloudApiClient_WhenNotConfigured_ShouldReturnFalse()
    {
        // Arrange
        var cloudApiClient = _fixture.Factory.Services.GetRequiredService<ICloudApiClient>();

        // Act
        var isConfigured = cloudApiClient.IsConfigured();

        // Assert
        isConfigured.Should().BeFalse("default configuration should not have cloud API enabled");
    }

    [Fact]
    public async Task CloudApiClient_WhenNotConfigured_ShouldNotHaveTokens()
    {
        // Arrange
        var cloudApiClient = _fixture.Factory.Services.GetRequiredService<ICloudApiClient>();

        // Act
        var hasTokens = await cloudApiClient.HasTokensAvailableAsync();

        // Assert
        hasTokens.Should().BeFalse("cloud API not configured, so no tokens should be available");
    }

    [Fact]
    public void TokenUsageTracker_ShouldBeRegistered()
    {
        // Act
        var tokenTracker = _fixture.Factory.Services.GetService<ITokenUsageTracker>();

        // Assert
        tokenTracker.Should().NotBeNull();
        tokenTracker.Should().BeOfType<InMemoryTokenUsageTracker>();
    }

    [Fact]
    public async Task TokenUsageTracker_ShouldTrackUsage()
    {
        // Arrange
        var tokenTracker = _fixture.Factory.Services.GetRequiredService<ITokenUsageTracker>();
        const string provider = "test-provider";
        const int tokens = 1500;

        // Act
        await tokenTracker.RecordUsageAsync(tokens, provider);
        var usage = await tokenTracker.GetMonthlyUsageAsync(provider);

        // Assert
        usage.Should().Be(tokens);
    }

    [Fact]
    public async Task TokenUsageTracker_ShouldEnforceMonthlyLimit()
    {
        // Arrange
        var tokenTracker = _fixture.Factory.Services.GetRequiredService<ITokenUsageTracker>();
        const string provider = "test-provider-limit";
        const int monthlyLimit = 10000;
        
        await tokenTracker.RecordUsageAsync(9000, provider);

        // Act
        var withinLimit = await tokenTracker.IsWithinLimitAsync(500, provider, monthlyLimit);
        var overLimit = await tokenTracker.IsWithinLimitAsync(1500, provider, monthlyLimit);

        // Assert
        withinLimit.Should().BeTrue("9000 + 500 = 9500 is within 10000 limit");
        overLimit.Should().BeFalse("9000 + 1500 = 10500 exceeds 10000 limit");
    }
}
